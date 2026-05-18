using System.ComponentModel;
using ModelContextProtocol.Server;
using ConferenceAssistant.Core.Models;
using ConferenceAssistant.Core.Services;
using ConferenceAssistant.Ingestion;
using ConferenceAssistant.Ingestion.Pipelines;
using ConferenceAssistant.Agents.Workflows;

namespace ConferenceAssistant.Mcp.Server;

[McpServerToolType]
public class ConferenceTools(
    IPollService pollService,
    ISessionService sessionService,
    ISemanticSearchService searchService,
    SessionSummaryWorkflow summaryWorkflow,
    QuestionAnswerWorkflow questionAnswerWorkflow,
    ContentIngestionPipeline ingestionPipeline)
{
    [McpServerTool, Description("Get the current session status including active topic and all poll statuses")]
    public object GetSessionStatus()
    {
        var topic = sessionService.GetCurrentTopic();
        var topics = sessionService.GetAllTopics();
        return new
        {
            SessionCode = sessionService.SessionCode,
            CurrentTopic = topic?.Title,
            Topics = topics.Select(t => new { t.Title, t.Status }).ToList()
        };
    }

    [McpServerTool, Description("Get the currently active poll with question, options, and live vote counts")]
    public object? GetActivePoll()
    {
        var polls = pollService.GetActivePolls();
        var poll = polls.FirstOrDefault();
        if (poll is null) return null;

        var tally = pollService.GetResultTally(poll.Id);
        return new
        {
            poll.Id,
            poll.Question,
            poll.Options,
            Results = tally,
            TotalVotes = tally.Values.Sum()
        };
    }

    [McpServerTool, Description("Get detailed results for a specific poll by ID")]
    public object GetPollResults(string pollId)
    {
        var poll = pollService.GetPoll(pollId);
        if (poll is null) return new { error = "Poll not found" };

        var tally = pollService.GetResultTally(pollId);
        var insights = sessionService.GetInsightsForPoll(pollId);

        return new
        {
            poll.Question,
            poll.Status,
            Results = tally,
            TotalVotes = tally.Values.Sum(),
            Insights = insights.Select(i => i.Content).ToList()
        };
    }

    [McpServerTool, Description("Semantic search over all session knowledge including outline, responses, and ingested docs")]
    public async Task<IReadOnlyList<object>> SearchSessionKnowledge(string query, int topK = 5)
    {
        var results = await searchService.SearchAsync(query, topK);
        return results.Select(r => new
        {
            r.Content,
            r.Source,
            r.Summary,
            r.Keywords
        } as object).ToList();
    }

    [McpServerTool, Description("Get top audience questions ordered by upvotes")]
    public IReadOnlyList<object> GetAudienceQuestions(int topK = 10)
    {
        var questions = sessionService.GetQuestions().Take(topK);
        return questions.Select(q => new
        {
            q.Id,
            q.Text,
            q.Upvotes,
            q.Answer
        } as object).ToList();
    }

    [McpServerTool, Description("Get all AI-generated insights grouped by type")]
    public IReadOnlyList<object> GetAllInsights()
    {
        return sessionService.GetInsights().Select(i => new
        {
            i.Type,
            i.Content,
            i.GeneratedAt
        } as object).ToList();
    }

    [McpServerTool, Description("Generate a comprehensive session summary - triggers multi-agent workflow")]
    public async Task<string> GenerateSessionSummary()
    {
        return await summaryWorkflow.GenerateSummaryAsync();
    }

    [McpServerTool, Description("Get the full session outline: all topics and their slides")]
    public object GetOutline()
    {
        var topics = sessionService.GetAllTopics();
        return topics.Select(t => new
        {
            t.Title,
            t.Description,
            t.Status,
            Slides = sessionService.GetSlidesForTopic(t.Id).Select(s => new
            {
                s.Title,
                s.Type,
                s.SpeakerNotes
            }).ToList()
        }).ToList();
    }

    [McpServerTool, Description("Get all AI-generated insights as formatted markdown (conference://insights)")]
    public string GetInsightsMarkdown()
    {
        var insights = sessionService.GetInsights();
        if (!insights.Any()) return "No insights yet.";

        return string.Join("\n\n", insights.Select(i =>
            $"### {i.Type} - {i.GeneratedAt:HH:mm}\n{i.Content}"));
    }

    [McpServerTool, Description("Answer an audience question using the session knowledge base. Provide the questionId and questionText.")]
    public async Task<string> AnswerAudienceQuestion(string questionId, string questionText)
    {
        return await questionAnswerWorkflow.ExecuteAsync(questionId, questionText);
    }

    [McpServerTool, Description(
        "Ingest external content (markdown or plain text) into the session knowledge base. " +
        "The content is chunked, summarised by AI, embedded, and stored for semantic search. " +
        "Use sourceUrl to identify the origin (e.g. a Microsoft Learn page URL).")]
    public async Task<string> IngestContent(string content, string sourceUrl)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Error: content is required.";
        if (string.IsNullOrWhiteSpace(sourceUrl))
            return "Error: sourceUrl is required.";

        await ingestionPipeline.IngestAsync(content, sourceUrl);
        return $"Content from '{sourceUrl}' ingested successfully into the knowledge base.";
    }
}
