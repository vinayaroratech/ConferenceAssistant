using ConferenceAssistant.Core.Domain;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Base class for all domain entities. An entity has a unique identity (Id)
/// and supports raising domain events to signal meaningful state changes.
/// </summary>
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; set; } = default!;

    private List<IDomainEvent>? _domainEvents;

    public IReadOnlyList<IDomainEvent> DomainEvents =>
        _domainEvents?.AsReadOnly() ?? (IReadOnlyList<IDomainEvent>)Array.Empty<IDomainEvent>();

    protected void RaiseDomainEvent(IDomainEvent @event)
    {
        _domainEvents ??= [];
        _domainEvents.Add(@event);
    }

    public void ClearDomainEvents() => _domainEvents?.Clear();

    public override bool Equals(object? obj) =>
        obj is Entity<TId> other && EqualityComparer<TId>.Default.Equals(Id, other.Id);

    public override int GetHashCode() => Id.GetHashCode();

    public static bool operator ==(Entity<TId>? left, Entity<TId>? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Entity<TId>? left, Entity<TId>? right) => !(left == right);
}
