// .NET Aspire AppHost - Conference Assistant
// Orchestrates: Ollama (local LLM), Qdrant (vector store), PostgreSQL, Web app

var builder = DistributedApplication.CreateBuilder(args);

// --- Ollama (open-source LLM, runs locally via Docker) ---
var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithOpenWebUI();  // optional browser UI at http://localhost:8080

var chatModel = ollama.AddModel("llama3.2");
var embedModel = ollama.AddModel("nomic-embed-text");

// --- Qdrant (vector store for semantic search) ---
var qdrant = builder.AddQdrant("qdrant")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

// --- PostgreSQL (relational persistence - reserved for future use) ---
var postgres = builder.AddPostgres("postgres")
    .WithPgWeb()
    .WithDataVolume();

var conferenceDb = postgres.AddDatabase("conferencedb");

// --- Web App ---
builder.AddProject<Projects.ConferenceAssistant_Web>("web")
    .WithReference(chatModel)
    .WithReference(embedModel)
    .WithReference(qdrant)
    .WaitFor(ollama)
    .WaitFor(qdrant)
    // Use persistent Qdrant when running via Aspire.
    // Aspire injects ConnectionStrings__qdrant automatically from WithReference(qdrant).
    .WithEnvironment("VectorStore__Provider", "Qdrant")
    // Aspire injects ConnectionStrings__qdrant for gRPC (6334).
    // The health check needs the HTTP REST endpoint (6333) separately.
    .WithEnvironment("VectorStore__QdrantHttpEndpoint", "http://localhost:6333");

builder.Build().Run();
