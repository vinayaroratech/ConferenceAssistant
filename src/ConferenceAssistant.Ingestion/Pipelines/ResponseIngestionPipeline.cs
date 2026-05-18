using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.VectorData;
using ConferenceAssistant.Ingestion.Models;

namespace ConferenceAssistant.Ingestion.Pipelines;

/// <summary>
/// Ingests structured poll tally data into the vector store with AI-extracted sentiment and keywords.
/// Unlike <see cref="ContentIngestionPipeline"/>, this pipeline is purpose-built for short, structured
/// poll result text - no chunking, direct sentiment classification and keyword extraction via LLM.
/// </summary>
public class ResponseIngestionPipeline(
    IChatClient chatClient,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    VectorStoreCollection<Guid, ConferenceRecord> vectorStore,
    ILogger<ResponseIngestionPipeline>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<ResponseIngestionPipeline>.Instance;

    public async Task IngestResponsesAsync(
        string pollId,
        string question,
        Dictionary<string, int> tally,
        CancellationToken ct = default)
    {
        var totalVotes = tally.Values.Sum();
        _logger.LogInformation(
            "[ResponseIngestion] Starting ingestion for poll {PollId} - {Question} ({Total} votes)",
            pollId, question, totalVotes);

        var resultText = $"Poll: {question}\nTotal votes: {totalVotes}\n" +
            string.Join("\n", tally.Select(kv =>
                $"- {kv.Key}: {kv.Value} votes ({(totalVotes > 0 ? kv.Value * 100 / totalVotes : 0)}%)"));

        var sentimentResponse = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User,
                $"Classify the overall audience sentiment from these poll results as one word " +
                $"(positive, negative, neutral, mixed, curious, confused):\n\n{resultText}")],
            cancellationToken: ct);

        var keywordResponse = await chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User,
                $"Extract 3-5 keywords from these poll results. Comma-separated:\n\n{resultText}")],
            cancellationToken: ct);

        var sentiment = sentimentResponse.Text?.Trim();
        var keywords = keywordResponse.Text?.Split(',', StringSplitOptions.TrimEntries).ToList() ?? [];

        _logger.LogDebug(
            "[ResponseIngestion] Poll {PollId} - sentiment={Sentiment} keywords={Keywords}",
            pollId, sentiment, string.Join(", ", keywords));

        var record = new ConferenceRecord
        {
            Content = resultText,
            Source = "response",
            TopicId = pollId,
            Sentiment = sentiment,
            Keywords = keywords
        };

        var embeddingResults = await embeddingGenerator.GenerateAsync([record.Content], cancellationToken: ct);
        record.Embedding = embeddingResults[0].Vector;
        await vectorStore.UpsertAsync(record, ct);

        _logger.LogInformation("[ResponseIngestion] Poll {PollId} stored in vector store", pollId);
    }
}
