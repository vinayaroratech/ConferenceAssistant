using Microsoft.Agents.AI;

namespace ConferenceAssistant.Agents;

/// <summary>
/// Factory that creates each named specialist agent with its canonical instructions and tool set.
/// Centralising agent construction here ensures every workflow and test references the same
/// agent definitions, following the Microsoft Agent Framework composition pattern.
/// </summary>
public interface IAgentFactory
{
    // ── Workflow agents ───────────────────────────────────────────────────────
    ChatClientAgent CreateSurveyArchitect();
    ChatClientAgent CreateResponseAnalyst();
    ChatClientAgent CreateKnowledgeCurator();

    // ── SessionSummary sub-agents (composed into Synthesizer) ─────────────────
    ChatClientAgent CreatePollAnalyst();
    ChatClientAgent CreateQuestionAnalyst();
    ChatClientAgent CreateInsightAnalyst();
    ChatClientAgent CreateSynthesizer();
}
