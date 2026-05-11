namespace CozyTts.Application.Common;

public sealed class DialogueLine(string? speaker, string text)
{
    public string? Speaker { get; } = speaker;

    public string Text { get; set; } = text;
}
