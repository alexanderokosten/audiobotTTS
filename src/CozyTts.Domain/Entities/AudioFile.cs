namespace CozyTts.Domain.Entities;

public sealed class AudioFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid JobId { get; set; }

    public VoiceGenerationJob? Job { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string FilePath { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public double? DurationSeconds { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
