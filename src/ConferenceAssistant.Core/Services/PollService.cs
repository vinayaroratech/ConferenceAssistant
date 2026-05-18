using ConferenceAssistant.Core.Domain;
using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public class PollService(DomainEventDispatcher dispatcher) : IPollService
{
    private readonly InMemoryStore<Poll> _polls = new();
    private readonly InMemoryStore<PollResponse> _responses = new();
    // key: "pollId:voterId" - prevents one connection from voting twice on the same poll
    private readonly HashSet<string> _voteKeys = new();

    public void Reset()
    {
        _polls.Clear();
        _responses.Clear();
        _voteKeys.Clear();
    }

    public Poll CreatePoll(string? topicId, string question, List<string> options)
    {
        var poll = Poll.Create(topicId, question, options);
        _polls.Add(poll.Id, poll);
        dispatcher.DispatchAndClear(poll);
        return poll;
    }

    public Poll? GetPoll(string pollId) => _polls.Get(pollId);

    public IReadOnlyList<Poll> GetActivePolls()
        => _polls.Query(p => p.Status == PollStatus.Active);

    public IReadOnlyList<Poll> GetAllPolls() => _polls.GetAll();

    public void LaunchPoll(string pollId)
    {
        var poll = _polls.Get(pollId)
            ?? throw new InvalidOperationException($"Poll {pollId} not found");
        poll.Launch();
        dispatcher.DispatchAndClear(poll);
    }

    public void ClosePoll(string pollId)
    {
        var poll = _polls.Get(pollId)
            ?? throw new InvalidOperationException($"Poll {pollId} not found");
        poll.Close();
        dispatcher.DispatchAndClear(poll);
    }

    public void ReopenPoll(string pollId)
    {
        var poll = _polls.Get(pollId)
            ?? throw new InvalidOperationException($"Poll {pollId} not found");
        poll.Reopen();
        dispatcher.DispatchAndClear(poll);
    }

    public PollResponse SubmitResponse(string pollId, string selectedOption, string? attendeeId = null)
    {
        var poll = _polls.Get(pollId)
            ?? throw new InvalidOperationException($"Poll {pollId} not found");
        if (poll.Status != PollStatus.Active)
            throw new InvalidOperationException($"Poll {pollId} is not active");

        // Deduplicate by voter - silently ignore repeat votes
        if (attendeeId is not null)
        {
            var key = $"{pollId}:{attendeeId}";
            lock (_voteKeys)
            {
                if (!_voteKeys.Add(key))
                    throw new InvalidOperationException("Already voted on this poll");
            }
        }

        var response = PollResponse.Cast(pollId, selectedOption, attendeeId);
        _responses.Add(response.Id, response);
        return response;
    }

    public IReadOnlyList<PollResponse> GetResponses(string pollId)
        => _responses.Query(r => r.PollId == pollId);

    public Dictionary<string, int> GetResultTally(string pollId)
    {
        var poll = _polls.Get(pollId);
        if (poll is null) return new();

        var responses = GetResponses(pollId);
        return poll.Options.ToDictionary(
            option => option,
            option => responses.Count(r => r.SelectedOption == option)
        );
    }
}
