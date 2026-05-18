using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using ConferenceAssistant.Ingestion.Models;

namespace ConferenceAssistant.Ingestion;

public class SemanticSearchService(
    VectorStoreCollection<Guid, ConferenceRecord> vectorStore,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    ILogger<SemanticSearchService>? logger = null) : ISemanticSearchService
{
    private readonly ILogger _logger = logger ?? NullLogger<SemanticSearchService>.Instance;
    public async Task<IReadOnlyList<ConferenceRecord>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken ct = default)
    {
        var embeddingResults = await embeddingGenerator.GenerateAsync([query], cancellationToken: ct);

        _logger.LogInformation("[VectorSearch] Query: \"{Query}\" topK={TopK}", query, topK);

        var results = vectorStore.SearchAsync(
            embeddingResults[0].Vector,
            topK,
            cancellationToken: ct);

        var records = new List<ConferenceRecord>();
        await foreach (var result in results)
        {
            records.Add(result.Record);
        }

        if (records.Count == 0)
            _logger.LogWarning("[VectorSearch] No results returned for query: \"{Query}\"", query);

        return records;
    }

    public async Task<IReadOnlyList<ConferenceRecord>> SearchBySourceAsync(
        string query,
        string source,
        int topK = 5,
        CancellationToken ct = default)
    {
        var all = await SearchAsync(query, topK * 2, ct);
        return all.Where(r => r.Source == source).Take(topK).ToList();
    }

    /// <summary>
    /// Returns up to <paramref name="max"/> records from the store using a zero-vector search.
    /// Useful for debugging / inspection - not for semantic queries.
    /// </summary>
    public async Task<IReadOnlyList<ConferenceRecord>> GetAllAsync(
        int max = 200,
        CancellationToken ct = default)
    {
        // A zero vector returns ALL records in the InMemory store sorted by L2 distance.
        var zeroVector = new ReadOnlyMemory<float>(new float[768]);

        var records = new List<ConferenceRecord>();
        await foreach (var result in vectorStore.SearchAsync(zeroVector, max, cancellationToken: ct))
        {
            records.Add(result.Record);
        }

        // Re-sort by ingestion time so the most recent appear first
        records.Sort((a, b) => b.IngestedAt.CompareTo(a.IngestedAt));
        return records;
    }
}
