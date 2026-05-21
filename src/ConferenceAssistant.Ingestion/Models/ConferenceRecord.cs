namespace ConferenceAssistant.Ingestion.Models;

// Schema is defined programmatically in DependencyInjection.cs via VectorStoreCollectionDefinition
// so the embedding dimension can be read from appsettings at runtime. No VectorStore* attributes needed.
public class ConferenceRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Content { get; set; } = "";
    public string Source { get; set; } = "";  // "outline", "response", "question", "mcp-doc"
    public string TopicId { get; set; } = "";
    public string? Summary { get; set; }
    public List<string> Keywords { get; set; } = [];
    public string? Sentiment { get; set; }
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
    public ReadOnlyMemory<float> Embedding { get; set; }
}
