using Microsoft.Extensions.DependencyInjection;

namespace ConferenceAssistant.Mcp;

public static class McpServiceCollectionExtensions
{
    public static IServiceCollection AddMcpServices(this IServiceCollection services)
    {
        // MCP server tools are registered via AddMcpServer() in Program.cs
        // MCP clients are created on-demand (see Clients/McpClientFactory.cs)
        return services;
    }
}
