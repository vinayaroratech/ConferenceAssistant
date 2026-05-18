using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ConferenceAssistant.Web.HealthChecks;

public class QdrantHealthCheck(IConfiguration configuration, IHttpClientFactory httpClientFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        // Qdrant exposes two ports: HTTP REST on 6333, gRPC on 6334.
        // VectorStore:QdrantEndpoint is the gRPC endpoint used by the vector store client.
        // The health check must use the HTTP REST endpoint (6333) for /healthz.
        // VectorStore:QdrantHttpEndpoint is a separate key for this purpose.
        var endpoint = configuration["VectorStore:QdrantHttpEndpoint"] ?? "http://localhost:6333";

        try
        {
            var client = httpClientFactory.CreateClient("health");
            var response = await client.GetAsync($"{endpoint.TrimEnd('/')}/healthz", ct);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"Qdrant is healthy at {endpoint}");

            return HealthCheckResult.Degraded(
                $"Qdrant returned HTTP {(int)response.StatusCode} from {endpoint}. " +
                "The app is running with the InMemory vector store - Qdrant is optional for development.");
        }
        catch (Exception ex)
        {
            // Qdrant is not required in development (InMemory is used).
            // Report Degraded rather than Unhealthy so the app stays green locally without Docker.
            return HealthCheckResult.Degraded(
                $"Qdrant is unreachable at {endpoint}. " +
                "The app is using the InMemory vector store and will function normally. " +
                $"Error: {ex.Message}");
        }
    }
}
