using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using ConferenceAssistant.Core.Extensions;
using ConferenceAssistant.Ingestion.Models;
using ConferenceAssistant.Ingestion.Pipelines;

namespace ConferenceAssistant.Ingestion;

public static class IngestionServiceCollectionExtensions
{
    public static IServiceCollection AddIngestionServices(this IServiceCollection services)
    {
        // Vector store - chosen at runtime by configuration:
        //   VectorStore:Provider = "InMemory"  (default, no persistence)
        //   VectorStore:Provider = "Qdrant"    (persistent, requires Qdrant running)
        services.AddSingleton<VectorStoreCollection<Guid, ConferenceRecord>>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var provider = config["VectorStore:Provider"] ?? "InMemory";

            // Collection name is scoped to the AI provider so github (1536-dim) and
            // ollama (768-dim) collections coexist in Qdrant without conflicts.
            var collectionName = $"conference-knowledge-{config.GetAiProvider()}";
            var embeddingDim   = config.GetAiEmbeddingDimension();

            sp.GetRequiredService<ILoggerFactory>()
              .CreateLogger(nameof(IngestionServiceCollectionExtensions))
              .LogInformation(
                  "Vector store → collection: {Collection}, provider: {Provider}, embedding dim: {Dim}",
                  collectionName, config.GetAiProvider(), embeddingDim);

            var definition = new VectorStoreCollectionDefinition
            {
                Properties =
                [
                    new VectorStoreKeyProperty(nameof(ConferenceRecord.Id), typeof(Guid)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.Content), typeof(string)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.Source), typeof(string)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.TopicId), typeof(string)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.Summary), typeof(string)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.Keywords), typeof(List<string>)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.Sentiment), typeof(string)),
                    new VectorStoreDataProperty(nameof(ConferenceRecord.IngestedAt), typeof(DateTimeOffset)),
                    new VectorStoreVectorProperty<ReadOnlyMemory<float>>(nameof(ConferenceRecord.Embedding), embeddingDim),
                ]
            };

            if (provider.Equals("Qdrant", StringComparison.OrdinalIgnoreCase))
            {
                // Aspire injects ConnectionStrings:qdrant automatically when run via AppHost.
                // The Aspire Qdrant hosting resource provides the gRPC endpoint (port 6334) in
                // the format: "Endpoint=http://host:grpcPort;Key=apikey"
                // For standalone use, set VectorStore:QdrantEndpoint in appsettings.json.
                var rawConnStr = config.GetConnectionString("qdrant")
                    ?? config["VectorStore:QdrantEndpoint"]
                    ?? "http://localhost:6334";

                // Parse Aspire connection string format: "Endpoint=http://host:port;Key=apikey"
                // OR fall through if it is already a plain URL.
                var endpointStr = rawConnStr;
                string? apiKey = null;
                if (rawConnStr.Contains('='))
                {
                    foreach (var seg in rawConnStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var eq = seg.IndexOf('=');
                        if (eq < 0) continue;
                        var segKey = seg[..eq].Trim();
                        var segVal = seg[(eq + 1)..].Trim();
                        if (segKey.Equals("Endpoint", StringComparison.OrdinalIgnoreCase))
                            endpointStr = segVal;
                        else if (segKey.Equals("Key", StringComparison.OrdinalIgnoreCase))
                            apiKey = segVal;
                    }
                }

                // QdrantClient(Uri) creates a gRPC channel to the given endpoint - Aspire
                // provides the gRPC port (6334) in the connection string, so this is correct.
                var qdrantClient = new QdrantClient(new Uri(endpointStr), apiKey: apiKey);
                var qdrantStore = new QdrantVectorStore(qdrantClient, ownsClient: true);
                return qdrantStore.GetCollection<Guid, ConferenceRecord>(collectionName, definition);
            }

            // Default: InMemory (ephemeral, no external dependency)
            var inMemoryStore = new InMemoryVectorStore();
            return inMemoryStore.GetCollection<Guid, ConferenceRecord>(collectionName, definition);
        });

        // Pipelines
        services.AddSingleton<OutlineIngestionPipeline>();
        services.AddSingleton<ResponseIngestionPipeline>();
        services.AddSingleton<ContentIngestionPipeline>();

        // Tokenizer used by DataIngestion chunkers for chunk-size estimation.
        // A minimal character-level BPE tokenizer is sufficient - we do not use
        // MaxTokensPerChunk for header/section splitting, so only approximation is needed.
        services.AddSingleton<Tokenizer>(_ =>
        {
            // Build a vocab that maps every printable ASCII character to a unique ID.
            // With no merge rules the tokenizer is character-level (1 char ≈ 1 token).
            var vocab = new Dictionary<string, int> { ["<unk>"] = 0 };
            for (int i = 32; i <= 126; i++)
                vocab[((char)i).ToString()] = i - 31; // IDs 1-95

            var vocabJson = JsonSerializer.Serialize(vocab);
            const string merges = "#version: 0.2\n"; // empty merges = no BPE merging

            using var vocabStream = new MemoryStream(Encoding.UTF8.GetBytes(vocabJson));
            using var mergesStream = new MemoryStream(Encoding.UTF8.GetBytes(merges));
            return BpeTokenizer.Create(vocabStream, mergesStream);
        });

        // Search
        services.AddSingleton<ISemanticSearchService, SemanticSearchService>();

        return services;
    }
}
