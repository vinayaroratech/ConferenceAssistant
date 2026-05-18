using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ConferenceAssistant.Core.Services;
using ConferenceAssistant.Ingestion;
using ConferenceAssistant.Ingestion.Pipelines;

namespace ConferenceAssistant.Agents.Tools;

public static class KnowledgeTools
{
    public static AIFunction CreateGetCurrentTopicTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var topic = sessionService.GetCurrentTopic();
                logger?.LogInformation("[Tool:get_current_topic] topicId={TopicId}", topic?.Id ?? "none");
                if (topic is null)
                    return "No active topic. The session has not started yet.";

                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Topic ID: {topic.Id}");
                sb.AppendLine($"Title: {topic.Title}");
                if (!string.IsNullOrWhiteSpace(topic.Description))
                    sb.AppendLine($"Description: {topic.Description}");
                if (topic.TalkingPoints.Count > 0)
                    sb.AppendLine($"Talking points:\n{string.Join("\n", topic.TalkingPoints.Select(p => $"  - {p}"))}");
                sb.AppendLine($"Status: {topic.Status}");
                return sb.ToString().TrimEnd();
            },
            "get_current_topic",
            "Get the currently active session topic, including its ID, title, description, and talking points. " +
            "Call this first to understand what is being discussed before creating a poll.");

    public static AIFunction CreateSearchKnowledgeTool(ISemanticSearchService searchService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            async (string query, int topK) =>
            {
                logger?.LogInformation("[Tool:search_knowledge] query={Query} topK={TopK}", query, topK > 0 ? topK : 5);
                var results = await searchService.SearchAsync(query, topK > 0 ? topK : 5);
                logger?.LogInformation("[Tool:search_knowledge] Returned {Count} results", results.Count);
                if (results.Count == 0) return "No relevant knowledge found.";

                return string.Join("\n\n---\n\n", results.Select(r =>
                    $"[{r.Source}] {(r.Summary ?? r.Content[..Math.Min(200, r.Content.Length)])}"));
            },
            "search_knowledge",
            "Search the session knowledge base for content relevant to a topic or question. " +
            "Call this before creating a poll or writing an analysis - search results are your " +
            "source of truth for grounding options and insights in actual session content.");

    public static AIFunction CreateIngestContentTool(ContentIngestionPipeline mcpPipeline, ILogger? logger = null)
        => AIFunctionFactory.Create(
            async (string content, string sourceUrl) =>
            {
                logger?.LogInformation("[Tool:ingest_content] Ingesting from {SourceUrl} ({Length} chars)", sourceUrl, content.Length);
                await mcpPipeline.IngestAsync(content, sourceUrl);
                logger?.LogInformation("[Tool:ingest_content] Ingested {SourceUrl}", sourceUrl);
                return $"Content from {sourceUrl} ingested into knowledge base.";
            },
            "ingest_content",
            "Ingest external content (e.g., from MCP) into the knowledge base.");

    public static AIFunction CreateGetAudienceQuestionsTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var questions = sessionService.GetQuestions();
                logger?.LogInformation("[Tool:get_audience_questions] Returning {Count} questions", questions.Count);
                if (questions.Count == 0) return "No audience questions submitted for this session.";

                var sb = new System.Text.StringBuilder();
                sb.Append($"Total questions: {questions.Count}\n\n");
                foreach (var q in questions.OrderByDescending(q => q.Upvotes))
                {
                    sb.Append($"Q (↑{q.Upvotes}): {q.Text}");
                    if (q.Answer != null) sb.Append($"\n  Answer: {q.Answer}");
                    sb.Append("\n");
                }
                return sb.ToString();
            },
            "get_audience_questions",
            "Get all audience questions submitted during the session, ordered by upvotes. Use this for session summarization and Q&A analysis.");

    public static AIFunction CreateIngestPollResponsesTool(
        IPollService pollService, ResponseIngestionPipeline responseIngestion, ILogger? logger = null)
        => AIFunctionFactory.Create(
            async (string pollId, CancellationToken ct) =>
            {
                logger?.LogInformation("[Tool:ingest_poll_responses] Ingesting responses for poll {PollId}", pollId);
                var poll = pollService.GetPoll(pollId);
                if (poll is null)
                {
                    logger?.LogWarning("[Tool:ingest_poll_responses] Poll {PollId} not found", pollId);
                    return $"Poll {pollId} not found - skipping ingestion.";
                }
                var tally = pollService.GetResultTally(pollId);
                await responseIngestion.IngestResponsesAsync(pollId, poll.Question, tally, ct);
                logger?.LogInformation("[Tool:ingest_poll_responses] Ingested responses for poll {PollId}", pollId);
                return $"Poll responses for {pollId} ingested into knowledge base.";
            },
            "ingest_poll_responses",
            "Ingest a poll's responses into the knowledge base. Call this before analysing poll results to ensure response data is available for search.");

    public static AIFunction CreateSaveQuestionAnswerTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            (string questionId, string answer) =>
            {
                logger?.LogInformation("[Tool:save_question_answer] Saving answer for question {QuestionId}", questionId);
                sessionService.AnswerQuestion(questionId, answer);
                logger?.LogInformation("[Tool:save_question_answer] Answer saved for question {QuestionId}", questionId);
                return $"Answer saved for question {questionId}.";
            },
            "save_question_answer",
            "Save an AI-generated answer to an audience question.");
}
