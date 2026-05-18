using ConferenceAssistant.Core.Domain;

namespace ConferenceAssistant.Core.Domain.Events;

/// <summary>Raised when the presenter calls EndSession().</summary>
public record SessionEnded : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
