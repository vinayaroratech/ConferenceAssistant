using System.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using ConferenceAssistant.Core.Models;
using ConferenceAssistant.Core.Services;

namespace ConferenceAssistant.Agents.Tools;

public static class InsightTools
{
    public static AIFunction CreateStoreInsightTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            (
                [Description("The exact poll ID string that was analysed")] string pollId,
                [Description("Plain-text analysis paragraph (2-4 sentences). Quote actual vote counts and percentages. No JSON, no curly braces.")] string content,
                [Description("Insight category: PollAnalysis, AudienceTrend, KnowledgeGap, or Summary")] string type) =>
            {
                logger?.LogInformation("[Tool:store_insight] pollId={PollId} type={Type}", pollId, type);
                var insightType = Enum.TryParse<InsightType>(type, true, out var t) ? t : InsightType.PollAnalysis;
                var insight = Insight.Create(pollId, content, insightType);
                sessionService.AddInsight(insight);
                logger?.LogInformation("[Tool:store_insight] Stored insight {InsightId}", insight.Id);
                return $"Insight stored: {insight.Id}";
            },
            "store_insight",
            """
            Store an AI-generated insight about a poll.
            - pollId: the poll ID that was analysed.
            - content: a plain-text paragraph (2-4 sentences) containing your human-readable analysis.
                       You MUST quote the actual vote counts and percentages from the poll results.
                       Do NOT copy any example text. Do NOT pass JSON or curly braces.
            - type: one of PollAnalysis, AudienceTrend, KnowledgeGap, Summary.
            """);

    public static AIFunction CreateGetInsightsTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var insights = sessionService.GetInsights();
                logger?.LogInformation("[Tool:get_insights] Returning {Count} insights", insights.Count);
                if (insights.Count == 0) return "No insights generated yet.";
                return string.Join("\n\n", insights.Select(i => $"[{i.Type}] Poll:{i.PollId} \u2014 {i.Content}"));
            },
            "get_insights",
            "Retrieve all AI-generated insights from the current session. " +
            "Call this before storing a new insight to check whether this poll has already been analysed. " +
            "Do not store a duplicate if an insight for this poll ID already exists.");

    public static AIFunction CreateGetAllInsightsTool(ISessionService sessionService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var insights = sessionService.GetInsights();
                logger?.LogInformation("[Tool:get_all_insights] Returning {Count} insights for summary", insights.Count);
                if (insights.Count == 0) return "No insights generated yet for this session.";

                var sb = new System.Text.StringBuilder();
                sb.Append($"Total insights: {insights.Count}\n\n");
                foreach (var i in insights)
                    sb.Append($"[{i.Type}] Poll:{i.PollId} - {i.Content}\n");
                return sb.ToString();
            },
            "get_all_insights",
            "Get all AI-generated insights for session summarization. Returns all insights with their types and poll associations.");

    public static AIFunction CreateGetSessionSummaryDataTool(
        ISessionService sessionService, IPollService pollService, ILogger? logger = null)
        => AIFunctionFactory.Create(
            () =>
            {
                var topics = sessionService.GetAllTopics();
                var insights = sessionService.GetInsights();
                var questions = sessionService.GetQuestions();
                var polls = pollService.GetAllPolls();

                logger?.LogInformation(
                    "[Tool:get_session_summary_data] topics={Topics} polls={Polls} insights={Insights} questions={Questions}",
                    topics.Count(), polls.Count, insights.Count, questions.Count);

                var sb = new System.Text.StringBuilder();
                sb.Append($"Session: {sessionService.SessionCode}\n");
                sb.Append($"Topics covered: {topics.Count(t => t.Status == TopicStatus.Completed)}/{topics.Count}\n");
                sb.Append($"Polls run: {polls.Count}\n");
                sb.Append($"Questions asked: {questions.Count}\n");
                sb.Append($"Insights generated: {insights.Count}\n\n");

                sb.Append("## Poll Results\n");
                foreach (var poll in polls)
                {
                    var tally = pollService.GetResultTally(poll.Id);
                    sb.Append($"\n### {poll.Question}\n");
                    foreach (var (option, count) in tally)
                        sb.Append($"- {option}: {count}\n");
                }

                sb.Append("\n## Insights\n");
                foreach (var insight in insights)
                    sb.Append($"- [{insight.Type}] {insight.Content}\n");

                sb.Append("\n## Top Questions\n");
                foreach (var q in questions.Take(10))
                    sb.Append($"- {q.Text} (↑{q.Upvotes}){(q.Answer != null ? $" → {q.Answer}" : "")}\n");

                return sb.ToString();
            },
            "get_session_summary_data",
            "Get comprehensive session data including polls, insights, and questions.");
}
