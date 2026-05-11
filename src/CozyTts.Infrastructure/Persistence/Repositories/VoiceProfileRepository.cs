using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CozyTts.Infrastructure.Persistence.Repositories;

public sealed class VoiceProfileRepository(CozyTtsDbContext dbContext) : IVoiceProfileRepository
{
    public async Task<IReadOnlyList<VoiceProfile>> ListEnabledAsync(CancellationToken cancellationToken)
    {
        return await dbContext.VoiceProfiles
            .AsNoTracking()
            .Where(voice => voice.IsEnabled)
            .OrderBy(voice => voice.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<VoiceProfile?> GetByCodeAsync(string code, bool track, CancellationToken cancellationToken)
    {
        IQueryable<VoiceProfile> query = dbContext.VoiceProfiles;
        if (!track)
        {
            query = query.AsNoTracking();
        }

        var normalizedCode = code.Trim().ToLowerInvariant();
        return query.FirstOrDefaultAsync(
            voice => voice.IsEnabled && voice.Code.ToLower() == normalizedCode,
            cancellationToken);
    }

    public Task<VoiceProfile?> GetByIdAsync(Guid id, bool track, CancellationToken cancellationToken)
    {
        IQueryable<VoiceProfile> query = dbContext.VoiceProfiles;
        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(voice => voice.Id == id && voice.IsEnabled, cancellationToken);
    }
}
