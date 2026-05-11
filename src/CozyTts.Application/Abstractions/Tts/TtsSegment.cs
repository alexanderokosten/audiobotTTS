using CozyTts.Domain.Entities;

namespace CozyTts.Application.Abstractions.Tts;

public sealed record TtsSegment(string? Speaker, string Text, VoiceProfile VoiceProfile);
