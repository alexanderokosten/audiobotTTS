namespace CozyTts.Application.Contracts;

public sealed record GenerateVoiceRequest(
    string VoiceProfileCode,
    string Speed,
    string OutputFormat,
    string? Language = null,
    string? EmotionPrompt = null,
    bool UseDialogueVoices = false,
    Dictionary<string, string>? SpeakerVoiceProfileCodes = null);
