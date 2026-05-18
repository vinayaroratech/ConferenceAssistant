using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public interface IPollService
{
    void Reset();
    Poll CreatePoll(string? topicId, string question, List<string> options);
    Poll? GetPoll(string pollId);
    IReadOnlyList<Poll> GetActivePolls();
    IReadOnlyList<Poll> GetAllPolls();
    void LaunchPoll(string pollId);
    void ClosePoll(string pollId);
    void ReopenPoll(string pollId);
    PollResponse SubmitResponse(string pollId, string selectedOption, string? attendeeId = null);
    IReadOnlyList<PollResponse> GetResponses(string pollId);
    Dictionary<string, int> GetResultTally(string pollId);
}
