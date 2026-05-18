using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConferenceAssistant.Web.HealthChecks;

public static class HealthStatusWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static async Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;

        var result = new
        {
            status = report.Status.ToString(),
            timestamp = DateTimeOffset.UtcNow,
            duration = report.TotalDuration.TotalMilliseconds + "ms",
            components = report.Entries.ToDictionary(
                e => e.Key,
                e => (object)new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds + "ms",
                    error = e.Value.Exception?.Message
                })
        };

        var json = JsonSerializer.Serialize(result, JsonOptions);
        await context.Response.WriteAsync(json, Encoding.UTF8);
    }
}
