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
            $"Analyse the results for poll ID: {pollId}",
            cancellationToken: ct);

        var result = response.Text ?? "";
        _logger.LogInformation("[ResponseAnalysisWorkflow] Analysis complete for poll {PollId}", pollId);
        activityService?.Log("ResponseAnalyst", $"Insight stored for poll {pollId}");
        return result;
    }
}
