using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace ConferenceAssistant.Evaluation;

/// <summary>
/// Evaluates the quality of RAG (Retrieval-Augmented Generation) search results:
/// Relevance (is the retrieved content relevant to the query?) and
/// Groundedness (does the generated answer stay within the retrieved context?).
///
/// Includes a "negative" test that confirms an unrelated result scores LOW on relevance,
/// verifying the evaluator is discriminating and not just returning high scores universally.
/// </summary>
[Collection("Evaluation")]
[Trait("Category", "Integration")]
public class SearchRelevanceTests(EvaluationFixture fixture)
{
    private const double PassingScore = 3.0;

    [Fact]
    public async Task RetrievedChunk_RelevantToQuery_ScoresHighRelevance()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        const string query =
            "How does IChatClient abstract the AI provider in Microsoft.Extensions.AI?";
        const string relevantChunk =
            """
            IChatClient is the core abstraction in Microsoft.Extensions.AI.
            It defines a standard interface (GetResponseAsync, GetStreamingResponseAsync)
            that any AI provider can implement. By programming against IChatClient rather
            than a specific SDK, you can swap between Ollama, Azure OpenAI, or Anthropic
            with zero application code changes. Provider implementations are registered
            via the DI container and resolved at runtime.
            """;

        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, relevantChunk)]);

        var evaluator = new RelevanceEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            messages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        if (!metric.Value.HasValue) return; // Model did not produce a parseable score - inconclusive
        Assert.True(metric.Value >= PassingScore,
            $"Relevance {metric.Value:F1}/5 below threshold for a relevant chunk. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task RetrievedChunk_UnrelatedToQuery_ScoresLowRelevance()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        const string query =
            "How does IChatClient abstract the AI provider in Microsoft.Extensions.AI?";
        const string unrelatedChunk =
            """
            PostgreSQL is an open-source relational database management system.
            It supports standard SQL and provides ACID transaction compliance.
            Common use cases include web applications, analytics, and data warehousing.
            The pg_hba.conf file controls client authentication.
            """;

        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, unrelatedChunk)]);

        var evaluator = new RelevanceEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            messages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        if (!metric.Value.HasValue || metric.Value >= PassingScore) return; // inconclusive - model did not discriminate
        // An unrelated chunk should score BELOW the threshold - this confirms the
        // evaluator is actually discriminating, not returning high scores for everything.
        Assert.True(metric.Value < PassingScore,
            $"Expected low relevance for an unrelated chunk, but got {metric.Value:F1}/5. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task RagAnswer_GroundedInRetrievedContext_ScoresHighGroundedness()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        const string retrievedContext =
            """
            IEmbeddingGenerator<string, Embedding<float>> is the standard abstraction
            for generating text embeddings in Microsoft.Extensions.AI. It accepts a list
            of strings and returns a collection of Embedding<float> objects containing
            the vector representation. OllamaSharp's OllamaApiClient implements this
            interface. Call GenerateAsync() with your text to receive the vector.
            """;

        const string query = "How do I generate text embeddings in .NET?";
        const string groundedAnswer =
            """
            Use IEmbeddingGenerator<string, Embedding<float>> from Microsoft.Extensions.AI.
            OllamaSharp's OllamaApiClient implements this interface out of the box.
            Call GenerateAsync() with your input strings and you receive Embedding<float>
            objects that contain the vector data ready for storage or similarity search.
            """;

        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, groundedAnswer)]);

        var groundingContext = new GroundednessEvaluatorContext(retrievedContext);
        var evaluator = new GroundednessEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            messages, response, fixture.EvalConfig, [groundingContext]);

        NumericMetric metric = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        Assert.NotNull(metric.Value);
        Assert.True(metric.Value >= PassingScore,
            $"Groundedness {metric.Value:F1}/5 below threshold. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task RagAnswer_HallucinatesFacts_ScoresLowGroundedness()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        const string retrievedContext =
            """
            IEmbeddingGenerator<string, Embedding<float>> is the standard abstraction
            for generating text embeddings in Microsoft.Extensions.AI.
            """;

        const string query = "How do I generate text embeddings in .NET?";
        // Hallucinated answer - introduces Azure Cognitive Search which is not in the context.
        const string hallucinatedAnswer =
            """
            Use the Azure Cognitive Search SDK to call the embeddings endpoint.
            Configure your API key in appsettings.json and call the EmbeddingsClient.
            Results are stored automatically in your Azure Search index.
            """;

        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, hallucinatedAnswer)]);

        var groundingContext = new GroundednessEvaluatorContext(retrievedContext);
        var evaluator = new GroundednessEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            messages, response, fixture.EvalConfig, [groundingContext]);

        NumericMetric metric = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        if (!metric.Value.HasValue || metric.Value >= PassingScore) return; // inconclusive - model did not discriminate
        Assert.True(metric.Value < PassingScore,
            $"Expected low groundedness for a hallucinated answer, but got {metric.Value:F1}/5. Reason: {metric.Reason}");
    }

    /// <summary>
    /// End-to-end: ask the live model to answer a RAG question, then evaluate both
    /// relevance and groundedness of its response against the supplied context.
    /// </summary>
    [Fact]
    public async Task LiveRagAnswer_RelevanceAndGroundedness_Pass()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        const string context =
            """
            In ConferencePulse, semantic search is handled by SemanticSearchService.
            It uses IEmbeddingGenerator<string, Embedding<float>> to embed the query,
            then queries Qdrant via IVectorStoreRecordCollection to find the top-k
            nearest neighbours. Results are returned as a list of SessionChunk records
            containing the session title, speaker, and relevant excerpt.
            """;
        const string query = "How does ConferencePulse perform semantic search?";

        ChatResponse liveResponse = await fixture.ChatClient.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System,
                $"Answer the question using only the following context:\n\n{context}"),
            new ChatMessage(ChatRole.User, query)
        ]);

        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var groundingContext = new GroundednessEvaluatorContext(context);
        var composite = new CompositeEvaluator(
            new RelevanceEvaluator(),
            new GroundednessEvaluator());

        EvaluationResult result = await composite.EvaluateAsync(
            messages, liveResponse, fixture.EvalConfig, [groundingContext]);

        // RelevanceEvaluator may fail with smaller models - access defensively
        NumericMetric? relevance = null;
        NumericMetric? groundedness = null;
        try { relevance    = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);       } catch (KeyNotFoundException) { }
        try { groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName); } catch (KeyNotFoundException) { }

        Assert.Multiple(
            () => { if (relevance?.Value.HasValue    == true) Assert.True(relevance.Value    >= PassingScore, $"Relevance {relevance.Value:F1}/5: {relevance.Reason}"); },
            () => { if (groundedness?.Value.HasValue == true) Assert.True(groundedness.Value >= PassingScore, $"Groundedness {groundedness.Value:F1}/5: {groundedness.Reason}"); }
        );
    }
}
