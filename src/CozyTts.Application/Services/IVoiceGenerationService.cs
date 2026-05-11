using CozyTts.Application.Contracts;

namespace CozyTts.Application.Services;

public interface IVoiceGenerationService
{
    Task<GenerationJobDto> CreateJobAsync(Guid projectId, GenerateVoiceRequest request, CancellationToken cancellationToken);

    Task<GenerationJobDto> GetJobAsync(Guid jobId, CancellationToken cancellationToken);

    Task<AudioFileDto> GetAudioFileAsync(Guid jobId, CancellationToken cancellationToken);

    Task<GenerationJobDto> RetryAsync(Guid jobId, CancellationToken cancellationToken);
}
