using ConferenceAssistant.Core.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Core.Domain.Handlers;

/// <summary>
/// Reacts when a topic becomes active. Logs the activation for analytics/audit purposes.
/// Extend this handler to push events to telemetry, update dashboards, etc.
/// </summary>
public sealed class TopicActivatedHandler(ILogger<TopicActivatedHandler> logger)
    : IDomainEventHandler<TopicActivated>
{
    public Task HandleAsync(TopicActivated @event, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[TopicActivated] Topic {TopicId} became active at {OccurredAt:u}",
            @event.TopicId, @event.OccurredAt);
        return Task.CompletedTask;
    }
}
