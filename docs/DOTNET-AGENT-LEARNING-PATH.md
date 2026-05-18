# Microsoft Agent Framework - .NET Learning Path

Official Microsoft Learn links ordered from beginner to advanced, focused entirely on C# / .NET.

---

## Stage 1 - .NET AI Foundations

Start here to understand the core abstractions before touching any agent code.

1. [Microsoft.Extensions.AI libraries overview](https://learn.microsoft.com/dotnet/ai/microsoft-extensions-ai)
   *The `IChatClient` and `IEmbeddingGenerator` abstractions - swap providers with one line of code.*

2. [Use the `IChatClient` interface](https://learn.microsoft.com/dotnet/ai/ichatclient)
   *Send messages, stream responses, configure options - the central interface every provider implements.*

3. [Use the `IEmbeddingGenerator` interface](https://learn.microsoft.com/dotnet/ai/iembeddinggenerator)
   *Generate float vectors from text - the foundation of semantic search and RAG.*

4. [Microsoft Agent Framework overview](https://learn.microsoft.com/agent-framework/overview/)
   *What the framework is, agents vs workflows decision table, and the one-line quickstart.*

5. [Quickstart - .NET AI app template](https://learn.microsoft.com/dotnet/ai/quickstarts/ai-templates)
   *Scaffold a full RAG-enabled Blazor chat app with `IChatClient`, `IEmbeddingGenerator`, and a vector store wired up automatically.*

---

## Stage 2 - Choosing a Model Provider

Pick the provider that fits your scenario - all expose the same `AIAgent` / `IChatClient` interface.

6. [Providers overview & capability matrix](https://learn.microsoft.com/agent-framework/agents/providers/)
   *Side-by-side table: which providers support tools, structured outputs, code interpreter, file search, MCP.*

7. [Ollama provider - local models, no cloud](https://learn.microsoft.com/agent-framework/agents/providers/ollama)
   *Run `llama3.2`, `qwen3`, or any Ollama model locally. Ideal for development and air-gapped scenarios.*

8. [Azure OpenAI provider](https://learn.microsoft.com/agent-framework/agents/providers/azure-openai)
   *Three API surfaces: Chat Completions (simple), Responses API (full-featured), Assistants API (server-managed).*

9. [Microsoft Foundry provider](https://learn.microsoft.com/agent-framework/agents/providers/microsoft-foundry)
   *Managed, versioned agents in the Azure Foundry portal - production-grade with full tool support.*

10. [Anthropic provider](https://learn.microsoft.com/agent-framework/agents/providers/anthropic)
    *Claude models via `Microsoft.Agents.AI.Anthropic` - function tools and streaming.*

11. [GitHub Copilot provider](https://learn.microsoft.com/agent-framework/agents/providers/github-copilot)
    *Coding-oriented agents with shell execution, file operations, and MCP server integration.*

12. [Custom provider - build your own](https://learn.microsoft.com/agent-framework/agents/providers/custom)
    *Inherit from `AIAgent` to wrap any LLM service as a first-class agent.*

---

## Stage 3 - Core Agent Capabilities

Build out a production-ready single agent before adding orchestration.

13. [Tool calling with `AIFunctionFactory`](https://learn.microsoft.com/dotnet/ai/conceptual/tools)
    *Decorate any C# method and let the LLM decide when to call it. Auto-invocation middleware included.*

14. [Agent sessions & multi-turn conversations](https://learn.microsoft.com/agent-framework/agents/conversations/session)
    *`AgentSession` - how the framework stores and restores conversation state across turns.*

15. [Agent pipeline architecture](https://learn.microsoft.com/agent-framework/agents/agent-pipeline)
    *Layered pipeline: middleware → context providers → chat client → model. Know where to hook in.*

16. [Agent middleware](https://learn.microsoft.com/agent-framework/agents/middleware/)
    *Intercept every invocation with `Use(runFunc: ...)` - logging, auditing, safety filters.*

17. [Context providers - memory and RAG](https://learn.microsoft.com/agent-framework/journey/adding-context-providers)
    *Register providers once; they automatically inject history, documents, and instructions every turn.*

18. [RAG with `TextSearchProvider`](https://learn.microsoft.com/agent-framework/agents/rag)
    *Attach a search function (vector store, Azure AI Search, web) to any agent in three lines of code.*

19. [Context window compaction](https://learn.microsoft.com/agent-framework/agents/conversations/compaction)
    *Summarise or trim growing history to stay within token limits without losing key context.*

20. [Tool approval - human gates on tool calls](https://learn.microsoft.com/agent-framework/agents/tools/tool-approval)
    *Intercept and approve/deny individual tool invocations before the agent executes them.*

---

## Stage 4 - Vector Stores & Embeddings

Grounding agents in your data with semantic search.

21. [Vector databases for .NET AI apps - overview](https://learn.microsoft.com/dotnet/ai/vector-stores/overview)
    *RAG workflow end-to-end: embed → store → search → generate. Supported databases and when to use each.*

22. [Build a .NET AI vector search app](https://learn.microsoft.com/dotnet/ai/vector-stores/how-to/build-vector-search-app)
    *Console quickstart: generate embeddings with `IEmbeddingGenerator`, store in a vector collection, run semantic search.*

23. [Use vector stores in .NET AI apps](https://learn.microsoft.com/dotnet/ai/vector-stores/how-to/use-vector-stores)
    *CRUD operations, `VectorSearchOptions`, automatic embedding generation, hybrid search.*

24. [Agent Framework vector store integrations](https://learn.microsoft.com/agent-framework/integrations/#vector-stores)
    *All supported stores: Azure AI Search, Cosmos DB, Qdrant, Elasticsearch, In-Memory, and more - via `Microsoft.Extensions.VectorData`.*

25. [RAG conceptual guide](https://learn.microsoft.com/dotnet/ai/conceptual/rag)
    *When and why to use RAG; chunking, embedding, retrieval, and generation pipeline design decisions.*

---

## Stage 5 - Multi-Agent Workflows

Wire agents together into explicit, type-safe execution graphs.

26. [Workflows overview](https://learn.microsoft.com/agent-framework/workflows/)
    *Agent vs workflow: who decides what happens next - the LLM or the developer? Key features: type safety, checkpointing, HITL.*

27. [Agents in workflows](https://learn.microsoft.com/agent-framework/workflows/agents-in-workflows)
    *How to embed individual agents as executors inside a workflow graph.*

28. [Sequential orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/sequential)
    *Chain agents in order - each receives the previous agent's output. Simplest workflow pattern.*

29. [Concurrent orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/concurrent)
    *Fan-out to multiple agents in parallel; fan-in with an aggregator. `AgentWorkflowBuilder.BuildConcurrent(...)`.*

30. [Handoff orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/handoff)
    *Agents dynamically delegate to specialist agents - routing, triage, escalation patterns.*

31. [Group chat orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/group-chat)
    *Shared conversation thread; orchestrator picks next speaker. Supports round-robin, prompt-based, and custom strategies.*

32. [Magentic orchestration](https://learn.microsoft.com/agent-framework/workflows/orchestrations/magentic)
    *LLM-driven dynamic planning - the orchestrator itself decides which agents to involve at runtime.*

---

## Stage 6 - Production-Grade Features

Human oversight, fault tolerance, and scalable hosting.

33. [Human-in-the-loop (HITL) workflows](https://learn.microsoft.com/agent-framework/workflows/human-in-the-loop)
    *Pause execution at `RequestPort` nodes to wait for human approval before continuing.*

34. [Workflow checkpointing](https://learn.microsoft.com/agent-framework/workflows/checkpoints)
    *Save state at every step - resume from the last checkpoint after a crash or long wait.*

35. [Durable agents with Azure Functions](https://learn.microsoft.com/azure/durable-task/sdks/durable-agents-microsoft-agent-framework)
    *Persistent sessions, automatic checkpointing, HITL waits measured in days - serverless, scales to zero.*

36. [Agentic application patterns - deterministic vs. agent loops](https://learn.microsoft.com/azure/durable-task/sdks/durable-agents-patterns)
    *When to use deterministic durable workflows vs. open-ended agent loops - decision table and trade-offs.*

37. [Agent Framework with Azure App Configuration](https://learn.microsoft.com/azure/azure-app-configuration/howto-ai-agent-config-dotnet)
    *Load agent YAML specifications from App Configuration - change prompts and model settings without redeploying.*

38. [Agent-to-Agent (A2A) protocol](https://learn.microsoft.com/agent-framework/agents/providers/agent-to-agent)
    *Connect to any remote A2A-compliant agent endpoint as a standard `AIAgent` - cross-framework interop.*

---

## Stage 7 - Migration & Ecosystem

Migrating from older frameworks and integrating with the wider ecosystem.

39. [All Agent Framework integrations](https://learn.microsoft.com/agent-framework/integrations/)
    *Full list: vector stores, Azure Functions, Neo4j GraphRAG, Azure App Config, Aspire, and more.*

40. [Migration guide - Semantic Kernel → Agent Framework](https://learn.microsoft.com/agent-framework/migration-guide/from-semantic-kernel/)
    *Side-by-side API mapping: `Kernel` → `AIAgent`, `KernelPlugin` → tools, `AgentGroupChat` → group chat orchestration.*

41. [Migration guide - AutoGen → Agent Framework](https://learn.microsoft.com/agent-framework/migration-guide/from-autogen/)
    *Side-by-side mapping, plus why Agent Framework's built-in checkpointing replaces AutoGen's manual state management.*

---

## Quick-Reference: NuGet Packages

| Scenario | Package |
|---|---|
| Core abstractions only | `Microsoft.Extensions.AI.Abstractions` |
| Full middleware + DI helpers | `Microsoft.Extensions.AI` |
| Vector store abstractions | `Microsoft.Extensions.VectorData.Abstractions` |
| Agent Framework (any provider) | `Microsoft.Agents.AI.Abstractions` |
| Ollama (local) | `Microsoft.Agents.AI.Ollama` |
| Azure OpenAI | `Microsoft.Agents.AI.OpenAI` + `Azure.AI.OpenAI` |
| Microsoft Foundry | `Microsoft.Agents.AI.Foundry` + `Azure.AI.Projects` |
| Anthropic | `Microsoft.Agents.AI.Anthropic` |
| GitHub Copilot | `Microsoft.Agents.AI.GitHub.Copilot` |
| A2A remote agents | `Microsoft.Agents.AI.A2A` |
| Declarative agents | `Microsoft.Agents.AI.Declarative` |
| Durable workflows | `Microsoft.DurableTask.AgentFramework` |

> All packages are under active development. Check [NuGet](https://www.nuget.org/packages?q=Microsoft.Agents.AI) for latest stable/prerelease versions.
> All links point to official Microsoft Learn documentation.
