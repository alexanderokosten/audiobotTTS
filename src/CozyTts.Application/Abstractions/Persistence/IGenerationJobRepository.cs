using CozyTts.Domain.Entities;

namespace CozyTts.Application.Abstractions.Persistence;

public interface IGenerationJobRepository
{
    void Add(VoiceGenerationJob job);

    void AddAudioFile(AudioFile audioFile);

    Task<VoiceGenerationJob?> GetByIdAsync(
        Guid id,
        bool includeDetails,
        bool track,
        CancellationToken cancellationToken);
}
