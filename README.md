# Conference Assistant - The Living Presentation

> **The app is the talk.** Conference Assistant is a live, interactive conference session powered entirely by local AI. The presenter never leaves the app - polls generate themselves, audience questions are moderated in real-time, and a multi-agent AI synthesises the entire session into a closing summary.

---

## What It Does

| Feature | Description |
|---|---|
| **Multi-Conference Support** | Load any of three pre-built sessions (AI in .NET, .NET 10 Deep Dive, Cloud-Native) - switch live without restarting |
| **Live Polls** | AI-generated, context-aware polls appear at natural breakpoints; duplicate votes blocked server-side |
| **Audience Join** | Attendees open `/session/{code}` on any device and vote in real time |
| **Slide Preview** | `/preview` - browse the full slide deck, click any slide to read speaker notes, pre-create polls before the session |
| **Presenter Dashboard** | Full session control - advance topics, trigger polls, approve/reject questions, session timer, End Session |
| **Speaker Notes** | `/notes` - private second-screen view with current talking points and slide text |
| **Watch Mode** | `/watch/{code}` - audience read-only slide view with live current-slide highlight |
| **Display View** | `/display` - projector-ready screen with live poll bars and insights |
| **Q&A Moderation** | Questions queue with Approve / Reject; rate-limited (15 s cooldown per connection) |
| **Analytics** | `/analytics` - full session analytics, CSV export, copy unanswered questions |
| **Organizer** | `/organizer` - manage sessions, switch active conference |
| **Semantic Search** | Ask any question about session content; vector search finds relevant chunks |
| **Session Summary** | Three AI agents contribute perspectives; a fourth synthesises a closing summary |
| **MCP Server** | Exposes the entire session as tools that VS Code Copilot (or any MCP client) can query |
| **Health Dashboard** | `/health` - live status of Ollama, Qdrant, and session data |
| **Vector Store Browser** | `/vector-store` - inspect ingested knowledge chunks |

---

## Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Aspire AppHost                          │
│  ┌──────────┐  ┌─────────┐  ┌──────────────┐  ┌────────────┐    │
│  │  Ollama  │  │ Qdrant  │  │  PostgreSQL  │  │  Web App   │    │
│  │ (Docker) │  │(Docker) │  │  (Docker)    │  │  (Blazor)  │    │
│  └──────────┘  └─────────┘  └──────────────┘  └────────────┘    │
└─────────────────────────────────────────────────────────────────┘
```

```
ConferenceAssistant.Web          Blazor Server - entry point
│
├── ConferenceAssistant.Core          Domain models + in-memory services
├── ConferenceAssistant.Ingestion     RAG pipeline + semantic search
├── ConferenceAssistant.Agents        AI agents + orchestration workflows
├── ConferenceAssistant.Mcp           MCP server (tools for external AI clients)
└── ConferenceAssistant.ServiceDefaults  Shared Aspire/OTEL extensions
```

---

## Prerequisites

| Requirement | Version | How to verify |
|---|---|---|
| **.NET SDK 10** | 10.0+ | `dotnet --version` → must show `10.x.x` |
| **Docker Desktop** | Latest | Must be running before `dotnet run` - check the system tray icon |
| **Ollama** | Latest | Pulled automatically by Aspire on first run; or install from [ollama.com](https://ollama.com) for standalone mode |

> **No Azure required.** Everything runs locally.

### Verify prerequisites manually

```bash
dotnet --version   # Expected: 10.x.x
docker info        # Expected: prints engine info without errors
ollama list        # Expected: shows llama3.2 and nomic-embed-text (standalone mode only)
```

If models are missing in standalone mode:

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
```

---

## Quick Start

### 1. Clone and restore

```bash
git clone <repo-url>
cd ConferenceAssistant
dotnet restore
```

### 2. Run via Aspire

```bash
cd src/ConferenceAssistant.AppHost
dotnet run
```

Aspire starts all containers automatically:
- **Ollama** - pulls `llama3.2` and `nomic-embed-text` on first run (may take a few minutes)
- **Qdrant** - vector store, gRPC port 6334
- **PostgreSQL** - relational store, port 5432
- **Aspire Dashboard** - http://localhost:15888

### 3. Open the app

The Aspire dashboard shows the web app URL (usually `https://localhost:7xxx`).

