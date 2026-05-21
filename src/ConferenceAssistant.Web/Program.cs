using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.AI;
using ConferenceAssistant.Agents.Handlers;
using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Domain.Events;
using ConferenceAssistant.Core.Domain.Handlers;
using ConferenceAssistant.Core.Services;
using ConferenceAssistant.Ingestion;
using ConferenceAssistant.Agents;
using ConferenceAssistant.Mcp;
using ConferenceAssistant.Mcp.Server;
using ConferenceAssistant.Mcp.Tools;
using ConferenceAssistant.Core.Extensions;
using ConferenceAssistant.Web.Components;
using ConferenceAssistant.Web.Extensions;
using ConferenceAssistant.Web.HealthChecks;
using ModelContextProtocol.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ────────────────────
builder.Services.AddLogging(config =>
{
    config.AddConsole();
    config.SetMinimumLevel(LogLevel.Information);
});

// ── AI (IChatClient + IEmbeddingGenerator) ────────────────────────────────────
// Switch between "github" (GitHub Models) and "ollama" (local) via AI:Provider.
// Store secrets with: dotnet user-secrets set "AI:GitHub:Token" "<your-PAT>"
var aiProvider = builder.Configuration.GetAiProvider();
builder.AddAiServices(aiProvider);

// ── Core Services ──────────────
builder.Services.AddSingleton<DomainEventDispatcher>();
builder.Services.AddSingleton<IPollService, PollService>();
builder.Services.AddSingleton<ISessionService, SessionService>();
builder.Services.AddSingleton<IConferenceRegistry, ConferenceRegistry>();
builder.Services.AddSingleton<IAgentActivityService, AgentActivityService>();
builder.Services.AddSingleton<ISessionSummaryService, SessionSummaryService>();

// ── Domain Event Handlers (Core) 
builder.Services.AddSingleton<IDomainEventHandler<TopicActivated>, TopicActivatedHandler>();
builder.Services.AddSingleton<IDomainEventHandler<InsightStored>,  InsightStoredHandler>();

// ── Ingestion + Vector Data ──────
builder.Services.AddIngestionServices();

// ── Agents + Workflows ──────────────
builder.Services.AddAgentServices();

// ── Domain Event Handlers (Agents) ────────────────────────────────────────────
builder.Services.AddSingleton<IDomainEventHandler<PollClosed>,   PollClosedHandler>();
builder.Services.AddSingleton<IDomainEventHandler<SessionEnded>, SessionEndedHandler>();

// ── MCP Server ─────────────────
builder.Services.AddMcpServices();
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<ConferenceTools>()
    .WithTools<KnowledgeTools>();

// ── Blazor ─────────────────────
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ── Health Checks ───────────────
builder.Services.AddHttpClient("ollama-health").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(15));
builder.Services.AddHttpClient("health").ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(2));

var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck<QdrantHealthCheck>("qdrant",            tags: ["ready"])
    .AddCheck<SessionDataHealthCheck>("session-data", tags: ["ready"]);

if (!builder.Configuration.IsGitHubAiProvider())   // Ollama health check only applies when running locally
    healthChecks.AddCheck<OllamaHealthCheck>("ollama", tags: ["ready"]);

var app = builder.Build();

// --- Initialize session data ---
// ResolveDataFile walks up from cwd until it finds the file.
var ResolveDataFile = ConferenceAssistant.Core.Services.ConferenceRegistry.ResolveDataFile;

