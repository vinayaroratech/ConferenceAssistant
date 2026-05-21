using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConferenceAssistant.Web.HealthChecks;

public class OllamaHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        var endpoint   = configuration["AI:Ollama:Endpoint"]      ?? "http://localhost:11434";
        var chatModel  = configuration["AI:Ollama:ChatModel"]     ?? "llama3.2";
        var embedModel = configuration["AI:Ollama:EmbeddingModel"] ?? "nomic-embed-text";

        try
        {
            // Two-step check:
            // 1. Fast ping to / - confirms the process is up (returns "Ollama is running")
            // 2. /api/tags - lists pulled models (can be slow on first load, uses longer timeout)
            var client = httpClientFactory.CreateClient("ollama-health");

            var ping = await client.GetAsync($"{endpoint.TrimEnd('/')}/", ct);
            if (!ping.IsSuccessStatusCode)
                return HealthCheckResult.Unhealthy(
                    $"Ollama root endpoint returned HTTP {(int)ping.StatusCode} at {endpoint}.");

            var response = await client.GetAsync($"{endpoint.TrimEnd('/')}/api/tags", ct);
            response.EnsureSuccessStatusCode();

            var tags = await response.Content.ReadFromJsonAsync<OllamaTagsResponse>(ct);
            var modelNames = tags?.Models?.Select(m => m.Name).ToList() ?? [];

            var chatAvailable  = modelNames.Any(n => n.Contains(chatModel.Split(':')[0],  StringComparison.OrdinalIgnoreCase));
            var embedAvailable = modelNames.Any(n => n.Contains(embedModel.Split(':')[0], StringComparison.OrdinalIgnoreCase));

            if (chatAvailable && embedAvailable)
                return HealthCheckResult.Healthy($"Models ready: {chatModel}, {embedModel}");

            var missing = new List<string>();
            if (!chatAvailable)  missing.Add(chatModel);
            if (!embedAvailable) missing.Add(embedModel);

            return HealthCheckResult.Degraded(
                $"Ollama is reachable but these models are not yet pulled: {string.Join(", ", missing)}. " +
                $"Run: ollama pull {string.Join(" && ollama pull ", missing)}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Ollama is unreachable at {endpoint}. Ensure Docker is running and Ollama container is up. " +
                $"Error: {ex.Message}");
        }
    }

    private record OllamaTagsResponse([property: JsonPropertyName("models")] List<OllamaModel>? Models);
    private record OllamaModel([property: JsonPropertyName("name")] string Name);
}