| URL | Who uses it |
|---|---|
| `/` | **Home** - conference selector, session overview, links to all views |
| `/preview` | **Presenter** - pre-session: browse slides, pre-create polls |
| `/presenter` | **Presenter** - live session: topics, polls, Q&A, analysis, End Session |
| `/notes` | **Speaker** - private notes screen (talking points + slide text) |
| `/display` | **Projector** - live poll bars, current slide, insights ticker |
| `/session/{code}` | **Audience** - vote and ask questions on any device |
| `/watch/{code}` | **Audience** - follow-along read-only slide view |
| `/analytics` | **Organizer** - session analytics + CSV export |
| `/organizer` | **Organizer** - conference management |
| `/health` | **System** - live health dashboard |
| `/vector-store` | **System** - knowledge base browser |

### 4. Running without Aspire (standalone)

```bash
cd src/ConferenceAssistant.Web
dotnet run
```

Requires Ollama and optionally Qdrant running locally.

**Start Qdrant** (optional - the app falls back to InMemory if not running):

```bash
docker run -d --name qdrant \
  -p 6333:6333 -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant

curl http://localhost:6333/healthz   # Expected: {"title":"qdrant - vector search engine",...}
```

> The named volume `qdrant_storage` persists vector data across restarts. Omit `-v` for an ephemeral instance.

---

## Multi-Conference Support

Three sessions ship out of the box, all stored in a single `data/sessions.json` file:

| Session Code | Title | Slides file |
|---|---|---|
| `AICONF` | AI in .NET - The Living Presentation | `data/slides.md` |
| `DOTNET26` | .NET 10 Deep Dive | `data/dotnet26-slides.md` |
| `CLOUDNATIVE` | Cloud-Native Patterns with .NET | `data/cloudnative-slides.md` |

Conference metadata (names, paths, which is default) lives in `data/conferences.json`. The Home page (`/`) lets you click a conference card to preview its topics, then switch the live session with one button.

### Adding a new conference

1. Add a session block to `data/sessions.json`:
```json
{
  "sessionId": "MYNEWCONF",
  "title": "My Talk Title",
  "description": "One-line description",
  "topics": [ ...SessionTopic objects... ]
}
```

2. Add a slides file, e.g. `data/mynewconf-slides.md`, with `<!-- topic: topicId -->` markers.

3. Add an entry to `data/conferences.json`:
```json
{
  "code": "MYNEWCONF",
  "sessionId": "MYNEWCONF",
  "name": "My Talk Title",
  "description": "One-line description",
  "topicsPath": "data/sessions.json",
  "slidesPath": "data/mynewconf-slides.md",
  "isDefault": false
}
```

---

## Configuration

