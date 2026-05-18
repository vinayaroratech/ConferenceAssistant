using ConferenceAssistant.Agents.Workflows;
using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Agents.Handlers;

/// <summary>
/// Reacts when a poll is closed by automatically triggering the
/// <see cref="ResponseAnalysisWorkflow"/>. This removes the need for the presenter
/// to manually click "Analyze" - insights are generated the moment voting ends.
/// </summary>
public sealed class PollClosedHandler(
    ResponseAnalysisWorkflow workflow,
    ILogger<PollClosedHandler> logger) : IDomainEventHandler<PollClosed>
{
    public async Task HandleAsync(PollClosed @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[PollClosedHandler] Poll {PollId} closed - auto-triggering response analysis",
            @event.PollId);

        try
        {
            var result = await workflow.ExecuteAsync(@event.PollId, ct);
            logger.LogInformation(
                "[PollClosedHandler] Analysis complete for poll {PollId}: {Result}",
                @event.PollId, result);
        }
        catch (Exception ex)
        {
            // Analysis failure is non-fatal - presenter can still trigger manually
            logger.LogError(ex,
                "[PollClosedHandler] Auto-analysis failed for poll {PollId}",
                @event.PollId);
        }
    }
}
