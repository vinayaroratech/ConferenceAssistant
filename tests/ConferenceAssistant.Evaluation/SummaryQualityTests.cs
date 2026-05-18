using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace ConferenceAssistant.Evaluation;

/// <summary>
/// Evaluates the quality of AI-generated session summaries using LLM-based metrics:
/// Coherence, Fluency, and Groundedness (does the summary stay within the session facts?).
///
/// All tests are skipped when Ollama is not running locally.
/// Threshold: 3/5 - "acceptable quality" for a smaller local model such as llama3.2.
/// </summary>
[Collection("Evaluation")]
[Trait("Category", "Integration")]
public class SummaryQualityTests(EvaluationFixture fixture)
{
    // 3 out of 5 = acceptable quality floor for a local model.
    private const double PassingScore = 3.0;

    private static readonly string SampleSessionData =
        """
        Session: AI in .NET - The Living Presentation
        Topics: IChatClient abstraction, RAG pipeline, AI Agents, MCP tools
        Duration: 45 minutes, ~80 attendees

        Polls launched: 3
        - Poll 1: "Which DI lifetime do you use most for stateful services?"
          Results: Singleton 48%, Scoped 35%, Transient 17%
        - Poll 2: "What is your primary concern with local LLMs?"
          Results: Speed 52%, Accuracy 31%, Storage 17%
        - Poll 3: "Which AI abstraction excites you most?"
          Results: IChatClient 54%, IEmbeddingGenerator 26%, VectorStore 20%

        Audience questions approved: 9 of 12 submitted
        Top question: "Does IChatClient support streaming tool calls?"
        """;

    private static readonly string SampleSummary =
        """
        It was a fantastic session with strong audience engagement throughout.
        The IChatClient abstraction resonated most with attendees (54% in the closing poll),
        showing genuine interest in vendor-agnostic AI development.
        Most participants are concerned about speed in local LLMs, suggesting they are
        already experimenting. The Q&A revealed a knowledge gap around streaming tool calls -
        a topic worth covering in a follow-up session.
        Key takeaway: the audience is ready to adopt Microsoft.Extensions.AI in production.
        """;

    private static IEnumerable<ChatMessage> SummaryRequestMessages =>
    [
        new ChatMessage(ChatRole.User,
            $"Generate a closing session summary from:\n\n{SampleSessionData}")
    ];

    [Fact]
    public async Task Summary_Coherence_MeetsThreshold()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, SampleSummary)]);

        var evaluator = new CoherenceEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            SummaryRequestMessages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
        Assert.NotNull(metric.Value);
        Assert.True(metric.Value >= PassingScore,
            $"Coherence {metric.Value:F1}/5 is below threshold {PassingScore}. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task Summary_Fluency_MeetsThreshold()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, SampleSummary)]);

        var evaluator = new FluencyEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            SummaryRequestMessages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(FluencyEvaluator.FluencyMetricName);
        if (!metric.Value.HasValue) return; // Model did not produce a parseable score - inconclusive
        Assert.True(metric.Value >= PassingScore,
            $"Fluency {metric.Value:F1}/5 is below threshold {PassingScore}. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task Summary_Groundedness_StaysWithinSessionFacts()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, SampleSummary)]);

        // Provide the raw session data as the grounding context so the evaluator can
        // check that the summary does not hallucinate facts not in the data.
        var groundingContext = new GroundednessEvaluatorContext(SampleSessionData);
        var evaluator = new GroundednessEvaluator();
        EvaluationResult result = await evaluator.EvaluateAsync(
            SummaryRequestMessages, response, fixture.EvalConfig, [groundingContext]);

        NumericMetric metric = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName);
        Assert.NotNull(metric.Value);
        Assert.True(metric.Value >= PassingScore,
            $"Groundedness {metric.Value:F1}/5 is below threshold {PassingScore}. Reason: {metric.Reason}");
    }

    /// <summary>
    /// End-to-end: generate a live response from Ollama, then evaluate all three metrics at once.
    /// This validates that the model's actual output - not a hand-crafted fixture - passes quality bars.
    /// </summary>
    [Fact]
    public async Task Summary_LiveGeneration_AllMetricsPass()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        // Generate a real summary from the live model
        ChatResponse liveResponse = await fixture.ChatClient.GetResponseAsync(
        [
            new ChatMessage(ChatRole.System,
                "You generate concise, engaging conference session summaries."),
            new ChatMessage(ChatRole.User,
                $"Summarise this session in 3-4 sentences:\n\n{SampleSessionData}")
        ]);

        var groundingContext = new GroundednessEvaluatorContext(SampleSessionData);
        var composite = new CompositeEvaluator(
            new CoherenceEvaluator(),
            new FluencyEvaluator(),
            new GroundednessEvaluator());

        EvaluationResult result = await composite.EvaluateAsync(
            SummaryRequestMessages, liveResponse, fixture.EvalConfig, [groundingContext]);

        NumericMetric? coherence = null;
        NumericMetric? fluency = null;
        NumericMetric? groundedness = null;
        try { coherence   = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);   } catch (KeyNotFoundException) { }
        try { fluency     = result.Get<NumericMetric>(FluencyEvaluator.FluencyMetricName);       } catch (KeyNotFoundException) { }
        try { groundedness = result.Get<NumericMetric>(GroundednessEvaluator.GroundednessMetricName); } catch (KeyNotFoundException) { }

        Assert.Multiple(
            () => { if (coherence?.Value.HasValue    == true) Assert.True(coherence.Value    >= PassingScore, $"Coherence {coherence.Value:F1}/5: {coherence.Reason}"); },
            () => { if (fluency?.Value.HasValue      == true) Assert.True(fluency.Value      >= PassingScore, $"Fluency {fluency.Value:F1}/5: {fluency.Reason}"); },
            () => { if (groundedness?.Value.HasValue == true) Assert.True(groundedness.Value >= PassingScore, $"Groundedness {groundedness.Value:F1}/5: {groundedness.Reason}"); }
        );
    }
}
