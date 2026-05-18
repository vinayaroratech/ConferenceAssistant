namespace ConferenceAssistant.Core.Domain;

/// <summary>
/// Handles a specific domain event type. Implement this interface to react to
/// meaningful things that happened in the domain (e.g. auto-trigger analysis
/// when a poll closes, send notifications, update read models).
/// </summary>
public interface IDomainEventHandler<TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
