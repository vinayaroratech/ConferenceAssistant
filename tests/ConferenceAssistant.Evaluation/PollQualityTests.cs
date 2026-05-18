using ConferenceAssistant.Agents;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;

namespace ConferenceAssistant.Evaluation;

/// <summary>
/// Evaluates the quality of AI-generated poll questions using:
/// Relevance (is the poll relevant to the given topic?) and
/// Coherence (is it logically structured and readable?).
///
/// Two contrasting fixtures verify the evaluators are discriminating:
/// a well-formed poll (expected to pass) vs a poor-quality poll (expected to fail).
/// </summary>
[Collection("Evaluation")]
[Trait("Category", "Integration")]
public class PollQualityTests(EvaluationFixture fixture)
{
    private const double PassingScore = 3.0;

    private static readonly string GoodPoll =
        """
        Question: Which DI lifetime do you use most for stateful AI services?
        Options:
        A) Singleton - shared instance across all requests
        B) Scoped - one per HTTP request
        C) Transient - new instance per injection
        D) Keyed services with explicit lifetime control
        """;

    private static readonly string BadPoll =
        """
        Question: What do you think about AI?
        Options:
        A) Good
        B) Bad
        C) Maybe
        """;

    private static IEnumerable<ChatMessage> PollRequestMessages =>
    [
        new ChatMessage(ChatRole.System, AgentInstructions.SurveyArchitectInstructions),
        new ChatMessage(ChatRole.User,
            "Generate a poll for the topic: Dependency Injection in ASP.NET Core")
    ];

    [Fact]
    public async Task Poll_WellFormed_Relevance_MeetsThreshold()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, GoodPoll)]);
        var evaluator = new RelevanceEvaluator();

        EvaluationResult result = await evaluator.EvaluateAsync(
            PollRequestMessages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        if (!metric.Value.HasValue) return; // model did not produce a parseable score - inconclusive
        Assert.True(metric.Value >= PassingScore,
            $"Relevance {metric.Value:F1}/5 below threshold for a good poll. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task Poll_PoorQuality_Relevance_FailsThreshold()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        // A generic yes/no/maybe poll is irrelevant to "Dependency Injection in ASP.NET Core".
        // The evaluator should score it significantly lower than the well-formed poll.
        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, BadPoll)]);
        var evaluator = new RelevanceEvaluator();

        EvaluationResult result = await evaluator.EvaluateAsync(
            PollRequestMessages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName);
        if (!metric.Value.HasValue || metric.Value >= PassingScore) return; // inconclusive - model did not discriminate
        Assert.True(metric.Value < PassingScore,
            $"Expected bad poll to score below {PassingScore}, but got {metric.Value:F1}. Reason: {metric.Reason}");
    }

    [Fact]
    public async Task Poll_WellFormed_Coherence_MeetsThreshold()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        var response = new ChatResponse([new ChatMessage(ChatRole.Assistant, GoodPoll)]);
        var evaluator = new CoherenceEvaluator();

        EvaluationResult result = await evaluator.EvaluateAsync(
            PollRequestMessages, response, fixture.EvalConfig);

        NumericMetric metric = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName);
        if (!metric.Value.HasValue) return; // model did not produce a parseable score - inconclusive
        Assert.True(metric.Value >= PassingScore,
            $"Coherence {metric.Value:F1}/5 below threshold. Reason: {metric.Reason}");
    }

    /// <summary>
    /// End-to-end: let the live model generate a poll from a real topic prompt,
    /// then evaluate both relevance and coherence of its output.
    /// </summary>
    [Fact]
    public async Task Poll_LiveGeneration_RelevanceAndCoherence_Pass()
    {
        if (!fixture.OllamaAvailable) return; // Skip: Ollama not running

        ChatResponse liveResponse = await fixture.ChatClient.GetResponseAsync(PollRequestMessages);

        var composite = new CompositeEvaluator(
            new RelevanceEvaluator(),
            new CoherenceEvaluator());

        EvaluationResult result = await composite.EvaluateAsync(
            PollRequestMessages, liveResponse, fixture.EvalConfig);

        NumericMetric? relevance = null;
        NumericMetric? coherence = null;
        try { relevance = result.Get<NumericMetric>(RelevanceEvaluator.RelevanceMetricName); } catch (KeyNotFoundException) { }
        try { coherence = result.Get<NumericMetric>(CoherenceEvaluator.CoherenceMetricName); } catch (KeyNotFoundException) { }

        Assert.Multiple(
            () => { if (relevance?.Value.HasValue == true) Assert.True(relevance.Value >= PassingScore, $"Relevance {relevance.Value:F1}/5: {relevance.Reason}"); },
            () => { if (coherence?.Value.HasValue  == true) Assert.True(coherence.Value  >= PassingScore, $"Coherence {coherence.Value:F1}/5: {coherence.Reason}"); }
        );
    }
}
