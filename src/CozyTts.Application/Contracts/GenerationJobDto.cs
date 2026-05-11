namespace CozyTts.Application.Contracts;

public sealed record GenerationJobDto(
    Guid Id,
    Guid ProjectId,
    string Status,
    Guid VoiceProfileId,
    string VoiceProfileCode,
    string VoiceProfileName,
    string Speed,
    string OutputFormat,
    string? Language,
    string? EmotionPrompt,
    bool UseDialogueVoices,
    IReadOnlyDictionary<string, string> SpeakerVoiceProfileCodes,
    string? ErrorMessage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    AudioFileDto? AudioFile);
