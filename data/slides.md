---
title: Conference Assistant - The Living Presentation
subtitle: A .NET AI demo powered by Ollama
date: 2026
---

<!-- topic: meai -->
<!-- layout: centered -->
# Conference Assistant

## The Living Presentation

<!-- speaker: Welcome! This entire app is the demo. Everything you see is powered by the .NET AI stack running locally with Ollama. No cloud required. -->

---

<!-- topic: meai -->
# Microsoft.Extensions.AI

## The Abstraction Layer

<!-- speaker: Start here. IChatClient is the key insight - one interface, any model. -->

---

<!-- topic: meai -->
# Why Abstractions Matter

- Swap Ollama for GPT-4 with **one line** of config
- `IChatClient` - the universal chat interface
- `IEmbeddingGenerator` - turn text into vectors
- `ChatClientBuilder` - middleware pipeline (logging, caching, tools)
- `AIFunctionFactory` - any C# method becomes an AI tool

<!-- speaker: Demo: show the ChatClientBuilder registration in Program.cs -->

---

<!-- topic: meai -->
# IChatClient in Action

```csharp
// Register Ollama (or swap to any provider)
builder.Services.AddChatClient(
    new OllamaChatClient(new Uri("http://localhost:11434"), "llama3.2"))
    .UseFunctionInvocation()
    .Build();

// Use it anywhere via DI
var response = await chatClient.GetResponseAsync(
    "Summarize this session topic", ct);
```

<!-- speaker: Point out: same code works with Azure OpenAI, just change the first line. -->

---

<!-- topic: knowledge -->
# Data Ingestion & Vector Search

## Feeding the Brain

<!-- speaker: Transition to the knowledge layer. This is where the snowball starts. -->

---

<!-- topic: knowledge -->
# The RAG Pipeline

- **Read** - load markdown documents
- **Chunk** - split by headings into digestible pieces
- **Enrich** - AI generates summaries and keywords per chunk
- **Embed** - convert text to 768-dimensional vectors (nomic-embed-text)
- **Store** - save in in-memory vector store (swappable to Qdrant)

<!-- speaker: The snowball effect: each segment adds more context to the vector store. -->

---

<!-- topic: knowledge -->
# Semantic Search

```csharp
// Search by meaning, not keywords
var results = await searchService.SearchAsync(
    "what did the audience think about abstractions?", topK: 5);

// Returns the most semantically similar ConferenceRecords
foreach (var record in results)
    Console.WriteLine($"[{record.Source}] {record.Summary}");
```

<!-- speaker: Demo: type a query in the presenter view and show live results. -->

---

<!-- topic: agents -->
# Agent Framework

## The Intelligence Layer

<!-- speaker: Now we give the AI agency. It can decide what tools to call. -->

---

<!-- topic: agents -->
# Three Specialized Agents

- 🏗️ **SurveyArchitect** - generates context-aware polls using `search_knowledge`
- 📊 **ResponseAnalyst** - interprets results, stores insights
- 📚 **KnowledgeCurator** - RAG specialist, bridges local + external knowledge

<!-- speaker: Each agent has a persona, instructions, and a specific set of tools. -->

---

<!-- topic: agents -->
# Agent Tool Use

```csharp
// SurveyArchitect uses search_knowledge before creating polls
var agent = new ConferenceAgent(
    name: "SurveyArchitect",
    instructions: "Search for context first, then generate a poll...",
    chatClient,
    tools: [createPollTool, searchKnowledgeTool]);

var result = await agent.RunAsync(
    $"Generate a poll for topic: {topic.Title}");
```

<!-- speaker: The agent decides when to call tools - classic ReAct pattern. -->

---

<!-- topic: agents -->
# Multi-Agent Workflow

```
Poll closes
  → ResponseAnalyst: "Analyze poll {id}"
      → calls get_poll_results + search_knowledge + store_insight
  → KnowledgeCurator: "Enrich this insight"
      → searches vector store + external MCP sources
  → Insight pushed to display in real-time
```

<!-- speaker: Show the agent activity in the presenter view right column. -->

---

<!-- topic: mcp -->
# Model Context Protocol

## The Bridge

<!-- speaker: MCP is the USB-C of AI. Any client, any server, standard protocol. -->

---

<!-- topic: mcp -->
# Our App as MCP Server

- **10 tools** at `/mcp`
- `get_session_status` - current topic and all poll statuses
- `get_active_poll` - live poll with vote counts
- `search_session_knowledge` - semantic search over all ingested data
- `get_audience_questions` - top questions by upvotes
- `generate_session_summary` - triggers the full multi-agent workflow

<!-- speaker: Any MCP client can now query our live session data. -->

---

<!-- topic: mcp -->
# MCP Tool Registration

```csharp
// Server: expose tools via attribute
[McpServerToolType]
public class ConferenceTools(PollService polls, SemanticSearchService search)
{
    [McpServerTool, Description("Semantic search over session knowledge")]
    public async Task<IReadOnlyList<object>> SearchSessionKnowledge(string query)
        => (await search.SearchAsync(query, 5))
            .Select(r => new { r.Content, r.Source } as object)
            .ToList();
}

// Wire up in Program.cs
app.MapMcp("/mcp");
```

<!-- speaker: Two lines to expose any C# class as an MCP server. -->

---

<!-- topic: mcp -->
# Connect with VS Code Copilot

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "ConferenceAssistant": {
      "type": "http",
      "url": "http://localhost:5000/mcp"
    }
  }
}
```

Then ask Copilot: **"Summarize this session including all poll results"**

<!-- speaker: Open VS Code. Show the mcp.json. Ask Copilot the question. Watch it call our tools. -->

---

<!-- topic: closer -->
<!-- layout: centered -->
# The Living Presentation Closer

## All Six Technologies. One Command.

<!-- speaker: This is the moment everything comes together. Build the anticipation. -->

---

<!-- topic: closer -->
# The Cascade

```
"Summarize this session" (via MCP)
  → MCP server receives the request
  → SessionSummaryWorkflow triggered
      → SurveyArchitect: "What engagement patterns did you see?"
      → ResponseAnalyst: "What did the polls reveal?"
      → KnowledgeCurator: searches all ingested knowledge
  → IChatClient synthesizes all perspectives
  → Streaming summary returned via MCP
  → Displayed live on screen
```

<!-- speaker: Every technology fires. Point at the architecture diagram as you describe each hop. -->

---

<!-- topic: closer -->
# The Snowball Effect

| Segment | Knowledge Available |
|---------|---------------------|
| 1 - M.E.AI | Session outline only |
| 2 - Knowledge | + poll responses, enriched chunks |
| 3 - Agents | + AI insights, audience patterns |
| 4 - MCP | + external docs (Microsoft Learn) |
| **Closer** | **Full knowledge base - richest context** |

<!-- speaker: The final summary is the richest because every earlier segment fed the vector store. -->

---

<!-- topic: closer -->
<!-- layout: centered -->
# One Command. Six Technologies.

## Working Together.

> Not a presentation *about* AI - a presentation *that is* AI.

<!-- speaker: Pause for effect. Let the audience absorb it. Then take questions. -->
