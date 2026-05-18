namespace ConferenceAssistant.Core.Services;

public interface ISessionSummaryService
{
    bool IsGenerating { get; }
    string? CurrentSummary { get; }
    event Action? Changed;
    void BeginStreaming();
    void AppendChunk(string chunk);
    void CompleteStreaming();
}
