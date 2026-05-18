using System.Text.Json;
using System.Text.Json.Nodes;
using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public class ConferenceRegistry : IConferenceRegistry
{
    private List<Conference> _conferences = [];

    public IReadOnlyList<Conference> All => _conferences.AsReadOnly();

    public Conference? Default
        => _conferences.FirstOrDefault(c => c.IsDefault) ?? _conferences.FirstOrDefault();

    public Conference? GetByCode(string code)
        => _conferences.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public async Task LoadAsync(string jsonPath)
    {
        if (!File.Exists(jsonPath)) return;
        var json = await File.ReadAllTextAsync(jsonPath);
        _conferences = JsonSerializer.Deserialize<List<Conference>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        // Load lightweight topic previews for every conference so the Home page
        // can show each conference's agenda without switching sessions.
        foreach (var conf in _conferences)
        {
            var topicsFile = ResolveDataFile(conf.TopicsPath);
            if (topicsFile is null) continue;
            try
            {
                var topicsJson = await File.ReadAllTextAsync(topicsFile);
                var root = JsonNode.Parse(topicsJson);

                // Resolve the correct topics array, handling three formats:
                //   1. Multi-session array:  [{sessionId, topics:[...]}, ...]
                //   2. Single-session object: {sessionId, topics:[...]}
                //   3. Legacy flat array:     [{id, title, ...}, ...]
                JsonArray? nodes = null;

                if (root is JsonArray rootArray)
                {
                    var first = rootArray.FirstOrDefault();
                    bool isMultiSession = first is JsonObject fo && fo["sessionId"] is not null;

                    if (isMultiSession)
                    {
                        // Find the matching session by sessionId (conf.SessionId) or fall back to first
                        var matchId = conf.SessionId.Length > 0 ? conf.SessionId : conf.Code;
                        JsonObject? sessionObj = null;
                        foreach (var elem in rootArray)
                        {
                            if (elem is JsonObject jo &&
                                jo["sessionId"]?.GetValue<string>()?.Equals(matchId, StringComparison.OrdinalIgnoreCase) == true)
                            {
                                sessionObj = jo;
                                break;
                            }
                        }
                        sessionObj ??= rootArray.FirstOrDefault() as JsonObject;
                        nodes = sessionObj?["topics"]?.AsArray();
                    }
                    else
                    {
                        // Legacy flat array of topic objects
                        nodes = rootArray;
                    }
                }
                else if (root is JsonObject rootObj)
                {
                    // Single-session wrapper object
                    nodes = rootObj["topics"]?.AsArray();
                }

                if (nodes is null) continue;
                foreach (var node in nodes)
                {
                    var order = node?["order"]?.GetValue<int>() ?? 0;
                    var title = node?["title"]?.GetValue<string>() ?? "";
                    var desc  = node?["description"]?.GetValue<string>() ?? "";
                    conf.TopicPreviews.Add(new TopicPreview(order, title, desc));
                }
            }
            catch { /* topic file unreadable - skip previews for this conf */ }
        }
    }

    /// <summary>Walks up from the current working directory to find a data file.</summary>
    public static string? ResolveDataFile(string relativePath)
    {
        var dir = Directory.GetCurrentDirectory();
        for (var i = 0; i < 6; i++)
        {
            var candidate = Path.Combine(dir, relativePath);
            if (File.Exists(candidate)) return candidate;
            var parent = Directory.GetParent(dir);
            if (parent is null) break;
            dir = parent.FullName;
        }
        return null;
    }
}
