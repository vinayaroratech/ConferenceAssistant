using System.Text.RegularExpressions;
using ConferenceAssistant.Core.Models;

namespace ConferenceAssistant.Core.Services;

public static class SlideMarkdownParser
{
    private static readonly Regex SpeakerNotesRegex = new(
        @"<!--\s*speaker\s*:(.*?)-->",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex TopicRegex = new(
        @"<!--\s*topic\s*:\s*(.*?)\s*-->",
        RegexOptions.Compiled);

    private static readonly Regex LayoutRegex = new(
        @"<!--\s*layout\s*:\s*(.*?)\s*-->",
        RegexOptions.Compiled);

    private static readonly Regex FencedCodeBlockRegex = new(
        @"^```(\w*)\s*\n(.*?)^```",
        RegexOptions.Multiline | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex H1Regex = new(
        @"^#\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex H2Regex = new(
        @"^##\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    private static readonly Regex BulletRegex = new(
        @"^[\-\*]\s+(.+)$", RegexOptions.Multiline | RegexOptions.Compiled);

    public static async Task<List<Slide>> ParseFileAsync(string filePath)
    {
        var markdown = await File.ReadAllTextAsync(filePath);
        return Parse(markdown);
    }

    public static List<Slide> Parse(string markdown)
    {
        markdown = markdown.Replace("\r\n", "\n").Replace("\r", "\n");

        var blocks = Regex.Split(markdown, @"^---\s*$", RegexOptions.Multiline);

        // Skip YAML frontmatter if present (starts with ---)
        int startIndex = 0;
        if (markdown.TrimStart().StartsWith("---"))
            startIndex = 2; // skip empty prefix + frontmatter body

        var slides = new List<Slide>();
        string? currentTopicId = null;
        int order = 0;

        for (int i = startIndex; i < blocks.Length; i++)
        {
            var block = blocks[i];
            if (string.IsNullOrWhiteSpace(block)) continue;

            // Extract topic (sticky)
            var topicMatch = TopicRegex.Match(block);
            if (topicMatch.Success)
                currentTopicId = topicMatch.Groups[1].Value.Trim();

            // Extract layout
            var layoutMatch = LayoutRegex.Match(block);
            var layout = SlideLayout.Default;
            if (layoutMatch.Success)
            {
                layout = layoutMatch.Groups[1].Value.Trim().ToLowerInvariant() switch
                {
                    "centered" => SlideLayout.Centered,
                    "two-column" => SlideLayout.TwoColumn,
                    _ => SlideLayout.Default
                };
            }

            // Extract speaker notes
            var speakerMatch = SpeakerNotesRegex.Match(block);
            var speakerNotes = speakerMatch.Success ? speakerMatch.Groups[1].Value.Trim() : "";

            // Strip all HTML comments from visible content
            var visible = Regex.Replace(block, @"<!--.*?-->", "", RegexOptions.Singleline).Trim();

            if (string.IsNullOrWhiteSpace(visible))
            {
                if (!string.IsNullOrEmpty(speakerNotes))
                {
                    slides.Add(new Slide
                    {
                        TopicId = currentTopicId,
                        Order = order++,
                        Type = SlideType.Blank,
                        Layout = layout,
                        SpeakerNotes = speakerNotes
                    });
                }
                continue;
            }

            var slide = new Slide
            {
                TopicId = currentTopicId,
                Order = order++,
                Layout = layout,
                SpeakerNotes = speakerNotes
            };

            // H1
            var h1Match = H1Regex.Match(visible);
            // H2
            var h2Match = H2Regex.Match(visible);
            // Code
            var codeMatch = FencedCodeBlockRegex.Match(visible);
            // Bullets
            var bulletMatches = BulletRegex.Matches(visible);

            if (h1Match.Success && h2Match.Success && !codeMatch.Success && bulletMatches.Count == 0)
            {
                slide.Type = SlideType.Title;
                slide.Layout = layout == SlideLayout.Default ? SlideLayout.Centered : layout;
                slide.Title = h1Match.Groups[1].Value.Trim();
                slide.Subtitle = h2Match.Groups[1].Value.Trim();
            }
            else if (h1Match.Success && !h2Match.Success && !codeMatch.Success && bulletMatches.Count == 0)
            {
                slide.Type = SlideType.Section;
                slide.Title = h1Match.Groups[1].Value.Trim();
            }
            else if (codeMatch.Success)
            {
                slide.Type = SlideType.Code;
                slide.Title = h1Match.Success ? h1Match.Groups[1].Value.Trim() : "";
                slide.CodeLanguage = codeMatch.Groups[1].Value.Trim();
                slide.CodeSnippet = codeMatch.Groups[2].Value.Trim();
            }
            else if (bulletMatches.Count > 0)
            {
                slide.Type = SlideType.Content;
                slide.Title = h1Match.Success ? h1Match.Groups[1].Value.Trim() : "";
                slide.Bullets = bulletMatches.Select(m => m.Groups[1].Value.Trim()).ToList();
            }
            else
            {
                slide.Type = SlideType.Content;
                slide.Title = h1Match.Success ? h1Match.Groups[1].Value.Trim() : "";
                slide.BodyMarkdown = visible;
            }

            slides.Add(slide);
        }

        return slides;
    }
}
