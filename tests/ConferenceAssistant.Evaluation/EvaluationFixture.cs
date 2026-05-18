using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using OllamaSharp;

namespace ConferenceAssistant.Evaluation;

/// <summary>
/// Shared Ollama setup used across all evaluation tests.
/// All tests in the "Evaluation" collection use this fixture.
/// Tests are skipped (not failed) if Ollama is unreachable or the model is not loaded.
/// </summary>
public sealed class EvaluationFixture : IAsyncLifetime
{
    private const string OllamaEndpoint = "http://localhost:11434";
    private const string ChatModel = "llama3.2";

    public IChatClient ChatClient { get; private set; } = null!;

    /// <summary>Chat configuration used by quality evaluators as the judge model.</summary>
    public ChatConfiguration EvalConfig { get; private set; } = null!;

    /// <summary>False if Ollama is unreachable - tests call <c>Assert.Skip</c> when this is false.</summary>
    public bool OllamaAvailable { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            // Store as IChatClient first so GetResponseAsync resolves to the M.E.AI extension method
            IChatClient chatClient = new OllamaApiClient(new Uri(OllamaEndpoint), ChatModel);

            // Probe: a quick single-word response verifies the model is loaded
            var probe = await chatClient.GetResponseAsync(
                new List<ChatMessage> { new(ChatRole.User, "Reply only with the word READY.") });

            OllamaAvailable = !string.IsNullOrWhiteSpace(probe.Text);
            ChatClient = chatClient;
            EvalConfig = new ChatConfiguration(chatClient);
        }
        catch
        {
            OllamaAvailable = false;
        }
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

[CollectionDefinition("Evaluation")]
public class EvaluationCollection : ICollectionFixture<EvaluationFixture>;
