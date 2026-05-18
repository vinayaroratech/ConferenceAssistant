# ConferenceAssistant

> **The Living Presentation** - a fully interactive conference app that IS the demo. Every audience interaction (polls, questions, insights) runs on the .NET AI stack, locally, with no cloud required.

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com)
[![Aspire](https://img.shields.io/badge/Aspire-13.3-512BD4)](https://learn.microsoft.com/dotnet/aspire)
[![Ollama](https://img.shields.io/badge/LLM-Ollama%20%2F%20local-black)](https://ollama.com)
[![Qdrant](https://img.shields.io/badge/VectorStore-Qdrant-red)](https://qdrant.tech)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## What is this?

ConferenceAssistant is a **live conference engagement platform** built entirely on the modern .NET AI stack. It was designed as a conference talk where the application running on stage _is_ the presentation - audience members scan a QR code, vote on AI-generated polls, ask questions, and watch AI agents analyze responses and generate insights in real time.

Six .NET technologies fire in sequence during the session:

| # | Technology | Role |
|---|---|---|
| 1 | `Microsoft.Extensions.AI` | `IChatClient` / `IEmbeddingGenerator` abstractions |
| 2 | `Microsoft.Extensions.DataIngestion` | RAG pipeline: chunk → enrich → embed → store |
| 3 | `Microsoft.Extensions.VectorData` | Semantic search over session content |
| 4 | `Microsoft.Agents.AI` | Specialized AI agents with tool calling |
| 5 | `ModelContextProtocol` | MCP server exposing session tools to any AI client |
| 6 | `.NET Aspire` | Orchestration of Ollama, Qdrant, PostgreSQL, and the web app |

---

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Blazor Server (Web)                │
│  Presenter View │ Audience View │ Health Dashboard   │
└────────────┬────────────────────────────┬────────────┘
             │                            │
     ┌───────▼────────┐          ┌────────▼────────┐
     │  Agent Workflows│          │   MCP Server    │
     │  ┌───────────┐  │          │  /mcp endpoint  │
     │  │SurveyArch.│  │          │  10 tools       │
     │  │ResponseAn.│  │          └────────┬────────┘
     │  │KnowledgeCu│  │                   │
     │  │SessionSum.│  │          VS Code Copilot
     │  └───────────┘  │          (or any MCP client)
     └───────┬─────────┘
             │
     ┌───────▼───────────────────────────────────────┐
     │              Core Services                     │
     │  PollService │ SessionService │ AgentTools     │
     └───────┬───────────────┬───────────────────────┘
             │               │
     ┌───────▼──────┐ ┌──────▼──────────────┐
     │    Ollama    │ │       Qdrant         │
     │  llama3.2 /  │ │  Vector store        │
     │  qwen2.5:7b  │ │  (semantic search)   │
     │  nomic-embed │ └─────────────────────┘
     └──────────────┘
```

---

## Features

- **AI-generated polls** - the Survey Architect agent reads the current topic, searches the knowledge base, and crafts targeted audience polls
- **Real-time response analysis** - the Response Analyst agent interprets poll results and stores data-driven insights
- **Knowledge curation** - session content is chunked, embedded, and made semantically searchable throughout the talk
- **Audience Q&A** - attendees submit questions; the Knowledge Curator agent answers them from session context
- **Session summary** - the Session Summary workflow generates a comprehensive post-session report
- **MCP server** - exposes 10 tools so VS Code Copilot (or any MCP client) can query live session data
- **Health dashboard** - real-time view of Ollama, Qdrant, and ingestion pipeline status
- **QR code** - attendees join the audience view by scanning a generated QR code

---

## Prerequisites

| Requirement | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.108 | `global.json` pins the version |
| Docker Desktop | Latest | Runs Ollama, Qdrant, PostgreSQL via Aspire |
| .NET Aspire workload | 13.3+ | `dotnet workload install aspire` |
| Ollama models | - | Pulled automatically on first run |

> **No Azure or OpenAI account required.** Everything runs locally via Docker.

---

## Local Setup

### Ollama (required)

Ollama runs the local LLM and embedding model. Install it from [ollama.com](https://ollama.com), then pull the required models:

```bash
# Chat model (choose one - see model comparison table below)
ollama pull llama3.2

# Embedding model (required for semantic search)
ollama pull nomic-embed-text
```

Verify Ollama is running:
```bash
curl http://localhost:11434/api/tags
```

> When running via .NET Aspire, Ollama is started automatically inside Docker. Manual installation is only needed if you run the web app outside of Aspire.

### Qdrant (optional)

Qdrant is the vector store used for semantic search. It is **optional** - the app falls back to an in-memory vector store if Qdrant is unavailable.

To run Qdrant manually via Docker:
```bash
docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant
```

To disable Qdrant and use the in-memory store instead, set in `appsettings.json`:
```json
"VectorStore": {
  "Provider": "InMemory"
}
```

> When running via .NET Aspire, Qdrant is started automatically with a persistent data volume. No manual setup is required.

---

## Getting Started

### 1. Clone

```bash
git clone https://github.com/your-org/ConferenceAssistant.git
cd ConferenceAssistant
```

### 2. Install the Aspire workload

```bash
dotnet workload install aspire
```

### 3. Run

```bash
dotnet run --project src/ConferenceAssistant.AppHost
```

Aspire will start all containers automatically. On first run, Ollama will pull the required models (this may take a few minutes depending on your connection).

### 4. Open the app

| URL | Purpose |
|---|---|
| Aspire dashboard | `https://localhost:15888` |
| Presenter view | `https://localhost:PORT/presenter` |
| Audience view | `https://localhost:PORT/audience` |
| Health dashboard | `https://localhost:PORT/health` |

The exact port is shown in the Aspire dashboard under the `web` resource.

---

## Configuration

All settings are in `src/ConferenceAssistant.Web/appsettings.json`:

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "llama3.2",
    "EmbeddingModel": "nomic-embed-text"
  },
  "VectorStore": {
    "Provider": "Qdrant",
    "QdrantEndpoint": "http://localhost:6334",
    "QdrantHttpEndpoint": "http://localhost:6333"
  },
  "Session": {
    "Code": "AICONF",
    "OutlinePath": "data/session-outline.md",
    "TopicsPath": "data/sessions.json",
    "SlidesPath": "data/slides.md"
  }
}
```

### Switching LLM models

Change `ChatModel` to any model available in Ollama. Recommended options for tool calling:

| Model | Quality | Speed | Tool calling |
|---|---|---|---|
| `llama3.2` (default) | Good | Fast | Limited multi-step |
| `qwen2.5:7b` | Better | Moderate | Reliable multi-step |
| `phi4` | Best | Slow | Reliable |

```bash
ollama pull qwen2.5:7b
```

Then update `appsettings.json`:
```json
"ChatModel": "qwen2.5:7b"
```

### Customising the session

Replace the files in `data/` to run a different session:

| File | Purpose |
|---|---|
| `sessions.json` | Topic list with titles, descriptions, and status |
| `slides.md` | Slide content ingested into the knowledge base |
| `session-outline.md` | Speaker notes and poll trigger hints per segment |

---

## Project Structure

```
src/
├── ConferenceAssistant.AppHost/      # .NET Aspire orchestration
├── ConferenceAssistant.Web/          # Blazor Server app (presenter, audience, health views)
├── ConferenceAssistant.Core/         # Domain models and in-memory services
├── ConferenceAssistant.Agents/       # AI agent workflows (Survey, Analysis, Curation, Summary)
├── ConferenceAssistant.Ingestion/    # RAG pipeline and semantic search
├── ConferenceAssistant.Mcp/          # MCP server tools
├── ConferenceAssistant.CopilotDemo/  # VS Code Copilot integration demo
└── ConferenceAssistant.ServiceDefaults/ # Shared Aspire service defaults

tests/
└── ConferenceAssistant.Evaluation/   # Agent quality / evaluation tests

data/
├── sessions.json                     # Session topics
├── slides.md                         # Slide content
└── session-outline.md                # Speaker outline with poll triggers

docs/
├── AI-LEARNING-PATH.md               # Microsoft.Extensions.AI learning resources
└── DOTNET-AGENT-LEARNING-PATH.md     # Agent Framework learning resources
```

---

## AI Agents

The app has four specialized agents, each with a focused persona and a minimal tool set:

| Agent | Role | Tools |
|---|---|---|
| **Survey Architect** | Generates audience polls from session context | `GetCurrentTopic`, `SearchKnowledge`, `GetAudienceQuestions`, `GetAllPollResults`, `GetAllInsights`, `CreatePoll` |
| **Response Analyst** | Interprets poll results and stores insights | `StoreInsight` (context pre-fetched in C#) |
| **Knowledge Curator** | Answers audience questions from session content | `SearchKnowledge`, `SaveInsight` |
| **Session Summarizer** | Generates end-of-session summary | `GetAllInsights`, `GetAllPollResults`, `SearchKnowledge` |

> **Architecture note:** For small local models (≤4B parameters), all context is pre-fetched in C# before invoking the agent. The agent then performs a single, reliable tool call. This avoids the multi-step tool chaining that small models consistently fail at.

---

## MCP Server

ConferenceAssistant exposes a Model Context Protocol server at `/mcp`. Connect any MCP-compatible client (VS Code Copilot, Claude Desktop, etc.) to query live session data.

### Connecting VS Code Copilot

Add to your `.vscode/mcp.json`:

```json
{
  "servers": {
    "conference-pulse": {
      "type": "http",
      "url": "http://localhost:PORT/mcp"
    }
  }
}
```

### Available tools

| Tool | Description |
|---|---|
| `get_session_status` | Current topic and all topic statuses |
| `get_outline` | Full session outline with slides |
| `get_active_poll` | Currently open poll |
| `get_all_insights` | All AI-generated insights |
| `get_insights_markdown` | Insights formatted as markdown |
| `get_audience_questions` | Questions submitted by the audience |
| `search_knowledge` | Semantic search over the session knowledge base |
| `get_knowledge_stats` | Knowledge base statistics |
| `generate_session_summary` | Trigger the Session Summary workflow |

---

## Troubleshooting

### Qdrant shows "Degraded" on the health dashboard

The health check uses the **HTTP REST** endpoint (port 6333), not the gRPC endpoint (port 6334). Verify:
- Qdrant is running: `curl http://localhost:6333/healthz`
- `appsettings.json` has `"QdrantHttpEndpoint": "http://localhost:6333"` (not `6334`)

### Ollama model not found

```
Error: model 'llama3.2' not found
```

Run `ollama pull llama3.2` (or whichever model is set in `ChatModel`). Check available models with `ollama list`.

### Agent workflow takes 3+ minutes

This happens when the agent calls multiple tools sequentially with a large model. Options:
- Use `llama3.2` (3B, fastest) - works well with pre-fetched context workflows
- The `ResponseAnalysisWorkflow` and similar workflows pre-fetch all context in C# before invoking the agent, keeping inference to a single pass
- For `PollGenerationWorkflow`, switch to `qwen2.5:7b` for reliable multi-step tool calling

### Agent exits after the first tool call

This is a known behaviour of small local models (≤4B parameters). They treat the first meaningful tool response as "task complete" and stop. The solution is already applied in `ResponseAnalysisWorkflow`: pre-fetch all data in C# and give the agent a single tool to call. Apply the same pattern to any workflow showing this behaviour.

### Blazor "InvalidOperationException: The current thread is not associated with the Dispatcher"

This occurs when `StateHasChanged()` is called from a background thread (e.g., a `System.Threading.Timer` callback). Fix:
```csharp
// Wrong
_timer = new Timer(_ => StateHasChanged(), ...);

// Correct
_timer = new Timer(async _ => await InvokeAsync(StateHasChanged), ...);
```

### Knowledge base is empty after startup

The ingestion pipeline runs on startup and embeds `slides.md` into Qdrant. If the knowledge base is empty:
- Check the Aspire dashboard logs for `OutlineIngestionPipeline` or `ContentIngestionPipeline` errors
- Ensure `VectorStore:ForceReingest` is `false` on subsequent runs (set to `true` to force a full re-ingest)
- Verify Qdrant is healthy before the app starts

---

## Key Design Decisions

### Domain-Driven Design (DDD)

The domain model in `ConferenceAssistant.Core` follows DDD principles to keep business logic inside the entities themselves rather than scattered across services.

#### Aggregates and Entities

| Type | Class | Role |
|---|---|---|
| Aggregate Root | `Poll` | Owns its lifecycle: `Create()` → `Launch()` → `Close()` / `Reopen()` |
| Aggregate Root | `SessionTopic` | Controls topic status via `Activate()` / `Complete()` |
| Aggregate Root | `Conference` | Configuration root, loaded from file |
| Entity | `AudienceQuestion` | `Submit()` → `Approve()` / `Reject()` / `Upvote()` / `SetAnswer()` |
| Entity | `Insight` | Immutable after `Create()` - no setters exposed |
| Entity | `Slide` | Data entity parsed from markdown; no domain invariants |
| Value Object | `PollResponse` | Immutable vote record; `Cast()` factory, never mutated |
| Value Object | `PollPrompt` | `sealed record` embedded in topic configuration |

All entities inherit from `Entity<TId>` (identity + domain events list). Aggregate roots inherit `AggregateRoot<TId>`, marking them as the sole transaction boundary for their cluster.

#### Private Setters and Factory Methods

Properties that represent domain state use `private set` so they can only change through named behavior methods:

```csharp
// Before (anemic) - anyone can corrupt state:
poll.Status = PollStatus.Active;
poll.ClosedAt = null;

// After (DDD) - invariants are enforced:
poll.Launch();   // throws if not in Draft; raises PollLaunched event
poll.Close();    // throws if not Active;  raises PollClosed event
poll.Reopen();   // throws if not Closed;  raises PollReopened event
```

#### Domain Events

Every meaningful state change raises a domain event that is stored on the entity until the service dispatches it:

```
PollCreated → PollLaunched → VoteCast (×N) → PollClosed → PollReopened
TopicActivated → TopicCompleted
QuestionSubmitted → QuestionApproved / QuestionRejected → QuestionAnswered / QuestionUpvoted
InsightStored
```

Events are plain `record` types implementing `IDomainEvent`:

```csharp
public record PollClosed(string PollId, DateTimeOffset ClosedAt) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
```

#### Domain Event Dispatcher and Handlers

`DomainEventDispatcher` resolves all registered `IDomainEventHandler<TEvent>` implementations via DI and invokes them **fire-and-forget** after any aggregate mutation, so service methods never block on side effects:

```csharp
// In PollService:
poll.Close();
dispatcher.DispatchAndClear(poll);   // fires PollClosed in background
```

Three handlers are registered at startup:

| Handler | Project | Reacts To | What It Does |
|---|---|---|---|
| `PollClosedHandler` | `Agents` | `PollClosed` | Automatically runs `ResponseAnalysisWorkflow` - no manual "Analyze" click needed |
| `TopicActivatedHandler` | `Core` | `TopicActivated` | Logs topic activation for analytics/audit |
| `InsightStoredHandler` | `Core` | `InsightStored` | Logs insight ID, type, and poll for tracing |

**Adding a new handler** takes two steps:

```csharp
// 1. Implement the interface
public class MyHandler(SomeService svc) : IDomainEventHandler<PollLaunched>
{
    public Task HandleAsync(PollLaunched @event, CancellationToken ct = default)
    {
        // react to the event
        return Task.CompletedTask;
    }
}

// 2. Register in Program.cs (multiple handlers per event are supported)
builder.Services.AddSingleton<IDomainEventHandler<PollLaunched>, MyHandler>();
```

#### File Layout

```
src/ConferenceAssistant.Core/
├── Models/
│   ├── Entity.cs               ← base entity (Id + domain events)
│   ├── AggregateRoot.cs        ← aggregate root marker
│   ├── Poll.cs                 ← aggregate: Launch/Close/Reopen
│   ├── AudienceQuestion.cs     ← entity: Approve/Reject/Upvote/SetAnswer
│   ├── Insight.cs              ← entity: immutable, Create() factory
│   ├── PollResponse.cs         ← value object: Cast() factory
│   ├── SessionTopic.cs         ← aggregate: Activate/Complete
│   ├── Conference.cs           ← aggregate: configuration root
│   └── Slide.cs                ← entity: parsed from markdown
└── Domain/
    ├── IDomainEvent.cs
    ├── IDomainEventHandler.cs
    ├── DomainEventDispatcher.cs
    ├── Events/
    │   ├── PollEvents.cs       ← PollCreated, PollLaunched, PollClosed, PollReopened, VoteCast
    │   ├── QuestionEvents.cs   ← QuestionSubmitted, Approved, Rejected, Upvoted, Answered
    │   ├── TopicEvents.cs      ← TopicActivated, TopicCompleted
    │   └── InsightEvents.cs    ← InsightStored
    └── Handlers/
        ├── TopicActivatedHandler.cs
        └── InsightStoredHandler.cs

src/ConferenceAssistant.Agents/
└── Handlers/
    └── PollClosedHandler.cs    ← auto-triggers ResponseAnalysisWorkflow
```

### Pre-fetch context in C#, single tool call for agents

Small local models (llama3.2 3B, phi3-mini 3.8B) reliably exit after the first tool call that returns meaningful data. Prompting alone cannot override this - it is a model capacity limit, not a configuration problem.

**Decision:** All context gathering (poll results, knowledge search, duplicate checks) is done in C# before the agent is invoked. The agent receives a fully-populated prompt and only needs to call one tool (e.g., `StoreInsight`, `CreatePoll`). This makes workflows reliable on any model size.

### In-memory state, no database dependency for demos

All session state (polls, votes, questions, insights) is held in `SessionService` as in-memory collections. This makes the app self-contained for conference demos - no database migrations, no seed data, no connection strings to manage.

PostgreSQL is provisioned by Aspire but reserved for future persistence. The `ForceReingest` flag controls whether the vector store is repopulated on startup.

### `IChatClient` abstraction over direct Ollama/OpenAI SDKs

All LLM interaction goes through `Microsoft.Extensions.AI`'s `IChatClient`. Switching from Ollama to Azure OpenAI, or from `llama3.2` to `qwen2.5:7b`, requires changing one line in `Program.cs` (or one config value). No agent, workflow, or tool code changes.

### Agent instructions in a single static class

All agent personas and numbered instructions live in `AgentInstructions.cs`. This makes it easy to tune prompts, compare versions in git diffs, and share instruction text between the agent and evaluation tests - without scattering strings across multiple files.

### MCP server as a first-class output

The app exposes a `/mcp` endpoint from day one, not as an afterthought. This means VS Code Copilot (or Claude Desktop, or any MCP client) can query live session data during the demo - reinforcing the talk's message that MCP is the standard protocol for AI tool integration.

### Qdrant HTTP vs gRPC endpoints

Qdrant exposes two ports: gRPC (6334) for the vector store client, and HTTP REST (6333) for health checks and the dashboard. The app uses separate config keys (`QdrantEndpoint` for gRPC, `QdrantHttpEndpoint` for HTTP) to avoid probing the wrong port with HTTP health checks.

---

## Running Tests

```bash
dotnet test tests/ConferenceAssistant.Evaluation
```

The evaluation tests assess agent output quality - poll relevance, insight accuracy, and knowledge curator responses - using the running Ollama instance.

---

## Contributing

Contributions are welcome. Please open an issue before submitting a pull request for significant changes.

1. Fork the repo
2. Create a feature branch (`git checkout -b feature/your-feature`)
3. Commit your changes
4. Open a pull request

---

## Learning Resources

Curated learning paths for the technologies used in this project:

- [docs/AI-LEARNING-PATH.md](docs/AI-LEARNING-PATH.md) - `Microsoft.Extensions.AI` and `Microsoft.Agents.AI`
- [docs/DOTNET-AGENT-LEARNING-PATH.md](docs/DOTNET-AGENT-LEARNING-PATH.md) - Agent Framework deep dive

---

## License

MIT - see [LICENSE](LICENSE).
