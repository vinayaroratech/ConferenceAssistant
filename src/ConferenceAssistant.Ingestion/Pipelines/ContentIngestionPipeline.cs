using System.Text;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DataIngestion;
using Microsoft.Extensions.DataIngestion.Chunkers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.VectorData;
using Microsoft.ML.Tokenizers;
using ConferenceAssistant.Ingestion.Models;

namespace ConferenceAssistant.Ingestion.Pipelines;

public class ContentIngestionPipeline(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    VectorStoreCollection<Guid, ConferenceRecord> vectorStore,
    Tokenizer tokenizer,
    ILogger<ContentIngestionPipeline>? logger = null)
{
    public async Task IngestAsync(string content, string sourceUrl, CancellationToken ct = default)
    {
        logger?.LogInformation("[ContentIngestion] Starting ingestion from {SourceUrl} ({Bytes} bytes)",
            sourceUrl, Encoding.UTF8.GetByteCount(content));

        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        var document = await new MarkdownReader().ReadAsync(stream, sourceUrl, "text/markdown", ct);

        logger?.LogInformation("[ContentIngestion] Parsed document from {SourceUrl}", sourceUrl);
        await ProcessDocumentAsync(document, sourceUrl, ct);
    }

    private async Task ProcessDocumentAsync(IngestionDocument document, string sourceUrl, CancellationToken ct)
    {
        var chunkerOptions = new IngestionChunkerOptions(tokenizer);
        IAsyncEnumerable<IngestionChunk<string>> chunks = new SectionChunker(chunkerOptions).ProcessAsync(document, ct);

        var enricherOptions = new EnricherOptions(chatClient);
        chunks = new SummaryEnricher(enricherOptions, maxWordCount: null).ProcessAsync(chunks, ct);

        var upserted = 0;
        await foreach (var chunk in chunks.WithCancellation(ct))
        {
            var summary = chunk.HasMetadata && chunk.Metadata.TryGetValue(SummaryEnricher.MetadataKey, out var s)
                ? s?.ToString() : null;

            logger?.LogDebug("[ContentIngestion] Chunk {N} - summarised={HasSummary} length={Length} chars",
                upserted + 1, summary is not null, chunk.Content.Length);

            var embeddings = await embeddingGenerator.GenerateAsync([chunk.Content], cancellationToken: ct);

            logger?.LogDebug("[ContentIngestion] Chunk {N} - embedding generated ({Dims} dims)",
                upserted + 1, embeddings[0].Vector.Length);

            await vectorStore.UpsertAsync(new ConferenceRecord
            {
                Content = chunk.Content,
                Source = "content-doc",
                TopicId = sourceUrl,
                Summary = summary,
                Embedding = embeddings[0].Vector
            }, ct);

            upserted++;
            logger?.LogInformation("[ContentIngestion] Chunk {N} upserted to vector store (source={Source})", upserted, sourceUrl);
        }

        logger?.LogInformation("[ContentIngestion] Completed {SourceUrl} - {Total} chunk(s) stored", sourceUrl, upserted);
    }
}
