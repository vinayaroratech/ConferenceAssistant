using System.Text;

namespace ConferenceAssistant.Core.Services;

public class SessionSummaryService : ISessionSummaryService
{
    private readonly StringBuilder _buffer = new();

    public bool IsGenerating { get; private set; }
    public string? CurrentSummary { get; private set; }

    public event Action? Changed;

    public void BeginStreaming()
    {
        IsGenerating = true;
        _buffer.Clear();
        Changed?.Invoke();
    }

    public void AppendChunk(string chunk)
    {
        _buffer.Append(chunk);
        CurrentSummary = _buffer.ToString();
        Changed?.Invoke();
    }

    public void CompleteStreaming()
    {
        CurrentSummary = _buffer.ToString();
        IsGenerating = false;
        Changed?.Invoke();
    }
}
