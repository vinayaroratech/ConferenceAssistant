namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Aggregate Root representing a conference session configuration.
/// Loaded from configuration at startup; treated as read-only after initialization.
/// </summary>
public sealed class Conference : AggregateRoot<string>
{
    public string Code        { get; set; } = "";
    public string Name        { get; set; } = "";
    public string Description { get; set; } = "";
    public string TopicsPath  { get; set; } = "";
    /// <summary>Which session inside a multi-session topics file to load. Matches sessionId in sessions.json.</summary>
    public string SessionId   { get; set; } = "";
    public string SlidesPath  { get; set; } = "";
    public bool   IsDefault   { get; set; }

    /// <summary>Lightweight topic list loaded at startup for display on the Home page.</summary>
    public List<TopicPreview> TopicPreviews { get; set; } = [];
}

public record TopicPreview(int Order, string Title, string Description);
