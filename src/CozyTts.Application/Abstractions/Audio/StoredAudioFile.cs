namespace CozyTts.Application.Abstractions.Audio;

public sealed record StoredAudioFile(
    string FileName,
    string FilePath,
    string MimeType,
    long SizeBytes,
    double? DurationSeconds);
