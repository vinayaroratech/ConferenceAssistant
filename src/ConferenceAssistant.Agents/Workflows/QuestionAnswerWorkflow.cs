using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Agents;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Workflows;

public class QuestionAnswerWorkflow(
    IAgentFactory agentFactory,
    ILogger<QuestionAnswerWorkflow>? logger = null,
    IAgentActivityService? activityService = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<QuestionAnswerWorkflow>.Instance;

    public async Task<string> ExecuteAsync(
        string questionId,
        string questionText,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[QuestionAnswerWorkflow] Answering question {QuestionId}: {Text}", questionId, questionText);
        activityService?.Log("KnowledgeCurator", $"Answering: {questionText}");

        var agent = agentFactory.CreateKnowledgeCurator();

        var response = await agent.RunAsync(
            $"""
            Question ID: {questionId}
            Question: "{questionText}"

            Search the session knowledge base for relevant context, then provide a clear,
            concise answer (2-4 sentences). Save your answer using the question ID above.
            """,
            cancellationToken: ct);

        var answer = response.Text ?? "No answer generated.";
        _logger.LogInformation("[QuestionAnswerWorkflow] Answer ready for question {QuestionId} \r\n {Answer}", questionId, answer);
        activityService?.Log("KnowledgeCurator", $"Answered: {questionText[..Math.Min(60, questionText.Length)]}…");
        return answer;
    }
}
