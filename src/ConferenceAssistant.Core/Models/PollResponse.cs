using ConferenceAssistant.Core.Domain.Events;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Immutable record of a single attendee's vote on a poll.
/// Treated as a Value Object - once cast, a vote cannot be modified.
/// </summary>
public sealed class PollResponse : Entity<string>
{
    private PollResponse() { }

    public string PollId { get; private set; } = "";
    public string SelectedOption { get; private set; } = "";
    public string? AttendeeId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    /// <summary>Records a vote cast by an attendee. Raises VoteCast event.</summary>
    public static PollResponse Cast(string pollId, string selectedOption, string? attendeeId = null)
    {
        if (string.IsNullOrWhiteSpace(selectedOption))
            throw new ArgumentException("Selected option is required.", nameof(selectedOption));

        var response = new PollResponse
        {
            Id = Guid.NewGuid().ToString(),
            PollId = pollId,
            SelectedOption = selectedOption,
            AttendeeId = attendeeId,
            Timestamp = DateTimeOffset.UtcNow
        };
        response.RaiseDomainEvent(new VoteCast(pollId, selectedOption, attendeeId));
        return response;
    }
}
