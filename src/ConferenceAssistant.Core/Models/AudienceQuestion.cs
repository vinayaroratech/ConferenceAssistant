using ConferenceAssistant.Core.Domain.Events;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Entity representing a question submitted by an audience member.
/// All state changes go through behavior methods: Approve, Reject, Upvote, SetAnswer.
/// </summary>
public sealed class AudienceQuestion : Entity<string>
{
    private AudienceQuestion() { }

    public string Text { get; private set; } = "";
    public string? Answer { get; private set; }
    public string? AttendeeId { get; private set; }

    private int _upvotes;
    public int Upvotes => _upvotes;

    public DateTimeOffset AskedAt { get; private set; }

    /// <summary>Null = pending moderation, true = approved, false = rejected.</summary>
    public bool? IsApproved { get; private set; }

    /// <summary>Creates a new question submitted by an attendee. Raises QuestionSubmitted event.</summary>
    public static AudienceQuestion Submit(string text, string? attendeeId = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Question text is required.", nameof(text));

        var question = new AudienceQuestion
        {
            Id = Guid.NewGuid().ToString(),
            Text = text.Trim(),
            AttendeeId = attendeeId,
            AskedAt = DateTimeOffset.UtcNow
        };
        question.RaiseDomainEvent(new QuestionSubmitted(question.Id, text));
        return question;
    }

    /// <summary>Approves the question for display. Raises QuestionApproved event.</summary>
    public void Approve()
    {
        IsApproved = true;
        RaiseDomainEvent(new QuestionApproved(Id));
    }

    /// <summary>Rejects the question from display. Raises QuestionRejected event.</summary>
    public void Reject()
    {
        IsApproved = false;
        RaiseDomainEvent(new QuestionRejected(Id));
    }

    /// <summary>Increments the upvote count atomically. Raises QuestionUpvoted event.</summary>
    public void Upvote()
    {
        var newCount = Interlocked.Increment(ref _upvotes);
        RaiseDomainEvent(new QuestionUpvoted(Id, newCount));
    }

    /// <summary>Records the speaker's answer to this question. Raises QuestionAnswered event.</summary>
    public void SetAnswer(string answer)
    {
        Answer = answer;
        RaiseDomainEvent(new QuestionAnswered(Id, answer));
    }
}
