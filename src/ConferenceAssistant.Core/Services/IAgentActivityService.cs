namespace ConferenceAssistant.Core.Services;

public interface IAgentActivityService
{
    event Action? Changed;
    void Log(string agentName, string message);
    IReadOnlyList<AgentActivity> GetRecent(int count);
}
