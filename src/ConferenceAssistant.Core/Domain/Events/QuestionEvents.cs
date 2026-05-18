using ConferenceAssistant.Core.Domain;

namespace ConferenceAssistant.Core.Domain.Events;

public record QuestionSubmitted(string QuestionId, string Text) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record QuestionApproved(string QuestionId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record QuestionRejected(string QuestionId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record QuestionUpvoted(string QuestionId, int NewUpvoteCount) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}

public record QuestionAnswered(string QuestionId, string Answer) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
