using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ConferenceAssistant.Agents.Tools;
using ConferenceAssistant.Agents.Workflows;
using ConferenceAssistant.Core.Services;
using ConferenceAssistant.Ingestion.Pipelines;

namespace ConferenceAssistant.Agents;

public static class AgentsServiceCollectionExtensions
{
    public static IServiceCollection AddAgentServices(this IServiceCollection services)
    {
        services.AddSingleton<AgentTools>();
        services.AddSingleton<IAgentFactory, AgentFactory>();

        services.AddSingleton(sp =>
            new PollGenerationWorkflow(
                sp.GetRequiredService<IAgentFactory>(),
                sp.GetRequiredService<ILogger<PollGenerationWorkflow>>(),
                sp.GetService<IAgentActivityService>()));

        services.AddSingleton(sp =>
            new ResponseAnalysisWorkflow(
                sp.GetRequiredService<IAgentFactory>(),
                sp.GetRequiredService<ILogger<ResponseAnalysisWorkflow>>(),
                sp.GetService<IAgentActivityService>()));

        services.AddSingleton(sp =>
            new SessionSummaryWorkflow(
                sp.GetRequiredService<IAgentFactory>(),
                sp.GetService<ISessionSummaryService>(),
                sp.GetService<IAgentActivityService>(),
                sp.GetRequiredService<ILogger<SessionSummaryWorkflow>>()));

        services.AddSingleton(sp =>
            new QuestionAnswerWorkflow(
                sp.GetRequiredService<IAgentFactory>(),
                sp.GetRequiredService<ILogger<QuestionAnswerWorkflow>>(),
                sp.GetService<IAgentActivityService>()));

        return services;
    }
}
