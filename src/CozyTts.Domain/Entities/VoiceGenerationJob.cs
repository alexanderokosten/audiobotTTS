using CozyTts.Domain.Enums;

namespace CozyTts.Domain.Entities;

public sealed class VoiceGenerationJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid ProjectId { get; set; }

    public VoiceProject? Project { get; set; }

    public GenerationStatus Status { get; set; } = GenerationStatus.Pending;

    public Guid VoiceProfileId { get; set; }

    public VoiceProfile? VoiceProfile { get; set; }

    public SpeechSpeed Speed { get; set; } = SpeechSpeed.Normal;

    public AudioOutputFormat OutputFormat { get; set; } = AudioOutputFormat.Mp3;

    public string? Language { get; set; }

    public string? EmotionPrompt { get; set; }

    public bool UseDialogueVoices { get; set; }

    public string? SpeakerVoiceProfileCodesJson { get; set; }

    public string? QueueJobId { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public AudioFile? AudioFile { get; set; }
}
