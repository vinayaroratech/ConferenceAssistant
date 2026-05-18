using System.Collections.Concurrent;

namespace ConferenceAssistant.Core.Services;

public class AgentActivity
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public string AgentName { get; init; } = "";
    public string Message { get; init; } = "";
}

public class AgentActivityService : IAgentActivityService
{
    private const int MaxEntries = 50;
    private readonly ConcurrentQueue<AgentActivity> _entries = new();

    public event Action? Changed;

    public void Log(string agentName, string message)
    {
        _entries.Enqueue(new AgentActivity { AgentName = agentName, Message = message });
        while (_entries.Count > MaxEntries)
            _entries.TryDequeue(out _);
        Changed?.Invoke();
    }

    public IReadOnlyList<AgentActivity> GetRecent(int count)
        => _entries.Reverse().Take(count).ToList();
}
