using ConferenceAssistant.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ConferenceAssistant.Core.Domain;

/// <summary>
/// Resolves and invokes all registered <see cref="IDomainEventHandler{TEvent}"/> instances
/// for the events raised on an entity. Call <see cref="DispatchAndClear{TId}"/> after any
/// aggregate mutation - dispatch is fire-and-forget so the service call is never blocked.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider sp, ILogger<DomainEventDispatcher> logger)
{
    /// <summary>
    /// Grabs all pending domain events from <paramref name="entity"/>, clears them,
    /// and dispatches them asynchronously in the background.
    /// </summary>
    public void DispatchAndClear<TId>(Entity<TId> entity) where TId : notnull
    {
        var events = entity.DomainEvents.ToList();
        entity.ClearDomainEvents();
        if (events.Count == 0) return;

        // Fire-and-forget: service mutations are synchronous; dispatch should not block them.
        _ = DispatchAsync(events);
    }

    /// <summary>
    /// Dispatches a single domain event that did not originate from an aggregate entity
    /// (e.g. service-level events such as <c>SessionEnded</c>). Fire-and-forget.
    /// </summary>
    public void Dispatch(IDomainEvent domainEvent)
        => _ = DispatchAsync([domainEvent]);

    private async Task DispatchAsync(IReadOnlyList<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
        {
            try
            {
                await DispatchSingleAsync(domainEvent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[DomainEventDispatcher] Unhandled error in handler for {EventType}",
                    domainEvent.GetType().Name);
            }
        }
    }

    private async Task DispatchSingleAsync(IDomainEvent domainEvent)
    {
        var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(domainEvent.GetType());
        var handlers = sp.GetServices(handlerType);

        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            var method = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;
            await (Task)method.Invoke(handler, [domainEvent, CancellationToken.None])!;
        }
    }
}
