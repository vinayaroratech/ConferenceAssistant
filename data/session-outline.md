# Conference Assistant - Session Outline

## Session Overview

**Title:** Conference Assistant - The Living Presentation  
**Duration:** 45–60 minutes  
**Format:** Live interactive demo - the app IS the presentation  

---

## Segment 1: Microsoft.Extensions.AI (~10 min)

### Key Message
One abstraction. Any LLM. Swap Ollama for GPT-4 with one line.

### Demo Points
- Show `IChatClient` registration with Ollama in `Program.cs`
- Explain `ChatClientBuilder` middleware pipeline (function invocation enabled)
- Demonstrate `IEmbeddingGenerator` - embeddings are just vectors of floats
- Show `AIFunctionFactory.Create()` - any C# lambda becomes an AI tool

### Poll Trigger
After explaining abstractions, generate a poll about which aspect resonates most.

---

## Segment 2: Data Ingestion & Vector Search (~10 min)

### Key Message
The RAG pipeline: text becomes searchable meaning, not just keywords.

### Demo Points
- Walk through `OutlineIngestionPipeline`: read → chunk → enrich → embed → store
- Show the `ConferenceRecord` vector model (768 dimensions for nomic-embed-text)
- Demonstrate `SemanticSearchService.SearchAsync()` live in the presenter view
- Show how ingesting poll responses builds richer context

### Poll Trigger
After explaining the pipeline, generate a poll about RAG component value.

---

## Segment 3: Agent Framework (~10 min)

### Key Message
Agents decide which tools to call based on context - not just text generation.

### Demo Points
- Show `SurveyArchitectAgent.Instructions` - the persona and tool list
- Trigger `PollGenerationWorkflow` live - watch it call `search_knowledge` first
- Close the poll - watch `ResponseAnalysisWorkflow` run analyst → curator
- Show the insight appear in real-time on the Display view

### Poll Trigger
After showing agent tool use, poll on which agent role is most interesting.

---

## Segment 4: Model Context Protocol (~10 min)

### Key Message
MCP = USB-C for AI. Standard protocol, any client, any server.

### Demo Points
- Show `ConferenceTools` class with `[McpServerTool]` attributes
- Open VS Code, show `mcp.json` pointing at our `/mcp` endpoint
- Ask Copilot a question that triggers `search_session_knowledge`
- Show MCP client calling Microsoft Learn docs in real-time

### Poll Trigger
Poll on most valuable MCP integration scenario.

---

## Segment 5: The Closer (~5 min)

### Key Message
All six technologies fire in sequence. The final summary is the richest possible.

### Demo Points
- Ask Copilot: "Generate a comprehensive summary of this session"
- Watch the cascade: MCP → `SessionSummaryWorkflow` → 3 agents → synthesize
- Show the streaming summary appearing on the Display view
- Highlight the snowball effect: every segment contributed

---

## Key Takeaways

1. **`Microsoft.Extensions.AI`** - standardized AI abstractions for .NET
2. **Data Ingestion + VectorData** - RAG made simple with the pipeline pattern
3. **Agent Framework** - tool-using agents for autonomous workflows
4. **MCP** - the standard way to connect AI to your data and tools
5. **The snowball effect** - each feature makes the app smarter
6. **Open source** - everything running locally with Ollama, no cloud required
