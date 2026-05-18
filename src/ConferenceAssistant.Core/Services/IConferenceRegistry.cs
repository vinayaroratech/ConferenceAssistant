using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public interface IConferenceRegistry
{
    IReadOnlyList<Conference> All { get; }
    Conference? Default { get; }
    Conference? GetByCode(string code);
    Task LoadAsync(string jsonPath);
}
