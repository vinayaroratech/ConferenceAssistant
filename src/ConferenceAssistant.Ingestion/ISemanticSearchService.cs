using ConferenceAssistant.Ingestion.Models;

namespace ConferenceAssistant.Ingestion;

public interface ISemanticSearchService
{
    Task<IReadOnlyList<ConferenceRecord>> SearchAsync(string query, int topK = 5, CancellationToken ct = default);
    Task<IReadOnlyList<ConferenceRecord>> SearchBySourceAsync(string query, string source, int topK = 5, CancellationToken ct = default);
    Task<IReadOnlyList<ConferenceRecord>> GetAllAsync(int max = 200, CancellationToken ct = default);
}
