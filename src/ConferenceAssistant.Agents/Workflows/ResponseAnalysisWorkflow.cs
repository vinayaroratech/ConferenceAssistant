using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Agents;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Workflows;

public class ResponseAnalysisWorkflow(
    IAgentFactory agentFactory,
    ILogger<ResponseAnalysisWorkflow>? logger = null,
    IAgentActivityService? activityService = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<ResponseAnalysisWorkflow>.Instance;

    public async Task<string> ExecuteAsync(string pollId, CancellationToken ct = default)
    {
        _logger.LogInformation("[ResponseAnalysisWorkflow] Starting analysis for poll {PollId}", pollId);
        activityService?.Log("ResponseAnalyst", $"Analyzing poll {pollId}");

        var agent = agentFactory.CreateResponseAnalyst();

        var response = await agent.RunAsync(
            $"""
            POLL ID: {pollId}

            Analyse the results for the poll with ID "{pollId}".
            Pass "{pollId}" as the exact argument to GetPollResults - do not use a placeholder or schema.
            """,
            cancellationToken: ct);

        var result = response.Text ?? "";
        _logger.LogInformation("[ResponseAnalysisWorkflow] Analysis complete for poll {PollId}", pollId);
        activityService?.Log("ResponseAnalyst", $"Insight stored for poll {pollId}");
        return result;
    }
}
