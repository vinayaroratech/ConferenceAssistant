using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Domain.Events;

public record InsightStored(string InsightId, string PollId, InsightType Type) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
