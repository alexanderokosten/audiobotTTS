namespace CozyTts.Domain.Entities;

public sealed class VoiceProject
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;

    public string SourceText { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<VoiceGenerationJob> Jobs { get; set; } = new List<VoiceGenerationJob>();
}
