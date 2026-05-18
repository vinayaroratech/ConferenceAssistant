using Microsoft.Extensions.VectorData;

namespace ConferenceAssistant.Ingestion.Models;

public class ConferenceRecord
{
    [VectorStoreKey]
    public Guid Id { get; set; } = Guid.NewGuid();

    [VectorStoreData]
    public string Content { get; set; } = "";

    [VectorStoreData]
    public string Source { get; set; } = "";  // "outline", "response", "question", "mcp-doc"

    [VectorStoreData]
    public string TopicId { get; set; } = "";

    [VectorStoreData]
    public string? Summary { get; set; }

    [VectorStoreData]
    public List<string> Keywords { get; set; } = [];

    [VectorStoreData]
    public string? Sentiment { get; set; }

    [VectorStoreData]
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;

    // 768 dimensions for nomic-embed-text (Ollama default embedding model)
    [VectorStoreVector(768)]
    public ReadOnlyMemory<float> Embedding { get; set; }
}
