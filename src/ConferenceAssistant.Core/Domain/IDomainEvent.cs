namespace ConferenceAssistant.Core.Domain;

/// <summary>
/// Marker interface for domain events. Domain events represent something meaningful
/// that happened in the domain, expressed in past tense (PollLaunched, TopicCompleted).
/// Consumers can subscribe to these events to handle side effects outside the aggregate.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
