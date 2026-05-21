using Microsoft.Extensions.AI;
using Microsoft.Extensions.Caching.Distributed;
using OpenAI;
using System.ClientModel;
using OllamaSharp;
using ConferenceAssistant.Core.Extensions;

namespace ConferenceAssistant.Web.Extensions;

internal static class AiServicesExtensions
{
    private const string GitHubModelsEndpoint = "https://models.inference.ai.azure.com";

    // ── Provider helpers → see ConferenceAssistant.Core.Extensions.AiConfigurationExtensions ──

    // ── DI registration ──────────────────────────────────────────────────────

    /// <summary>
    /// Registers <see cref="IChatClient"/> and <see cref="IEmbeddingGenerator{TInput,TEmbedding}"/>
    /// based on the <c>AI:Provider</c> configuration value ("github" or "ollama").
    /// Also registers the distributed memory cache used by the chat client middleware.
    /// </summary>
    public static WebApplicationBuilder AddAiServices(
        this WebApplicationBuilder builder, string provider)
    {
        builder.Services.AddDistributedMemoryCache();

        if (provider == "github")
            builder.AddGitHubModels();
        else
            builder.AddOllamaAi();

        return builder;
    }

    // ── GitHub Models ────────────────────────────────────────────────────────

    private static void AddGitHubModels(this WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;

        var token = cfg["AI:GitHub:Token"]
            ?? throw new InvalidOperationException(
                "AI:GitHub:Token is required when AI:Provider=github. " +
                "Run: dotnet user-secrets set \"AI:GitHub:Token\" \"<your-PAT>\"");

        var chatModel  = cfg["AI:GitHub:ChatModel"]     ?? "gpt-4o";
        var embedModel = cfg["AI:GitHub:EmbeddingModel"] ?? "text-embedding-3-small";

        var client = new OpenAIClient(
            new ApiKeyCredential(token),
            new OpenAIClientOptions { Endpoint = new Uri(GitHubModelsEndpoint) });

        builder.Services.AddSingleton(sp =>
        {
            var pipeline = new ChatClientBuilder(client.GetChatClient(chatModel).AsIChatClient())
                // UseFunctionInvocation() omitted: ChatClientAgent manages its own tool-call loop.
                .UseDistributedCache(sp.GetRequiredService<IDistributedCache>())
                .UseLogging(sp.GetRequiredService<ILoggerFactory>())
                .Build(sp);

            sp.GetRequiredService<ILoggerFactory>()
              .CreateLogger(nameof(AiServicesExtensions))
              .LogInformation("IChatClient ready → GitHub Models ({Model})", chatModel);

            return pipeline;
        });

        builder.Services.AddEmbeddingGenerator(
            client.GetEmbeddingClient(embedModel).AsIEmbeddingGenerator());
    }

    // ── Ollama (local) ───────────────────────────────────────────────────────

    private static void AddOllamaAi(this WebApplicationBuilder builder)
    {
        var cfg = builder.Configuration;

        var endpoint   = cfg["AI:Ollama:Endpoint"]       ?? "http://localhost:11434";
        var chatModel  = cfg["AI:Ollama:ChatModel"]       ?? "llama3.2";
        var embedModel = cfg["AI:Ollama:EmbeddingModel"]  ?? "nomic-embed-text";

        builder.Services.AddSingleton(sp =>
        {
            var pipeline = new ChatClientBuilder(new OllamaApiClient(new Uri(endpoint), chatModel))
                // UseFunctionInvocation() omitted: ChatClientAgent manages its own tool-call loop.
                .UseDistributedCache(sp.GetRequiredService<IDistributedCache>())
                .UseLogging(sp.GetRequiredService<ILoggerFactory>())
                .Build(sp);

            sp.GetRequiredService<ILoggerFactory>()
              .CreateLogger(nameof(AiServicesExtensions))
              .LogInformation("IChatClient ready → Ollama ({Endpoint}, {Model})", endpoint, chatModel);

            return pipeline;
        });

        builder.Services.AddEmbeddingGenerator(
            (IEmbeddingGenerator<string, Embedding<float>>)
                new OllamaApiClient(new Uri(endpoint), embedModel));
    }
}
