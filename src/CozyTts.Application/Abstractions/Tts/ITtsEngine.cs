namespace CozyTts.Application.Abstractions.Tts;

public interface ITtsEngine
{
    Task<TtsResult> GenerateAsync(TtsRequest request, CancellationToken cancellationToken);
}
