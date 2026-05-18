namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Marks an entity as an Aggregate Root - the single entry point for all mutations
/// within the aggregate boundary. Only aggregate roots should be loaded from and
/// persisted to the repository.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull { }
