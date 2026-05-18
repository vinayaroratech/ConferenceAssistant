using ConferenceAssistant.Core.Domain.Events;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Aggregate Root representing a poll question with answer options.
/// All state transitions go through domain behavior methods: Launch, Close, Reopen.
/// </summary>
public sealed class Poll : AggregateRoot<string>
{
    private Poll() { }

    public string? TopicId { get; private set; }
    public string Question { get; private set; } = "";

    private readonly List<string> _options = [];
    public IReadOnlyList<string> Options => _options.AsReadOnly();

    public PollStatus Status { get; private set; } = PollStatus.Draft;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    /// <summary>Creates a new poll in Draft state. Raises PollCreated event.</summary>
    public static Poll Create(string? topicId, string question, IEnumerable<string> options)
    {
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Poll question is required.", nameof(question));

        var optList = options.Where(o => !string.IsNullOrWhiteSpace(o)).ToList();
        if (optList.Count < 2)
            throw new ArgumentException("A poll requires at least 2 options.", nameof(options));

        var poll = new Poll
        {
            Id = Guid.NewGuid().ToString(),
            TopicId = topicId,
            Question = question.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        poll._options.AddRange(optList);
        poll.RaiseDomainEvent(new PollCreated(poll.Id, topicId, question));
        return poll;
    }

    /// <summary>Transitions the poll from Draft to Active. Raises PollLaunched event.</summary>
    public void Launch()
    {
        if (Status != PollStatus.Draft)
            throw new InvalidOperationException($"Poll can only be launched from Draft. Current: {Status}");
        Status = PollStatus.Active;
        RaiseDomainEvent(new PollLaunched(Id));
    }

    /// <summary>Closes an active poll and records the close time. Raises PollClosed event.</summary>
    public void Close()
    {
        if (Status != PollStatus.Active)
            throw new InvalidOperationException($"Poll can only be closed when Active. Current: {Status}");
        Status = PollStatus.Closed;
        ClosedAt = DateTimeOffset.UtcNow;
        RaiseDomainEvent(new PollClosed(Id, ClosedAt.Value));
    }

    /// <summary>Reopens a closed poll for further voting. Raises PollReopened event.</summary>
    public void Reopen()
    {
        if (Status != PollStatus.Closed)
            throw new InvalidOperationException($"Poll can only be reopened when Closed. Current: {Status}");
        Status = PollStatus.Active;
        ClosedAt = null;
        RaiseDomainEvent(new PollReopened(Id));
    }
}

public enum PollStatus
{
    Draft,
    Active,
    Closed
}
