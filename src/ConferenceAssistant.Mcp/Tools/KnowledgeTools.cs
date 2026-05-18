using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using ConferenceAssistant.Ingestion;

namespace ConferenceAssistant.Mcp.Tools;

[McpServerToolType]
public class KnowledgeTools(ISemanticSearchService searchService)
{
    [McpServerTool, Description("Search the knowledge base by query (returns top 5 matches)")]
    public async Task<string> SearchKnowledge(string query, int topK = 5)
    {
        var results = await searchService.SearchAsync(query, topK);
        if (results.Count == 0) return "No results found.";

        return string.Join("\n\n---\n\n", results.Select(r =>
            $"Source: {r.Source}\n{r.Summary ?? r.Content[..Math.Min(300, r.Content.Length)]}"));
    }

    [McpServerTool, Description("Get knowledge base record count statistics")]
    public string GetKnowledgeStats()
    {
        return JsonSerializer.Serialize(new { Message = "Knowledge base stats available via search." });
    }
}
