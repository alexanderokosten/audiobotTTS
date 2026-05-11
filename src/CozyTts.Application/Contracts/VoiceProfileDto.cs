namespace CozyTts.Application.Contracts;

public sealed record VoiceProfileDto(
    Guid Id,
    string Code,
    string DisplayName,
    string Engine,
    string PiperModelPath,
    string? PiperConfigPath,
    string? QwenModel,
    string? QwenMode,
    string? QwenSpeaker,
    string? QwenLanguage,
    string? QwenInstruction,
    string? Description);
