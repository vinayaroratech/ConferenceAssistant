using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ConferenceAssistant.Core.Services;
using ConferenceAssistant.Ingestion;
using ConferenceAssistant.Ingestion.Pipelines;

namespace ConferenceAssistant.Agents.Tools;

/// <summary>
/// Aggregates all AI tool instances for the conference assistant agents.
/// Individual tools are exposed as properties so agents and workflows can compose
/// precise per-call tool sets via <see cref="ChatOptions.Tools"/>,
/// following the Microsoft Agents Framework pattern for tool scoping.
/// </summary>
public class AgentTools
{
    // Poll tools
    public AIFunction CreatePoll { get; }
    public AIFunction ClosePoll { get; }
    public AIFunction GetPollResults { get; }
    public AIFunction GetAllPollResults { get; }

    // Insight tools
    public AIFunction StoreInsight { get; }
    public AIFunction GetInsights { get; }
    public AIFunction GetAllInsights { get; }

    // Knowledge tools
    public AIFunction GetCurrentTopic { get; }
    public AIFunction SearchKnowledge { get; }
    public AIFunction IngestContent { get; }
    public AIFunction IngestPollResponses { get; }
    public AIFunction GetAudienceQuestions { get; }
    public AIFunction SaveQuestionAnswer { get; }

    // Summary utility (not registered to any agent - invoked directly by SessionSummaryWorkflow)
    public AIFunction GetSessionSummaryData { get; }

    public AgentTools(
        IPollService pollService,
        ISessionService sessionService,
        ISemanticSearchService searchService,
        ContentIngestionPipeline ingestionPipeline,
        ResponseIngestionPipeline responseIngestionPipeline,
        ILoggerFactory? loggerFactory = null)
    {
        var logger = (loggerFactory ?? NullLoggerFactory.Instance)
            .CreateLogger("ConferenceAssistant.Agents.Tools");

        CreatePoll            = PollTools.CreateCreatePollTool(pollService, logger);
        ClosePoll             = PollTools.CreateClosePollTool(pollService, logger);
        GetPollResults        = PollTools.CreateGetPollResultsTool(pollService, logger);
        GetAllPollResults     = PollTools.CreateGetAllPollResultsTool(pollService, logger);
        StoreInsight          = InsightTools.CreateStoreInsightTool(sessionService, logger);
        GetInsights           = InsightTools.CreateGetInsightsTool(sessionService, logger);
        GetAllInsights        = InsightTools.CreateGetAllInsightsTool(sessionService, logger);
        SearchKnowledge       = KnowledgeTools.CreateSearchKnowledgeTool(searchService, logger);
        GetCurrentTopic       = KnowledgeTools.CreateGetCurrentTopicTool(sessionService, logger);
        IngestContent         = KnowledgeTools.CreateIngestContentTool(ingestionPipeline, logger);
        IngestPollResponses   = KnowledgeTools.CreateIngestPollResponsesTool(pollService, responseIngestionPipeline, logger);
        GetAudienceQuestions  = KnowledgeTools.CreateGetAudienceQuestionsTool(sessionService, logger);
        SaveQuestionAnswer    = KnowledgeTools.CreateSaveQuestionAnswerTool(sessionService, logger);
        GetSessionSummaryData = InsightTools.CreateGetSessionSummaryDataTool(sessionService, pollService, logger);
    }
}
