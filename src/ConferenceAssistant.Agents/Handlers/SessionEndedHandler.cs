using ConferenceAssistant.Agents.Workflows;
using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Domain.Events;
using ConferenceAssistant.Core.Services;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Agents.Handlers;

/// <summary>
/// Reacts when the session ends by automatically triggering the
/// <see cref="SessionSummaryWorkflow"/>. This removes the need for the presenter
/// to manually click "Generate Summary" — the moment the session closes, the
/// Synthesizer agent kicks off and streams the summary to <see cref="ISessionSummaryService"/>.
/// If it fails, the activity log shows a message and the presenter can use the
/// Retry button in the UI to re-run.
/// </summary>
public sealed class SessionEndedHandler(
    SessionSummaryWorkflow workflow,
    IAgentActivityService activityService,
    ILogger<SessionEndedHandler> logger) : IDomainEventHandler<SessionEnded>
{
    public async Task HandleAsync(SessionEnded @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[SessionEndedHandler] Session ended at {OccurredAt:u} - auto-triggering summary generation",
            @event.OccurredAt);
        activityService.Log("Synthesizer", "Session ended — generating summary…");

        try
        {
            await workflow.GenerateSummaryStreamingAsync(ct);
            activityService.Log("Synthesizer", "✓ Auto-summary complete");
        }
        catch (Exception ex)
        {
            // Summary failure is non-fatal - presenter can use the Retry button
            logger.LogError(ex, "[SessionEndedHandler] Auto-summary generation failed");
            activityService.Log("Synthesizer", "⚠ Auto-summary failed — use Retry button");
        }
    }
}
