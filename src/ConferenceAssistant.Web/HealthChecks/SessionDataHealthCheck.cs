using ConferenceAssistant.Core.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConferenceAssistant.Web.HealthChecks;

public class SessionDataHealthCheck(ISessionService sessionService) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var topics = sessionService.GetAllTopics();
        var slides = sessionService.TotalSlides;

        if (topics.Count == 0 && slides == 0)
            return Task.FromResult(HealthCheckResult.Degraded(
                "No session data loaded. Check that data/sessions.json and data/slides.md exist " +
                "and that the Session:TopicsPath / Session:SlidesPath config values are correct."));

        var issues = new List<string>();
        if (topics.Count == 0) issues.Add("no topics loaded (data/sessions.json missing or empty)");
        if (slides == 0)       issues.Add("no slides loaded (data/slides.md missing or empty)");

        if (issues.Count > 0)
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Partial session data: {string.Join("; ", issues)}"));

        var active = topics.FirstOrDefault(t => t.Status == ConferenceAssistant.Core.Models.TopicStatus.Active);
        return Task.FromResult(HealthCheckResult.Healthy(
            $"{topics.Count} topics and {slides} slides loaded. " +
            $"Active topic: {active?.Title ?? "none"}"));
    }
}
