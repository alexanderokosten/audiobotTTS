using System.Text.RegularExpressions;

namespace CozyTts.Application.Common;

public static partial class DialogueScriptParser
{
    public static IReadOnlyList<DialogueLine> Parse(string sourceText)
    {
        var lines = sourceText
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n', StringSplitOptions.TrimEntries);

        var result = new List<DialogueLine>();
        DialogueLine? last = null;

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var match = SpeakerLineRegex().Match(line);
            if (match.Success)
            {
                last = new DialogueLine(
                    NormalizeSpeaker(match.Groups["speaker"].Value),
                    match.Groups["text"].Value.Trim());
                result.Add(last);
                continue;
            }

            if (last is not null)
            {
                last.Text = $"{last.Text} {line.Trim()}";
            }
            else
            {
                last = new DialogueLine(null, line.Trim());
                result.Add(last);
            }
        }

        return result.Where(item => !string.IsNullOrWhiteSpace(item.Text)).ToList();
    }

    public static string NormalizeSpeaker(string speaker)
    {
        return string.Join(' ', speaker.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"^(?<speaker>\p{L}[\p{L}\p{N} _.'-]{0,48})\s*:\s*(?<text>.+)$", RegexOptions.Compiled)]
    private static partial Regex SpeakerLineRegex();
}
