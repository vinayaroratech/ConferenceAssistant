using Microsoft.Extensions.Configuration;

namespace ConferenceAssistant.Core.Extensions;

/// <summary>
/// <see cref="IConfiguration"/> extension methods for reading AI provider settings.
/// Shared across ConferenceAssistant.Web, ConferenceAssistant.Ingestion, and other projects.
/// </summary>
public static class AiConfigurationExtensions
{
    /// <summary>Returns the normalized AI provider name ("github" or "ollama").</summary>
    public static string GetAiProvider(this IConfiguration cfg) =>
        (cfg["AI:Provider"] ?? "ollama").ToLowerInvariant();

    /// <summary>Returns true when the active AI provider is GitHub Models.</summary>
    public static bool IsGitHubAiProvider(this IConfiguration cfg) =>
        cfg.GetAiProvider() == "github";

    /// <summary>
    /// Returns the embedding vector dimension for the configured provider and model.
    /// GitHub Models text-embedding-3-small → 1536.
    /// Ollama nomic-embed-text → 768 (default).
    /// </summary>
    public static int GetAiEmbeddingDimension(this IConfiguration cfg)
    {
        if (cfg.IsGitHubAiProvider())
        {
            var model = cfg["AI:GitHub:EmbeddingModel"] ?? "text-embedding-3-small";
            return model.Contains("3-large", StringComparison.OrdinalIgnoreCase) ? 3072 : 1536;
        }
        else
        {
            // nomic-embed-text = 768; mxbai-embed-large = 1024; all-minilm = 384
            var model = cfg["AI:Ollama:EmbeddingModel"] ?? "nomic-embed-text";
            return model.ToLowerInvariant() switch
            {
                var m when m.Contains("mxbai")   => 1024,
                var m when m.Contains("minilm")  => 384,
                _                                => 768
            };
        }
    }
}
