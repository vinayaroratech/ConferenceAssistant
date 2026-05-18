using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Domain.Events;

public record PollCreated(string PollId, string? TopicId, string Question) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record PollLaunched(string PollId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record PollClosed(string PollId, DateTimeOffset ClosedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record PollReopened(string PollId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record VoteCast(string PollId, string SelectedOption, string? AttendeeId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
