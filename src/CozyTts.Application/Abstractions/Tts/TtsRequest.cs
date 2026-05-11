using CozyTts.Domain.Entities;
using CozyTts.Domain.Enums;

namespace CozyTts.Application.Abstractions.Tts;

public sealed record TtsRequest(
    Guid JobId,
    string Text,
    VoiceProfile VoiceProfile,
    SpeechSpeed Speed,
    AudioOutputFormat OutputFormat,
    string? Language,
    string? EmotionPrompt,
    IReadOnlyList<TtsSegment>? Segments = null);
