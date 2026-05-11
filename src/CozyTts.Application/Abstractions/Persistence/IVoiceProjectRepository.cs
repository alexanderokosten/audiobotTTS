using CozyTts.Domain.Entities;

namespace CozyTts.Application.Abstractions.Persistence;

public interface IVoiceProjectRepository
{
    void Add(VoiceProject project);

    void Remove(VoiceProject project);

    Task<IReadOnlyList<VoiceProject>> ListAsync(CancellationToken cancellationToken);

    Task<VoiceProject?> GetByIdAsync(
        Guid id,
        bool includeJobs,
        bool track,
        CancellationToken cancellationToken);
}
