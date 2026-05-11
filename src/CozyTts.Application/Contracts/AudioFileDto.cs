namespace CozyTts.Application.Contracts;

public sealed record AudioFileDto(
    Guid Id,
    Guid JobId,
    string FileName,
    string FilePath,
    string MimeType,
    long SizeBytes,
    double? DurationSeconds,
    DateTimeOffset CreatedAt);
