using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CozyTts.Infrastructure.Persistence.Repositories;

public sealed class GenerationJobRepository(CozyTtsDbContext dbContext) : IGenerationJobRepository
{
    public void Add(VoiceGenerationJob job)
    {
        dbContext.VoiceGenerationJobs.Add(job);
    }

    public void AddAudioFile(AudioFile audioFile)
    {
        dbContext.AudioFiles.Add(audioFile);
    }

    public Task<VoiceGenerationJob?> GetByIdAsync(
        Guid id,
        bool includeDetails,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<VoiceGenerationJob> query = dbContext.VoiceGenerationJobs;

        if (includeDetails)
        {
            query = query
                .Include(job => job.Project)
                .Include(job => job.VoiceProfile)
                .Include(job => job.AudioFile);
        }

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(job => job.Id == id, cancellationToken);
    }
}
