using ConferenceAssistant.Core.Domain.Events;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Aggregate Root representing a single topic within a conference session.
/// Configuration properties (Title, Description, etc.) are set during JSON loading.
/// Domain state (Status) is controlled exclusively through Activate() and Complete().
/// </summary>
public sealed class SessionTopic : AggregateRoot<string>
{
    // Configuration properties - populated by JSON deserialization
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int Order { get; set; }
    public List<string> TalkingPoints { get; set; } = [];
    public List<PollPrompt> PollPrompts { get; set; } = [];

    // Domain state - controlled by behavior methods only
    public TopicStatus Status { get; private set; } = TopicStatus.Upcoming;

    // Slides are injected by SessionService after loading; not part of the JSON payload
    private List<Slide> _slides = [];
    public IReadOnlyList<Slide> Slides => _slides.AsReadOnly();

    /// <summary>Transitions the topic to Active state. Raises TopicActivated event.</summary>
    public void Activate()
    {
        Status = TopicStatus.Active;
        RaiseDomainEvent(new TopicActivated(Id));
    }

    /// <summary>Transitions the topic to Completed state. Raises TopicCompleted event.</summary>
    public void Complete()
    {
        Status = TopicStatus.Completed;
        RaiseDomainEvent(new TopicCompleted(Id));
    }

    /// <summary>
    /// Associates the slides that belong to this topic.
    /// Called by SessionService after loading slides from the markdown file.
    /// </summary>
    internal void SetSlides(IEnumerable<Slide> slides) => _slides = slides.ToList();
}

public enum TopicStatus
{
    Upcoming,
    Active,
    Completed
}

/// <summary>
/// A pre-configured poll suggestion embedded in the session outline.
/// Treated as a Value Object - equal when context and hint match.
/// </summary>
public sealed record PollPrompt(string Context, string Hint)
{
    // Parameterless constructor required for System.Text.Json deserialization
    public PollPrompt() : this("", "") { }
}
