using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Tools;

public static class PollTools
{
    public static AIFunction CreateCreatePollTool(IPollService pollService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            (string? topicId, string question, string options) =>
            {
                logger?.LogInformation("[Tool:create_poll] question={Question} options={Options}", question, options);
                var optionList = options
                    .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                    .ToList();
                var poll = pollService.CreatePoll(topicId, question, optionList);
                logger?.LogInformation("[Tool:create_poll] Created poll {PollId}", poll.Id);
                return $"Poll created with ID: {poll.Id}. Question: {poll.Question}. Options: {string.Join(", ", poll.Options)}";
            },
            "create_poll",
            "Creates and persists a new poll in the session. " +
            "This is the ONLY way to save a poll - describing a poll in text is not sufficient. " +
            "Call this after deciding on your question and options. " +
            "'options' must be a comma-separated string of answer choices. Returns the poll ID.");

    public static AIFunction CreateGetPollResultsTool(IPollService pollService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            (string pollId) =>
            {
                logger?.LogInformation("[Tool:get_poll_results] pollId={PollId}", pollId);
                var poll = pollService.GetPoll(pollId);
                if (poll is null)
                {
                    logger?.LogWarning("[Tool:get_poll_results] Poll {PollId} not found", pollId);
                    return "Poll not found.";
                }
                var tally = pollService.GetResultTally(pollId);
                var total = tally.Values.Sum();
                logger?.LogInformation("[Tool:get_poll_results] Poll {PollId} - {Total} total votes", pollId, total);
                if (total == 0)
                    return $"Poll: {poll.Question}\nTotal votes: 0\nNO_RESPONSES_YET - no audience members have voted on this poll. Do not fabricate any vote counts or percentages.";
                var results = string.Join("\n", tally.Select(kv =>
                    $"- {kv.Key}: {kv.Value} votes ({kv.Value * 100 / total}%)"));
                var optionNames = string.Join(", ", tally.Keys.Select(k => $"\"{k}\""));
                return $"Poll: {poll.Question}\nTotal votes: {total}\n{results}\n" +
                       $"OPTION NAMES: {optionNames}\n" +
                       $"RULE: Always refer to options by their EXACT text above. " +
                       $"NEVER write \"Option A\", \"Option B\", \"Option C\", or any other letter label. " +
                       $"There are exactly {tally.Count} options - do not invent additional ones.";
            },
            "get_poll_results",
            "Retrieve the exact vote counts, percentages, and option texts for a specific poll. " +
            "ALWAYS call this before writing any analysis - it is your only source of real poll data. " +
            "Never assume or invent vote data; always retrieve it first.");

    public static AIFunction CreateClosePollTool(IPollService pollService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            (string pollId) =>
            {
                logger?.LogInformation("[Tool:close_poll] Closing poll {PollId}", pollId);
                pollService.ClosePoll(pollId);
                logger?.LogInformation("[Tool:close_poll] Poll {PollId} closed", pollId);
                return $"Poll {pollId} closed.";
            },
            "close_poll",
            "Close an active poll to stop further voting.");

    public static AIFunction CreateGetAllPollResultsTool(IPollService pollService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var polls = pollService.GetAllPolls();
                logger?.LogInformation("[Tool:get_all_poll_results] Returning results for {Count} polls", polls.Count);
                if (polls.Count == 0) return "No polls have been run in this session.";

                var sb = new System.Text.StringBuilder();
                foreach (var poll in polls)
                {
                    var tally = pollService.GetResultTally(poll.Id);
                    var total = tally.Values.Sum();
                    sb.Append($"\n### {poll.Question} (ID: {poll.Id})\n");
                    if (total == 0)
                    {
                        sb.Append("  No votes cast.\n");
                    }
                    else
                    {
                        foreach (var (option, count) in tally)
                            sb.Append($"  - {option}: {count} votes ({count * 100 / total}%)\n");
                    }
                }
                return sb.ToString();
            },
            "get_all_poll_results",
            "Get the results of all polls run in this session. Use this for session-level analysis and summarization.");
}
