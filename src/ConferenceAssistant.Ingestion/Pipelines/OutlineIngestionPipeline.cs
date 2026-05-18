using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using ConferenceAssistant.Ingestion.Models;

namespace ConferenceAssistant.Ingestion.Pipelines;

public class OutlineIngestionPipeline(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    VectorStoreCollection<Guid, ConferenceRecord> vectorStore,
    Tokenizer tokenizer,
    ILogger<OutlineIngestionPipeline>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<OutlineIngestionPipeline>.Instance;

    public async Task IngestAsync(string markdownPath, CancellationToken ct = default)
    {
        _logger.LogInformation("[OutlineIngestion] Starting ingestion from {Path}", markdownPath);
        await using var stream = File.OpenRead(markdownPath);
        var document = await new MarkdownReader().ReadAsync(stream, "outline", "text/markdown", ct);
        _logger.LogInformation("[OutlineIngestion] Document parsed from {Path}", markdownPath);
        await ProcessDocumentAsync(document, ct);
    }

    private async Task ProcessDocumentAsync(IngestionDocument document, CancellationToken ct)
    {
        // Chunk the document by markdown headers using the DataIngestion framework
        var chunkerOptions = new IngestionChunkerOptions(tokenizer);
        IAsyncEnumerable<IngestionChunk<string>> chunks = new HeaderChunker(chunkerOptions).ProcessAsync(document, ct);

        // Enrich each chunk with AI-generated summaries and keywords
        var enricherOptions = new EnricherOptions(chatClient);
        chunks = new SummaryEnricher(enricherOptions, maxWordCount: null).ProcessAsync(chunks, ct);
        chunks = new KeywordEnricher(enricherOptions, ReadOnlySpan<string>.Empty, maxKeywords: null, confidenceThreshold: null).ProcessAsync(chunks, ct);

        var upserted = 0;
        await foreach (var chunk in chunks.WithCancellation(ct))
        {
            var summary = chunk.HasMetadata && chunk.Metadata.TryGetValue(SummaryEnricher.MetadataKey, out var s)
                ? s?.ToString() : null;
            var keywords = chunk.HasMetadata && chunk.Metadata.TryGetValue(KeywordEnricher.MetadataKey, out var kw)
                ? (kw as IEnumerable<string>)?.ToList() ?? [] : (List<string>)[];

            _logger.LogDebug("[OutlineIngestion] Chunk {N} - keywords={Keywords} summary={Summary}",
                upserted + 1, string.Join(",", keywords), summary ?? "(none)");

            var embeddings = await embeddingGenerator.GenerateAsync([chunk.Content], cancellationToken: ct);
            await vectorStore.UpsertAsync(new ConferenceRecord
            {
                Content = chunk.Content,
                Source = "outline",
                Summary = summary,
                Keywords = keywords,
                Embedding = embeddings[0].Vector
            }, ct);
            upserted++;
        }
        _logger.LogInformation("[OutlineIngestion] Complete - {Total} chunk(s) stored", upserted);
    }
}
