using CozyTts.Domain.Entities;

namespace CozyTts.Application.Abstractions.Persistence;

public interface IVoiceProfileRepository
{
    Task<IReadOnlyList<VoiceProfile>> ListEnabledAsync(CancellationToken cancellationToken);

    Task<VoiceProfile?> GetByCodeAsync(string code, bool track, CancellationToken cancellationToken);

    Task<VoiceProfile?> GetByIdAsync(Guid id, bool track, CancellationToken cancellationToken);
}
