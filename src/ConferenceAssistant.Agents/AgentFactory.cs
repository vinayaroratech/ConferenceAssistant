using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ConferenceAssistant.Agents.Tools;

namespace ConferenceAssistant.Agents;

/// <summary>
/// Creates named <see cref="ChatClientAgent"/> instances pre-configured with their canonical
/// system instructions and tool sets. Workflows call these factory methods instead of
/// constructing agents inline, keeping agent definitions in one authoritative location.
/// </summary>
public class AgentFactory(IChatClient chatClient, AgentTools tools) : IAgentFactory
{
    public ChatClientAgent CreateSurveyArchitect() =>
        new(chatClient,
            AgentInstructions.SurveyArchitectInstructions,
            AgentInstructions.SurveyArchitectName,
            tools: [tools.GetCurrentTopic, tools.SearchKnowledge, tools.GetAudienceQuestions,
                    tools.GetAllPollResults, tools.GetAllInsights, tools.CreatePoll]);

    public ChatClientAgent CreateResponseAnalyst() =>
        new(chatClient,
            AgentInstructions.ResponseAnalystInstructions,
            AgentInstructions.ResponseAnalystName,
            tools: [tools.GetPollResults, tools.SearchKnowledge, tools.GetInsights, tools.StoreInsight]);

    public ChatClientAgent CreateKnowledgeCurator() =>
        new(chatClient,
            AgentInstructions.KnowledgeCuratorInstructions,
            AgentInstructions.KnowledgeCuratorName,
            tools: [tools.SearchKnowledge, tools.SaveQuestionAnswer]);

    public ChatClientAgent CreatePollAnalyst() =>
        new(chatClient,
            AgentInstructions.PollAnalystInstructions,
            AgentInstructions.PollAnalystName,
            AgentInstructions.PollAnalystDescription,
            [tools.GetAllPollResults]);

    public ChatClientAgent CreateQuestionAnalyst() =>
        new(chatClient,
            AgentInstructions.QuestionAnalystInstructions,
            AgentInstructions.QuestionAnalystName,
            AgentInstructions.QuestionAnalystDescription,
            [tools.GetAudienceQuestions]);

    public ChatClientAgent CreateInsightAnalyst() =>
        new(chatClient,
            AgentInstructions.InsightAnalystInstructions,
            AgentInstructions.InsightAnalystName,
            AgentInstructions.InsightAnalystDescription,
            [tools.GetAllInsights, tools.SearchKnowledge]);

    /// <summary>
    /// Creates the synthesizer agent that orchestrates <see cref="CreatePollAnalyst"/>,
    /// <see cref="CreateQuestionAnalyst"/>, and <see cref="CreateInsightAnalyst"/> as
    /// function tools, following the Microsoft Agent Framework "agent as function tool" pattern.
    /// See https://learn.microsoft.com/en-us/agent-framework/agents/tools/
    /// </summary>
    public ChatClientAgent CreateSynthesizer() =>
        new(chatClient,
            AgentInstructions.SynthesizerInstructions,
            AgentInstructions.SynthesizerName,
            tools: [CreatePollAnalyst().AsAIFunction(),
                    CreateQuestionAnalyst().AsAIFunction(),
                    CreateInsightAnalyst().AsAIFunction()]);
}
