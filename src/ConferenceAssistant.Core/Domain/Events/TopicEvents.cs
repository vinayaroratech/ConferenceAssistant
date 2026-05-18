using ConferenceAssistant.Core.Domain;

namespace ConferenceAssistant.Core.Domain.Events;

public record TopicActivated(string TopicId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record TopicCompleted(string TopicId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
