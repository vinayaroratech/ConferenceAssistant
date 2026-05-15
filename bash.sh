#!/usr/bin/env bash
# scaffold.sh — Creates the full ConferenceAssistant folder/file structure (empty files).
# Run from any directory; all paths are created relative to the script's location.
# Usage: bash scaffold.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT"

files=(
  # Root
  ".gitignore"
  "ConferenceAssistant.slnx"
  "Directory.Build.props"
  "Directory.Packages.props"
  "README.md"
  "global.json"

  # Data
  "data/conferences.json"
  "data/sessions.json"
  "data/session-outline.md"
  "data/slides.md"
  "data/dotnet26-slides.md"
  "data/cloudnative-slides.md"

  # ConferenceAssistant.Agents
  "src/ConferenceAssistant.Agents/ConferenceAssistant.Agents.csproj"
  "src/ConferenceAssistant.Agents/Agents.cs"
  "src/ConferenceAssistant.Agents/DependencyInjection.cs"
  "src/ConferenceAssistant.Agents/Tools/InsightTools.cs"
  "src/ConferenceAssistant.Agents/Tools/KnowledgeTools.cs"
  "src/ConferenceAssistant.Agents/Tools/PollTools.cs"
  "src/ConferenceAssistant.Agents/Workflows/PollGenerationWorkflow.cs"
  "src/ConferenceAssistant.Agents/Workflows/QuestionAnswerWorkflow.cs"
  "src/ConferenceAssistant.Agents/Workflows/ResponseAnalysisWorkflow.cs"
  "src/ConferenceAssistant.Agents/Workflows/SessionSummaryWorkflow.cs"

  # ConferenceAssistant.AppHost
  "src/ConferenceAssistant.AppHost/ConferenceAssistant.AppHost.csproj"
  "src/ConferenceAssistant.AppHost/Program.cs"
  "src/ConferenceAssistant.AppHost/Properties/launchSettings.json"

  # ConferenceAssistant.CopilotDemo
  "src/ConferenceAssistant.CopilotDemo/ConferenceAssistant.CopilotDemo.csproj"
  "src/ConferenceAssistant.CopilotDemo/Program.cs"

  # ConferenceAssistant.Core
  "src/ConferenceAssistant.Core/ConferenceAssistant.Core.csproj"
  "src/ConferenceAssistant.Core/Models/AudienceQuestion.cs"
  "src/ConferenceAssistant.Core/Models/Conference.cs"
  "src/ConferenceAssistant.Core/Models/Insight.cs"
  "src/ConferenceAssistant.Core/Models/Poll.cs"
  "src/ConferenceAssistant.Core/Models/PollResponse.cs"
  "src/ConferenceAssistant.Core/Models/SessionTopic.cs"
  "src/ConferenceAssistant.Core/Models/Slide.cs"
  "src/ConferenceAssistant.Core/Services/AgentActivityService.cs"
  "src/ConferenceAssistant.Core/Services/ConferenceRegistry.cs"
  "src/ConferenceAssistant.Core/Services/InMemoryStore.cs"
  "src/ConferenceAssistant.Core/Services/PollService.cs"
  "src/ConferenceAssistant.Core/Services/SessionService.cs"
  "src/ConferenceAssistant.Core/Services/SessionSummaryService.cs"
  "src/ConferenceAssistant.Core/Services/SlideMarkdownParser.cs"

  # ConferenceAssistant.Ingestion
  "src/ConferenceAssistant.Ingestion/ConferenceAssistant.Ingestion.csproj"
  "src/ConferenceAssistant.Ingestion/DependencyInjection.cs"
  "src/ConferenceAssistant.Ingestion/Models/ConferenceRecord.cs"
  "src/ConferenceAssistant.Ingestion/Pipelines/McpContentIngestionPipeline.cs"
  "src/ConferenceAssistant.Ingestion/Pipelines/OutlineIngestionPipeline.cs"
  "src/ConferenceAssistant.Ingestion/Pipelines/ResponseIngestionPipeline.cs"
  "src/ConferenceAssistant.Ingestion/SemanticSearchService.cs"

  # ConferenceAssistant.Mcp
  "src/ConferenceAssistant.Mcp/ConferenceAssistant.Mcp.csproj"
  "src/ConferenceAssistant.Mcp/DependencyInjection.cs"
  "src/ConferenceAssistant.Mcp/Server/ConferenceTools.cs"
  "src/ConferenceAssistant.Mcp/Tools/KnowledgeTools.cs"

  # ConferenceAssistant.ServiceDefaults
  "src/ConferenceAssistant.ServiceDefaults/ConferenceAssistant.ServiceDefaults.csproj"
  "src/ConferenceAssistant.ServiceDefaults/Extensions.cs"

  # ConferenceAssistant.Web — Components
  "src/ConferenceAssistant.Web/Components/App.razor"
  "src/ConferenceAssistant.Web/Components/Routes.razor"
  "src/ConferenceAssistant.Web/Components/_Imports.razor"
  "src/ConferenceAssistant.Web/Components/Layout/DashboardLayout.razor"
  "src/ConferenceAssistant.Web/Components/Layout/MainLayout.razor"
  "src/ConferenceAssistant.Web/Components/Layout/MainLayout.razor.css"
  "src/ConferenceAssistant.Web/Components/Layout/NavMenu.razor"
  "src/ConferenceAssistant.Web/Components/Layout/NavMenu.razor.css"
  "src/ConferenceAssistant.Web/Components/Layout/PresentationLayout.razor"
  "src/ConferenceAssistant.Web/Components/Layout/ReconnectModal.razor"
  "src/ConferenceAssistant.Web/Components/Layout/ReconnectModal.razor.css"
  "src/ConferenceAssistant.Web/Components/Layout/ReconnectModal.razor.js"
  "src/ConferenceAssistant.Web/Components/Pages/Analytics.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Display.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Error.razor"
  "src/ConferenceAssistant.Web/Components/Pages/HealthDashboard.razor"
  "src/ConferenceAssistant.Web/Components/Pages/HealthDashboard.razor.css"
  "src/ConferenceAssistant.Web/Components/Pages/Home.razor"
  "src/ConferenceAssistant.Web/Components/Pages/NotFound.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Notes.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Organizer.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Presenter.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Preview.razor"
  "src/ConferenceAssistant.Web/Components/Pages/Session.razor"
  "src/ConferenceAssistant.Web/Components/Pages/VectorStoreBrowser.razor"
  "src/ConferenceAssistant.Web/Components/Pages/VectorStoreBrowser.razor.css"
  "src/ConferenceAssistant.Web/Components/Pages/Watch.razor"
  "src/ConferenceAssistant.Web/Components/Shared/SlideRenderer.razor"

  # ConferenceAssistant.Web — project files
  "src/ConferenceAssistant.Web/ConferenceAssistant.Web.csproj"
  "src/ConferenceAssistant.Web/Program.cs"
  "src/ConferenceAssistant.Web/QrCodeHelper.cs"
  "src/ConferenceAssistant.Web/appsettings.json"
  "src/ConferenceAssistant.Web/appsettings.Development.json"
  "src/ConferenceAssistant.Web/Properties/launchSettings.json"

  # ConferenceAssistant.Web — health checks
  "src/ConferenceAssistant.Web/HealthChecks/HealthStatusWriter.cs"
  "src/ConferenceAssistant.Web/HealthChecks/OllamaHealthCheck.cs"
  "src/ConferenceAssistant.Web/HealthChecks/QdrantHealthCheck.cs"
  "src/ConferenceAssistant.Web/HealthChecks/SessionDataHealthCheck.cs"

  # ConferenceAssistant.Web — wwwroot
  "src/ConferenceAssistant.Web/wwwroot/app.css"
  "src/ConferenceAssistant.Web/wwwroot/favicon.png"

  # Tests
  "tests/ConferenceAssistant.Agents.Tests/ConferenceAssistant.Agents.Tests.csproj"
  "tests/ConferenceAssistant.Agents.Tests/UnitTest1.cs"
  "tests/ConferenceAssistant.Ingestion.Tests/ConferenceAssistant.Ingestion.Tests.csproj"
  "tests/ConferenceAssistant.Ingestion.Tests/UnitTest1.cs"
  "tests/ConferenceAssistant.Mcp.Tests/ConferenceAssistant.Mcp.Tests.csproj"
  "tests/ConferenceAssistant.Mcp.Tests/UnitTest1.cs"
)

created=0
skipped=0

for f in "${files[@]}"; do
  if [ -e "$f" ]; then
    echo "  skip  $f"
    ((skipped++)) || true
  else
    mkdir -p "$(dirname "$f")"
    touch "$f"
    echo "  create $f"
    ((created++)) || true
  fi
done

echo ""
echo "Done. Created: $created  |  Skipped (already exist): $skipped"
