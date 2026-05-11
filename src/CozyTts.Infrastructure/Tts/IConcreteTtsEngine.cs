using CozyTts.Application.Abstractions.Tts;

namespace CozyTts.Infrastructure.Tts;

public interface IConcreteTtsEngine
{
    string EngineCode { get; }

    Task<TtsResult> GenerateAsync(TtsRequest request, CancellationToken cancellationToken);
}
