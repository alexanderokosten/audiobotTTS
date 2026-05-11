using CozyTts.Application.Abstractions.Tts;

namespace CozyTts.Infrastructure.Tts;

internal sealed class RoutedTtsEngine(IEnumerable<IConcreteTtsEngine> engines) : ITtsEngine
{
    private readonly IReadOnlyDictionary<string, IConcreteTtsEngine> _engines = engines
        .GroupBy(engine => engine.EngineCode, StringComparer.OrdinalIgnoreCase)
        .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

    public Task<TtsResult> GenerateAsync(TtsRequest request, CancellationToken cancellationToken)
    {
        var engineCode = string.IsNullOrWhiteSpace(request.VoiceProfile.Engine)
            ? "piper"
            : request.VoiceProfile.Engine.Trim();

        if (!_engines.TryGetValue(engineCode, out var engine))
        {
            throw new InvalidOperationException(
                $"TTS engine '{engineCode}' is not registered. Available engines: {string.Join(", ", _engines.Keys)}.");
        }

        return engine.GenerateAsync(request, cancellationToken);
    }
}