`src/ConferenceAssistant.Web/appsettings.json`

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
    "ForceReingest": false
  },
  "Session": {
    "Code": "AICONF",
    "OutlinePath": "data/session-outline.md",
    "TopicsPath": "data/sessions.json",
    "SlidesPath": "data/slides.md"
  }
}
```

> The `Session` block is a **fallback**. When `data/conferences.json` is present, it overrides these values - the default conference's `topicsPath` and `slidesPath` are used instead.

To swap to a different Ollama model, change `ChatModel`. All code uses `IChatClient` and `IEmbeddingGenerator` abstractions so models are interchangeable.

---

## Health Checks

| Endpoint | What it checks |
|---|---|
| `GET /health/status` | Full JSON report - all components |
| `GET /health/ready` | Readiness: Ollama + Qdrant + session data |
| `GET /health/alive` | Liveness: is the process up |

```bash
curl https://localhost:7xxx/health/status
```

Example healthy response:

```json
{
  "status": "Healthy",
  "components": {
    "ollama":       { "status": "Healthy", "description": "Models ready: llama3.2, nomic-embed-text" },
    "qdrant":       { "status": "Healthy", "description": "Qdrant is healthy at http://localhost:6333" },
    "session-data": { "status": "Healthy", "description": "5 topics and 42 slides loaded. Active topic: Microsoft.Extensions.AI" }
  }
}
```

### Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| `ollama` Unhealthy - "unreachable" | Docker not running / container not started | Start Docker Desktop; wait for Aspire to show `ollama` as Running |
| `ollama` Degraded - "models not yet pulled" | First-run download in progress | Watch ollama container logs in Aspire dashboard |
| `qdrant` Degraded - "unreachable" | App uses InMemory fallback - fully functional, no persistence | Start Qdrant: `docker run -p 6333:6333 qdrant/qdrant` |
| `session-data` Degraded - "no topics loaded" | `data/sessions.json` not found | Ensure `data/sessions.json` exists relative to working directory; check `Session:TopicsPath` config |

---

## Project Structure

### `ConferenceAssistant.Core`

Pure domain - no AI dependencies.

| File | Purpose |
|---|---|
| `Models/Conference.cs` + `TopicPreview` | Conference metadata + lightweight topic list for the Home page |
| `Models/SessionTopic.cs` | A talk segment: title, talking points, poll prompts, slides |
| `Models/Poll.cs` + `PollResponse.cs` | Poll lifecycle (Draft → Active → Closed) and audience responses |
| `Models/AudienceQuestion.cs` | Submitted question with thread-safe upvote counter |
| `Models/Insight.cs` | AI-generated insight attached to a poll |
| `Services/ConferenceRegistry.cs` | Loads `conferences.json`; resolves topic previews from `sessions.json` |
| `Services/SessionService.cs` | Topic navigation, slide control, Q&A, session end, conference switching |
| `Services/PollService.cs` | Poll CRUD, launch/close, response submission, server-side vote dedup |
| `Services/InMemoryStore.cs` | Generic thread-safe in-process store |
| `Services/SlideMarkdownParser.cs` | Parses `data/*.md` slide files into `Slide` objects keyed to topic IDs |

### `ConferenceAssistant.Ingestion`

RAG pipeline - reads documents, chunks them, embeds, and stores in the vector store.

| File | Purpose |
|---|---|
| `SemanticSearchService.cs` | Query → embedding → vector search → ranked results |
| `Pipelines/OutlineIngestionPipeline.cs` | Reads `session-outline.md`, chunks by headings, LLM summary + keywords, stores embeddings |
| `Pipelines/ResponseIngestionPipeline.cs` | Ingests poll responses with sentiment analysis |
| `Pipelines/McpContentIngestionPipeline.cs` | Fetches content via MCP tools and ingests it |

**Ingestion flow:**

```
session-outline.md
  → split on ## headings
  → foreach chunk:
      LLM: "summarise in one sentence"   → Summary
      LLM: "extract 3-5 keywords"        → Keywords[]
      EmbeddingGenerator: text → float[] → Embedding
      VectorStore.UpsertAsync(record)
```

### `ConferenceAssistant.Agents`

Named AI agents with fixed personas and tool sets.

| Agent | Role | Tools |
|---|---|---|
| **SurveyArchitect** | Generates contextual poll questions | `search_knowledge`, `create_poll` |
| **ResponseAnalyst** | Reads poll results, identifies patterns | `get_poll_results`, `search_knowledge` |
| **KnowledgeCurator** | Broad semantic search across all ingested content | `search_knowledge` |

| Workflow | What it does |
|---|---|
| `PollGenerationWorkflow` | SurveyArchitect searches context then creates a poll |
| `ResponseAnalysisWorkflow` | ResponseAnalyst reads results → Curator finds related knowledge → Insight saved |
| `SessionSummaryWorkflow` | All three agents contribute → final LLM call synthesises a closing summary |

### `ConferenceAssistant.Mcp`

MCP server at `POST /mcp`. Exposes 8 tools:

| Tool | Description |
|---|---|
| `GetSessionStatus` | Current topic, all topic statuses |
| `GetActivePoll` | Live poll question + real-time vote counts |
| `GetPollResults` | Full results + insights for a poll |
| `SearchSessionKnowledge` | Semantic search across ingested content |
| `GetTopAudienceQuestions` | Questions sorted by upvotes |
| `GenerateSessionSummary` | Runs the full multi-agent summary workflow |
| `SearchKnowledge` | Text search returning source + summary snippets |
| `GetKnowledgeStats` | Knowledge base metadata |

**Connect from VS Code Copilot** - add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "conference-pulse": {
      "type": "http",
      "url": "https://localhost:7xxx/mcp"
    }
  }
}
```

### `ConferenceAssistant.Web`

Blazor Server application. All interactive components use `@rendermode InteractiveServer`.

| Page | Route | Layout | Purpose |
|---|---|---|---|
| `Home.razor` | `/` | Dashboard | Conference selector, session overview, quick-launch links |
| `Preview.razor` | `/preview` | Presentation | Slide deck grid, speaker notes panel, pre-create polls |
| `Presenter.razor` | `/presenter` | Presentation | Topic panel, slide scroller, poll generation, Q&A moderation, session timer, End Session |
| `Notes.razor` | `/notes` | Presentation | Private speaker notes (talking points + slide text) |
| `Display.razor` | `/display` | Presentation | Projector: live poll bars, current slide, insights ticker |
| `Watch.razor` | `/watch/{code}` | Presentation | Audience slide follow-along view |
| `Session.razor` | `/session/{code}` | Presentation | Audience: vote, ask questions (15 s rate limit), upvote |
| `Analytics.razor` | `/analytics` | Dashboard | Poll results, question log, CSV export, copy unanswered |
| `Organizer.razor` | `/organizer` | Dashboard | Conference management |
| `HealthDashboard.razor` | `/health` | Dashboard | Live health status of all components |
| `VectorStoreBrowser.razor` | `/vector-store` | Dashboard | Browse ingested knowledge chunks |

---

## Data Files (`data/`)

| File | Purpose |
|---|---|
| `sessions.json` | All sessions in one file - array of `{sessionId, title, description, topics:[...]}` objects |
| `conferences.json` | Conference registry - codes, names, paths, which is default |
| `slides.md` | AICONF slide deck - `<!-- topic: id -->` markers key slides to topics |
| `dotnet26-slides.md` | DOTNET26 slide deck |
| `cloudnative-slides.md` | CLOUDNATIVE slide deck |
| `session-outline.md` | Full AICONF session outline - ingested as RAG content on startup |

> `seed-topics.json`, `dotnet26-topics.json`, and `cloudnative-topics.json` are **superseded** by `sessions.json` and can be deleted.

---

## Technology Stack

| Technology | Version | Role |
|---|---|---|
| **.NET Aspire** | 13.3.1 | Container orchestration, service discovery |
| **Blazor Server (.NET 10)** | Built-in | UI (`@rendermode InteractiveServer`) |
| **Microsoft.Extensions.AI** | 10.5.2 | `IChatClient` + `IEmbeddingGenerator` abstractions |
| **OllamaSharp** | 5.4.25 | Ollama provider implementing M.E.AI interfaces |
| **Microsoft.Extensions.VectorData** | Built-in | `VectorStoreCollection<TKey,TRecord>` abstraction |
| **SK InMemory** | preview | In-process vector store (dev / demo) |
| **SK Qdrant** | preview | Persistent vector store |
| **ModelContextProtocol.AspNetCore** | 1.2.0 | MCP server (`[McpServerTool]`, `/mcp` endpoint) |
| **QRCoder** | Latest | QR code generation for audience join URL |

---

## Key Design Decisions

**Why OllamaSharp instead of `Microsoft.Extensions.AI.Ollama`?**  
`M.E.AI.Ollama` was deprecated. OllamaSharp ships `OllamaApiClient` implementing both `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` directly.

**Why a single `sessions.json` instead of per-conference topic files?**  
All sessions live in one place - easier to edit, version, and reason about. `ConferenceRegistry` and `SessionService` both detect the multi-session format automatically and extract the right session by `sessionId`. Legacy flat-array files are still supported as a fallback.

**Why in-memory state instead of a database?**  
Conference sessions are ephemeral. All domain state lives in `InMemoryStore<T>` for zero-latency reads on Blazor SignalR circuits. PostgreSQL is wired up and reserved for future persistence (audit log, exported summaries).

**Why three agents + a synthesiser in `SessionSummaryWorkflow`?**  
Each agent has a distinct lens (engagement, understanding, themes). The final LLM call gets richer, more diverse input than a single prompt would produce.

**Why server-side vote deduplication?**  
Each Blazor circuit gets a stable `_voterId = Guid.NewGuid()` on connection. `PollService` tracks a `HashSet<string>` of `"{pollId}:{voterId}"` keys, blocking duplicate submissions without requiring authentication.

---

## Session Flow (What Happens Live)

```
App starts
  └─ ConferenceRegistry.LoadAsync("data/conferences.json")
       └─ Reads sessions.json, populates TopicPreviews for each conference
  └─ SessionService.LoadTopicsAsync("data/sessions.json", sessionId: "AICONF")
  └─ SessionService.LoadSlidesAsync("data/slides.md")
  └─ OutlineIngestionPipeline.IngestAsync("data/session-outline.md")
       └─ Chunks embedded and stored in vector store

Presenter opens /preview
  └─ Browses full slide deck, reads speaker notes
  └─ Optionally pre-creates polls before going live

Presenter opens /presenter → goes live
  └─ Sees current topic, talking points, slide navigator, session timer

Presenter clicks "Generate Poll"
  └─ PollGenerationWorkflow → SurveyArchitect searches knowledge → creates Poll (Active)

Audience at /session/AICONF votes
  └─ PollService.SubmitResponse(pollId, option, voterId)  [server-side dedup]

Presenter clicks "Close & Analyse"
  └─ ResponseIngestionPipeline ingests each response (sentiment + embedding)
  └─ ResponseAnalyst analyses results → Insight saved
  └─ KnowledgeCurator finds related knowledge → Insight saved
  └─ Display view shows insight ticker in real time

Presenter clicks "End Session"
  └─ SessionService.EndSession() - marks all topics Completed, fires SessionEnded event
  └─ Audience session page shows "session ended" banner

Presenter clicks "Generate Summary"
  └─ SessionSummaryWorkflow → three agents contribute → final LLM synthesises → rich closing summary

Presenter / Organizer opens /analytics
  └─ Full poll results, audience questions, CSV export
```

---

## Running Tests

```bash
dotnet test
```

Test projects:
- `tests/ConferenceAssistant.Agents.Tests`
- `tests/ConferenceAssistant.Ingestion.Tests`
- `tests/ConferenceAssistant.Mcp.Tests`

---

## Build Verification

```powershell
dotnet build src/ConferenceAssistant.Web --no-restore 2>&1 | Select-String ": error CS|: error RZ"
# No output + exit code 1 = clean build
```


```
ConferenceAssistant.Web  (Blazor Server - entry point)
│
├── ConferenceAssistant.Core          Domain models + in-memory services
├── ConferenceAssistant.Ingestion     RAG pipeline + semantic search
├── ConferenceAssistant.Agents        AI agents + orchestration workflows
├── ConferenceAssistant.Mcp           MCP server (tools for external AI clients)
└── ConferenceAssistant.ServiceDefaults  Shared Aspire/OTEL extensions
```

---

## Prerequisites

| Requirement | Version | How to verify |
|---|---|---|
| **.NET SDK 10** | 10.0+ | `dotnet --version` → must show `10.x.x` |
| **Docker Desktop** | Latest | Must be running before `dotnet run` - check the system tray icon |
| **Ollama** | Latest | Pulled automatically by Aspire on first run; or install from [ollama.com](https://ollama.com) for standalone mode |

> **No Azure required.** Everything runs locally.

### Verify prerequisites manually

```bash
# .NET SDK
dotnet --version
# Expected: 10.x.x

# Docker
docker info
# Expected: prints engine info without errors

# Ollama (standalone mode only)
ollama list
# Expected: table showing llama3.2 and nomic-embed-text
```

If models are missing in standalone mode, pull them:

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
```

---

## Quick Start

### 1. Clone and restore

```bash
git clone <repo-url>
cd ConferenceAssistant
dotnet restore
```

### 2. Run via Aspire

```bash
cd src/ConferenceAssistant.AppHost
dotnet run
```

Aspire starts all containers automatically:
- **Ollama** - pulls `llama3.2` and `nomic-embed-text` on first run (may take a few minutes)
- **Qdrant** - vector store on port 6334
- **PostgreSQL** - relational store on port 5432
- **Aspire Dashboard** - http://localhost:15888

### 3. Open the app

The Aspire dashboard shows the web app URL (usually `https://localhost:7xxx`). Three views:

| URL | Who uses it |
|---|---|
| `/` | Home - links to all views |
| `/presenter` | **Speaker** - controls topics, polls, analysis |
| `/display` | **Projector** - large-screen view with live charts |
| `/session/AICONF` | **Audience** - join on any phone/laptop |

### 4. Running without Aspire (standalone)

```bash
cd src/ConferenceAssistant.Web
dotnet run
```

Requires Ollama and optionally Qdrant running locally. Start each service first:

**Ollama** - install from [ollama.com](https://ollama.com), then pull the required models:

```bash
ollama pull llama3.2
ollama pull nomic-embed-text
```

**Qdrant** (optional - the app falls back to InMemory if not running):

```bash
# Start Qdrant with a persistent data volume
docker run -d \
  --name qdrant \
  -p 6333:6333 \
  -p 6334:6334 \
  -v qdrant_storage:/qdrant/storage \
  qdrant/qdrant

# Verify it is ready
curl http://localhost:6333/healthz
# Expected: {"title":"qdrant - vector search engine","version":"..."}
```

Port mapping:
- `6333` - Qdrant REST API (used by the health check and Aspire client)
- `6334` - Qdrant gRPC (used by the SK Qdrant connector for fast vector operations)

To stop and remove the container later:

```bash
docker stop qdrant && docker rm qdrant
```

> The named volume `qdrant_storage` persists your vector data across container restarts. Omit `-v qdrant_storage:/qdrant/storage` if you want an ephemeral instance.

---

## Verifying Everything is Running

### Health endpoints

The app exposes three health endpoints once started:

| Endpoint | What it checks | Returns |
|---|---|---|
| `GET /health/status` | All components - full JSON report | 200 Healthy / 503 Unhealthy |
| `GET /health/ready` | Readiness: Ollama + Qdrant + session data | 200 / 503 |
| `GET /health/alive` | Liveness only - is the process up | 200 / 503 |

### Full status report

Hit `/health/status` to see the state of every component:

```bash
curl https://localhost:7xxx/health/status
```

Example healthy response:

```json
{
  "status": "Healthy",
  "timestamp": "2026-05-13T12:00:00Z",
  "duration": "234ms",
  "components": {
    "ollama": {
      "status": "Healthy",
      "description": "Models ready: llama3.2, nomic-embed-text",
      "duration": "120ms",
      "error": null
    },
    "qdrant": {
      "status": "Healthy",
      "description": "Qdrant is healthy at http://localhost:6333",
      "duration": "45ms",
      "error": null
    },
    "session-data": {
      "status": "Healthy",
      "description": "5 topics and 42 slides loaded. Active topic: Microsoft.Extensions.AI",
      "duration": "1ms",
      "error": null
    }
  }
}
```

### Interpreting status values

| Component status | Meaning | Action |
|---|---|---|
| `Healthy` | Working normally | Nothing to do |
| `Degraded` | Running but with limitations (e.g. models not yet pulled, Qdrant offline but InMemory in use) | See `description` in the JSON |
| `Unhealthy` | Component is down and required for the feature to work | See `error` field |

### Troubleshooting common issues

**`ollama` → Unhealthy: "Ollama unreachable"**  
Docker is not running, or the Ollama container has not started yet.  
Fix: Start Docker Desktop and wait for the Aspire dashboard to show the `ollama` container as Running.

**`ollama` → Degraded: "models not yet pulled"**  
Ollama is running but the models are still downloading (first run can take several minutes).  
Fix: Watch the `ollama` container logs in the Aspire dashboard for download progress.

**`qdrant` → Degraded: "Qdrant unreachable"**  
The app is functional (InMemory vector store is used). Qdrant is only needed for persistent vector search across restarts.  
Fix: In standalone mode, start Qdrant via Docker: `docker run -p 6333:6333 qdrant/qdrant`

**`session-data` → Degraded: "no topics loaded"**  
The data files were not found at startup.  
Fix: Ensure `data/seed-topics.json`, `data/slides.md`, and `data/session-outline.md` exist relative to the working directory.

### Aspire Dashboard checks

When running via `dotnet run` in `ConferenceAssistant.AppHost`, open the Aspire dashboard (shown in console output, usually `http://localhost:15888`):

- **Resources tab** - all services show green `Running` state
- **Logs tab** - select `web` and confirm `"Slides loaded: N slides"` appears at startup
- **Traces tab** - a request to `/health/status` should show all sub-checks with timing

---

## Configuration

`src/ConferenceAssistant.Web/appsettings.json`

```json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "ChatModel": "llama3.2",
    "EmbeddingModel": "nomic-embed-text"
  },
  "Session": {
    "Code": "AICONF",
    "OutlinePath": "data/session-outline.md",
    "TopicsPath": "data/seed-topics.json",
    "SlidesPath": "data/slides.md"
  }
}
```

To swap to a different Ollama model, change `ChatModel`. The rest of the code is model-agnostic - it uses `IChatClient` and `IEmbeddingGenerator` abstractions throughout.

---

## Project Structure

### `ConferenceAssistant.Core`

Pure domain - no AI dependencies.

| File | Purpose |
|---|---|
| `Models/SessionTopic.cs` | A talk segment with title, talking points, poll prompts, and slides |
| `Models/Poll.cs` + `PollResponse.cs` | Poll lifecycle (Draft → Active → Closed) and audience responses |
| `Models/AudienceQuestion.cs` | A submitted question with thread-safe upvote counter |
| `Models/Insight.cs` | AI-generated insight attached to a poll (analysis, trend, gap, summary) |
| `Services/SessionService.cs` | Topic navigation, slide control, Q&A management |
| `Services/PollService.cs` | Poll CRUD, launch/close, response submission, vote tallying |
| `Services/InMemoryStore.cs` | Generic thread-safe in-process store (all state lives here) |
| `Services/SlideMarkdownParser.cs` | Parses `data/slides.md` into `Slide` objects keyed to topic IDs |

### `ConferenceAssistant.Ingestion`

RAG (Retrieval-Augmented Generation) pipeline.

| File | Purpose |
|---|---|
| `Models/ConferenceRecord.cs` | Vector store document - content, source, summary, keywords, 1536-dim embedding |
| `DependencyInjection.cs` | Registers `InMemoryVectorStore`, `VectorStoreCollection`, `SemanticSearchService` |
| `SemanticSearchService.cs` | Converts query → embedding → vector search → ranked `ConferenceRecord` list |
| `Pipelines/OutlineIngestionPipeline.cs` | Reads `session-outline.md`, chunks by headings, asks LLM for summary + keywords, stores embeddings |
| `Pipelines/ResponseIngestionPipeline.cs` | Ingests poll responses with sentiment analysis into the vector store |
| `Pipelines/McpContentIngestionPipeline.cs` | Fetches content via MCP tools and ingests it |

**Ingestion flow (OutlineIngestionPipeline):**

```
session-outline.md
  → split on ## headings
  → foreach chunk:
      LLM: "summarise in one sentence"     → Summary
      LLM: "extract 3-5 keywords"          → Keywords[]
      EmbeddingGenerator: text → float[]   → Embedding (1536 dims)
      VectorStore.UpsertAsync(record)
```

### `ConferenceAssistant.Agents`

Named AI agents with fixed personas and tool sets.

| Agent | Role | Tools available |
|---|---|---|
| **SurveyArchitect** | Generates contextual poll questions | `search_knowledge`, `create_poll` |
| **ResponseAnalyst** | Reads poll results, identifies patterns | `get_poll_results`, `search_knowledge` |
| **KnowledgeCurator** | Broad semantic search across all ingested content | `search_knowledge` |

**Workflows:**

| Workflow | What it does |
|---|---|
| `PollGenerationWorkflow` | Asks SurveyArchitect to search context then create a poll |
| `ResponseAnalysisWorkflow` | ResponseAnalyst reads results → Curator finds related knowledge → Insight saved |
| `SessionSummaryWorkflow` | All three agents contribute perspectives → direct LLM call synthesises final summary |

Each agent is an `IChatClient` wrapper with a system-prompt persona. Tool calls use `AIFunctionFactory` - any `Func<>` or static method decorated appropriately becomes callable by the LLM.

### `ConferenceAssistant.Mcp`

Exposes the session as an MCP server at `POST /mcp`.

**ConferenceTools** (session-wide):

| Tool | Description |
|---|---|
| `GetSessionStatus` | Current topic, all topic statuses |
| `GetActivePoll` | Live poll question + real-time vote counts |
| `GetPollResults` | Full results + insights for a specific poll |
| `SearchSessionKnowledge` | Semantic search across all ingested content |
| `GetTopAudienceQuestions` | Questions sorted by upvotes |
| `GenerateSessionSummary` | Runs the full multi-agent summary workflow |

**KnowledgeTools** (knowledge base):

| Tool | Description |
|---|---|
| `SearchKnowledge` | Text search returning source + summary snippets |
| `GetKnowledgeStats` | Knowledge base metadata |

To use from VS Code Copilot, add to `.vscode/mcp.json`:

```json
{
  "servers": {
    "conference-pulse": {
      "type": "http",
      "url": "https://localhost:7xxx/mcp"
    }
  }
}
```

### `ConferenceAssistant.Web`

Blazor Server application. Interactive components use SignalR.

| Page | Route | Purpose |
|---|---|---|
| `Home.razor` | `/` | Session overview, navigation links |
| `Presenter.razor` | `/presenter` | Topic panel, slide control, poll generation, Q&A, analysis buttons |
| `Display.razor` | `/display` | Live poll bar chart, slide content, insights ticker |
| `Session.razor` | `/session/{code}` | Audience voting, ask questions, upvote |

### `ConferenceAssistant.AppHost`

.NET Aspire orchestrator. Declares all resources:

```csharp
var ollama = builder.AddOllama("ollama").WithDataVolume().WithOpenWebUI();
var chatModel   = ollama.AddModel("llama3.2");
var embedModel  = ollama.AddModel("nomic-embed-text");
var qdrant      = builder.AddQdrant("qdrant").WithDataVolume().WithLifetime(ContainerLifetime.Persistent);
var postgres    = builder.AddPostgres("postgres").WithPgWeb().WithDataVolume();
```

The web project receives connection strings for Qdrant and Ollama automatically via Aspire's `WithReference()` wiring.

---

## Technology Stack

| Technology | Package | Role |
|---|---|---|
| **.NET Aspire 9.2** | `Aspire.AppHost.Sdk/9.2.0` | Container orchestration, service discovery |
| **Blazor Server (.NET 10)** | Built-in | UI framework (Interactive Server rendering) |
| **Microsoft.Extensions.AI 10.6** | `Microsoft.Extensions.AI` | `IChatClient` + `IEmbeddingGenerator` abstractions |
| **OllamaSharp 5.4.25** | `OllamaSharp` | Ollama provider implementing M.E.AI interfaces |
| **VectorData 10.6** | `Microsoft.Extensions.VectorData.Abstractions` | `VectorStoreCollection<TKey, TRecord>` abstraction |
| **SK InMemory 1.74-preview** | `Microsoft.SemanticKernel.Connectors.InMemory` | In-process vector store (dev / demo) |
| **SK Qdrant 1.74-preview** | `Microsoft.SemanticKernel.Connectors.Qdrant` | Persistent vector store (production) |
| **ModelContextProtocol 1.2** | `ModelContextProtocol.AspNetCore` | MCP server (`[McpServerTool]`, `/mcp` endpoint) |

---

## Data Files (`data/`)

| File | Purpose |
|---|---|
| `session-outline.md` | Full 5-segment talk outline - ingested as RAG content on startup |
| `seed-topics.json` | Ordered list of `SessionTopic` objects (IDs, titles, talking points, poll hints) |
| `slides.md` | Slide content in markdown format, keyed to topic IDs |

---

## Key Design Decisions

**Why OllamaSharp instead of `Microsoft.Extensions.AI.Ollama`?**  
`M.E.AI.Ollama` was deprecated in favour of OllamaSharp, which ships `OllamaApiClient` implementing both `IChatClient` and `IEmbeddingGenerator<string, Embedding<float>>` directly.

**Why `VectorStoreCollection<TKey,TRecord>` instead of an interface?**  
In `Microsoft.Extensions.VectorData` v10, `IVectorStoreRecordCollection` was replaced by the abstract class `VectorStoreCollection<TKey,TRecord>`. All code uses this type directly.

**Why in-memory state instead of a database?**  
Conference sessions are ephemeral. All domain state (`SessionService`, `PollService`) lives in `InMemoryStore<T>` for zero-latency reads on a Blazor SignalR circuit. PostgreSQL is wired up but reserved for future persistence (audit log, exported summaries).

**Why three agents + a synthesiser in SessionSummaryWorkflow?**  
Each agent has a distinct lens (engagement, understanding, themes). The final LLM call gets richer, more diverse input than a single prompt would produce, resulting in a more nuanced closing summary.

---

## Running Tests

```bash
dotnet test
```

Test projects:
- `tests/ConferenceAssistant.Agents.Tests`
- `tests/ConferenceAssistant.Ingestion.Tests`
- `tests/ConferenceAssistant.Mcp.Tests`

---

## Session Flow (What Happens Live)

```
App starts
  └─ LoadTopicsAsync("data/seed-topics.json")
  └─ LoadSlidesAsync("data/slides.md")
  └─ OutlineIngestionPipeline.IngestAsync("data/session-outline.md")
       └─ chunks embedded and stored in vector store

Presenter opens /presenter
  └─ Sees current topic, talking points, slide navigator

Presenter clicks "Generate Poll"
  └─ PollGenerationWorkflow.GeneratePollAsync(topicId, topicTitle)
       └─ SurveyArchitect.RunAsync(prompt)
            └─ LLM calls search_knowledge → finds relevant outline chunks
            └─ LLM calls create_poll → Poll saved, status = Active

Audience at /session/AICONF votes
  └─ PollService.SubmitResponse(pollId, selectedOption)

Presenter clicks "Close & Analyse"
  └─ ResponseAnalysisWorkflow runs
       └─ ResponseIngestionPipeline ingests each response (sentiment + embedding)
       └─ ResponseAnalyst agent analyses results → Insight saved
       └─ KnowledgeCurator searches related knowledge → Insight saved

Display view shows insight ticker in real time

Presenter clicks "Generate Summary" at end
  └─ SessionSummaryWorkflow.GenerateSummaryAsync()
       └─ SurveyArchitect: engagement perspective
       └─ ResponseAnalyst: audience understanding perspective
       └─ KnowledgeCurator: thematic connections
       └─ Final LLM call synthesises all three → rich closing summary displayed
```