using (var scope = app.Services.CreateScope())
{
    // Ensure the vector store collection exists before any pipeline tries to upsert into it.
    app.Logger.LogInformation("Initializing vector store collection...");
    var vectorCollection = scope.ServiceProvider
        .GetRequiredService<Microsoft.Extensions.VectorData.VectorStoreCollection<Guid, ConferenceAssistant.Ingestion.Models.ConferenceRecord>>();
    await vectorCollection.EnsureCollectionExistsAsync();
    app.Logger.LogInformation("Vector store collection ready.");

    // Check if the vector store already has data (relevant for persistent stores like Qdrant).
    // A zero-vector search with topK=1 is the lightest way to probe for existing records.
    var zeroVec = new ReadOnlyMemory<float>(new float[builder.Configuration.GetAiEmbeddingDimension()]);
    var probe = vectorCollection.SearchAsync(zeroVec, 1);
    var hasExistingData = false;
    await foreach (var _ in probe) { hasExistingData = true; break; }

    // Load conference registry
    var conferenceRegistry = scope.ServiceProvider.GetRequiredService<IConferenceRegistry>();
    var conferencesPath = ResolveDataFile("data/conferences.json");
    if (conferencesPath is not null)
    {
        await conferenceRegistry.LoadAsync(conferencesPath);
        app.Logger.LogInformation("Loaded {Count} conference(s).", conferenceRegistry.All.Count);
    }

    var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();
    // Apply the default conference name + code, then load its data files
    var defaultConf = conferenceRegistry.Default;
    if (defaultConf is not null)
    {
        sessionService.SessionName = defaultConf.Name;
        sessionService.SessionCode = defaultConf.Code;
    }

    // Prefer the default conference's own paths; fall back to legacy config keys
    var defaultTopicsRelative = defaultConf?.TopicsPath
        ?? builder.Configuration["Session:TopicsPath"]
        ?? "data/sessions.json";
    var defaultSlidesRelative = defaultConf?.SlidesPath
        ?? builder.Configuration["Session:SlidesPath"]
        ?? "data/slides.md";

    var topicsPath = ResolveDataFile(defaultTopicsRelative);
    if (topicsPath is not null)
    {
        app.Logger.LogInformation("Loading topics from {Path}...", topicsPath);
        await sessionService.LoadTopicsAsync(topicsPath, defaultConf?.SessionId);
        app.Logger.LogInformation("Loaded {Count} topics.", sessionService.GetAllTopics().Count);
    }
    else
    {
        app.Logger.LogWarning("Topics file not found - session will start without topics.");
    }

    var slidesPath = ResolveDataFile(defaultSlidesRelative);
    if (slidesPath is not null)
    {
        app.Logger.LogInformation("Loading slides from {Path}...", slidesPath);
        await sessionService.LoadSlidesAsync(slidesPath);
        app.Logger.LogInformation("Loaded {Count} slides.", sessionService.TotalSlides);
    }
    else
    {
        app.Logger.LogWarning("Slides file not found - presentation will have no slides.");
    }

    var outlinePipeline = scope.ServiceProvider.GetRequiredService<
        ConferenceAssistant.Ingestion.Pipelines.OutlineIngestionPipeline>();
    var outlinePath = ResolveDataFile(builder.Configuration["Session:OutlinePath"] ?? "data/session-outline.md");
    if (outlinePath is not null)
    {
        if (hasExistingData)
        {
            // Persistent store (e.g. Qdrant) already has records - skip re-ingestion.
            // Remove the data volume or set VectorStore__ForceReingest=true to re-ingest.
            var forceReingest = builder.Configuration.GetValue<bool>("VectorStore:ForceReingest");
            if (forceReingest)
            {
                app.Logger.LogInformation("ForceReingest=true - re-ingesting session outline from {Path}...", outlinePath);
                await outlinePipeline.IngestAsync(outlinePath);
                app.Logger.LogInformation("Session outline re-ingestion complete.");
            }
            else
            {
                app.Logger.LogInformation("Vector store already contains data - skipping outline ingestion. Set VectorStore:ForceReingest=true to override.");
            }
        }
        else
        {
            app.Logger.LogInformation("Ingesting session outline from {Path}...", outlinePath);
            await outlinePipeline.IngestAsync(outlinePath);
            app.Logger.LogInformation("Session outline ingestion complete.");
        }
    }
    else
    {
        app.Logger.LogWarning("Session outline file not found - knowledge base will be empty.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

// Health endpoints
// GET /health/status  - full JSON report (all components)
// GET /health/ready   - readiness (Ollama + session data + Qdrant)
// GET /health/alive   - liveness only (process is up)
app.MapHealthChecks("/health/status", new HealthCheckOptions
{
    ResponseWriter = HealthStatusWriter.WriteJsonAsync
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready"),
    ResponseWriter = HealthStatusWriter.WriteJsonAsync
});
app.MapHealthChecks("/health/alive");

// MCP endpoint
app.MapMcp("/mcp");

// Blazor
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

