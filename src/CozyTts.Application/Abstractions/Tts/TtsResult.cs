namespace CozyTts.Application.Abstractions.Tts;

public sealed record TtsResult(
    string FilePath,
    string MimeType,
    long SizeBytes,
    double? DurationSeconds,
    string TempDirectory);
