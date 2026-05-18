using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Agents;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Workflows;


public class PollGenerationWorkflow(
    IAgentFactory agentFactory,
    ILogger<PollGenerationWorkflow>? logger = null,
    IAgentActivityService? activityService = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<PollGenerationWorkflow>.Instance;

    public async Task<string> GeneratePollAsync(
        string? topicId,
        string topicTitle,
        string? topicDescription = null,
        IEnumerable<string>? talkingPoints = null,
        string? pollHint = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation("[PollGenerationWorkflow] Generating poll for topic '{TopicTitle}' (ID: {TopicId})", topicTitle, topicId ?? "none");
        activityService?.Log("SurveyArchitect", $"Generating poll for topic: {topicTitle}");

        var topicContext = new System.Text.StringBuilder();
        if (!string.IsNullOrWhiteSpace(topicDescription))
            topicContext.AppendLine($"Topic description: {topicDescription}");
        var points = talkingPoints?.ToList();
        if (points is { Count: > 0 })
            topicContext.AppendLine($"Key talking points:\n{string.Join("\n", points.Select(p => $"  - {p}"))}");

        var agent = agentFactory.CreateSurveyArchitect();

        var userMessage = $"""
            Create and save a poll for the following topic.

            Generate an engaging poll for the topic currently being discussed. The topic ID is: {topicId}. Use the available tools to understand the context and create a relevant poll.)
            """;

        var response = await agent.RunAsync(userMessage, cancellationToken: ct);

        _logger.LogInformation("[PollGenerationWorkflow] Poll generation complete for topic '{TopicTitle}'", topicTitle);
        activityService?.Log("SurveyArchitect", $"Poll created for topic: {topicTitle}");
        return response.Text ?? "";
    }
}
