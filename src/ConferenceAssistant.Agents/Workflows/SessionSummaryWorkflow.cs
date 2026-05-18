using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Agents;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Workflows;

public class SessionSummaryWorkflow(
    IAgentFactory agentFactory,
    ISessionSummaryService? summaryService = null,
    IAgentActivityService? activityService = null,
    ILogger<SessionSummaryWorkflow>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<SessionSummaryWorkflow>.Instance;

    public async Task<string> GenerateSummaryAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("[SessionSummaryWorkflow] Starting session summary generation");
        activityService?.Log("Synthesizer", "Gathering specialist analyses");

        var synthesizer = agentFactory.CreateSynthesizer();
        var response = await synthesizer.RunAsync(
            "Generate a comprehensive summary of this conference session by consulting all specialist agents.",
            cancellationToken: ct);

        var summary = response.Text ?? "Summary generation failed.";
        _logger.LogInformation("[SessionSummaryWorkflow] Summary complete ({Length} chars)", summary.Length);
        activityService?.Log("Synthesizer", "Summary complete");
        return summary;
    }

    /// <summary>Streams the summary, pushing each chunk to <see cref="ISessionSummaryService"/> as it arrives.</summary>
    public async Task GenerateSummaryStreamingAsync(CancellationToken ct = default)
    {
        summaryService?.BeginStreaming();
        activityService?.Log("SessionSummary", "Starting streaming session summary");
        try
        {
            _logger.LogInformation("[SessionSummaryWorkflow] Starting streaming session summary generation");
            var synthesizer = agentFactory.CreateSynthesizer();
            activityService?.Log("Synthesizer", "Gathering specialist analyses and synthesizing summary");

            await foreach (var update in synthesizer.RunStreamingAsync(
                "Generate a comprehensive summary of this conference session by consulting all specialist agents.",
                cancellationToken: ct))
            {
                summaryService?.AppendChunk(update.Text ?? "");
            }
        }
        finally
        {
            summaryService?.CompleteStreaming();
            activityService?.Log("SessionSummary", "Streaming complete");
        }
    }
}
