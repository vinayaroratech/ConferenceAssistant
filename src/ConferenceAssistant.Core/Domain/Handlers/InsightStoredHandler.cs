using ConferenceAssistant.Core.Domain.Events;
using ConferenceAssistant.Core.Services;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Core.Domain.Handlers;

/// <summary>
/// Reacts when an AI insight is stored. Pushes an activity log entry so the
/// Presenter's Agent Activity panel updates immediately (triggering UI refresh).
/// </summary>
public sealed class InsightStoredHandler(
    IAgentActivityService activityService,
    ILogger<InsightStoredHandler> logger) : IDomainEventHandler<InsightStored>
{
    public Task HandleAsync(InsightStored @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[InsightStored] Insight {InsightId} of type {Type} stored for poll {PollId} at {OccurredAt:u}",
            @event.InsightId, @event.Type, @event.PollId, @event.OccurredAt);

        var shortPollId = @event.PollId.Length > 8 ? @event.PollId[..8] + "…" : @event.PollId;
        activityService.Log("ResponseAnalyst",
            $"✓ Insight stored — poll {shortPollId} ({@event.Type})");

        return Task.CompletedTask;
    }
}
