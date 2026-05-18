using ConferenceAssistant.Core.Domain.Events;

namespace ConferenceAssistant.Core.Models;

/// <summary>
/// Immutable AI-generated insight about a poll or session trend.
/// Insights are created once and never modified; use Create() factory.
/// </summary>
public sealed class Insight : Entity<string>
{
    private Insight() { }

    public string PollId { get; private set; } = "";
    public string Content { get; private set; } = "";
    public InsightType Type { get; private set; }
    public DateTimeOffset GeneratedAt { get; private set; }

    /// <summary>Creates and returns a new Insight. Raises InsightStored event.</summary>
    public static Insight Create(string pollId, string content, InsightType type)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Insight content is required.", nameof(content));

        var insight = new Insight
        {
            Id = Guid.NewGuid().ToString(),
            PollId = pollId,
            Content = content.Trim(),
            Type = type,
            GeneratedAt = DateTimeOffset.UtcNow
        };
        insight.RaiseDomainEvent(new InsightStored(insight.Id, pollId, type));
        return insight;
    }
}

public enum InsightType
{
    PollAnalysis,
    AudienceTrend,
    KnowledgeGap,
    Summary
}
