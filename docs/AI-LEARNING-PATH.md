# AI Agents & Microsoft Agent Framework - Learning Path

Official Microsoft Learn links ordered from beginner to advanced.

---

## Stage 1 - Foundations (Start Here)

1. [What are AI agents?](https://learn.microsoft.com/dotnet/ai/conceptual/agents)
   *Core concept - what an agent is vs. a plain LLM call; how .NET approaches agents.*

2. [Agent architecture components](https://learn.microsoft.com/microsoft-copilot-studio/guidance/architecture/components-of-agent-architecture)
   *The five building blocks every agent is made of: client, orchestrator, LLM, tools, semantic index.*

3. [Microsoft.Extensions.AI overview](https://learn.microsoft.com/dotnet/ai/ai-extensions-overview)
   *The `IChatClient` abstraction - vendor-agnostic interface for all LLM providers.*

4. [Microsoft Agent Framework overview](https://learn.microsoft.com/agent-framework/overview/agent-framework-overview)
   *What the framework is, why it supersedes Semantic Kernel + AutoGen, when to use it.*

---

## Stage 2 - Building Your First Agent

5. [Tool calling with Microsoft.Extensions.AI](https://learn.microsoft.com/dotnet/ai/conceptual/tools)
   *How agents invoke .NET methods as tools using `AIFunctionFactory`.*

6. [Agent sessions and state management](https://learn.microsoft.com/agent-framework/agents/conversations/session)
   *How agents maintain conversation state across multiple turns.*

7. [Agent pipeline architecture](https://learn.microsoft.com/agent-framework/agents/agent-pipeline)
   *How an agent processes a request step-by-step; where to hook in middleware.*

8. [Agent middleware](https://learn.microsoft.com/agent-framework/agents/middleware/)
   *Intercept, log, or transform agent input/output using the middleware pattern.*

---

## Stage 3 - Multi-Agent Workflows

9. [Sequential orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/sequential)
   *Chain agents in order - each receives the previous agent's output. Easiest workflow pattern.*

10. [Concurrent orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/concurrent)
    *Run multiple agents in parallel on the same input; aggregate results (fan-out / fan-in).*

11. [Handoff orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/handoff)
    *Agents dynamically delegate to specialist agents - routing and escalation patterns.*

12. [Group chat orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/group-chat)
    *Collaborative conversation between agents with a shared thread; maker-checker loops.*

13. [Magentic orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/magentic)
    *LLM-driven dynamic planning - the orchestrator decides which agents to involve at runtime.*

---

## Stage 4 - Architecture & Design Patterns

14. [AI agent orchestration patterns](https://learn.microsoft.com/azure/architecture/ai-ml/guide/ai-agent-design-patterns)
    *Reference architecture: sequential, concurrent, group chat, handoff, magentic - with diagrams and when-to-use guidance.*

15. [AI architecture design](https://learn.microsoft.com/azure/architecture/ai-ml/)
    *RAG, agent-based architecture, and design decisions for AI systems on Azure.*

16. [Data architecture for AI agents](https://learn.microsoft.com/azure/cloud-adoption-framework/ai-agents/data-architecture-plan)
    *How to connect agents to governed enterprise data; MCP server planning.*

17. [Baseline Microsoft Foundry chat reference architecture](https://learn.microsoft.com/azure/architecture/ai-ml/architecture/baseline-microsoft-foundry-chat)
    *Production-grade reference architecture for chat agents on Azure.*

---

## Stage 5 - Model Context Protocol (MCP)

18. [Model Context Protocol - what it is](https://learn.microsoft.com/azure/ai-foundry/agents/concepts/model-context-protocol)
    *The open standard for connecting agents to external tools and data sources.*

19. [Connect agents to MCP servers](https://learn.microsoft.com/azure/ai-foundry/agents/how-to/tools/model-context-protocol)
    *Step-by-step: attaching an MCP server as a tool source for your agent.*

20. [Build and register an MCP server](https://learn.microsoft.com/azure/ai-foundry/mcp/build-your-own-mcp-server)
    *Author your own MCP server and expose it to agents.*

---

## Stage 6 - Production & Migration

21. [Migration guide - from Semantic Kernel to Agent Framework](https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/)
    *Side-by-side mapping of SK concepts to Agent Framework equivalents.*

22. [Migration guide - from AutoGen to Agent Framework](https://learn.microsoft.com/agent-framework/migration-guide/from-autogen/)
    *Side-by-side mapping of AutoGen concepts to Agent Framework equivalents.*

23. [Responsible AI in agents](https://learn.microsoft.com/azure/cloud-adoption-framework/ai-agents/)
    *Governance, security, and compliance planning for agentic AI adoption.*

---

> All links point to official Microsoft Learn documentation.
> Check each page for the latest version - Agent Framework is under active development.
