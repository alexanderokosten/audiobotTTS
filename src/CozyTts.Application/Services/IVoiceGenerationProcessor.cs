namespace CozyTts.Application.Services;

public interface IVoiceGenerationProcessor
{
    Task ProcessAsync(Guid jobId, CancellationToken cancellationToken);
}
