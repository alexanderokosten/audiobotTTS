using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CozyTts.Infrastructure.Persistence.Repositories;

public sealed class VoiceProjectRepository(CozyTtsDbContext dbContext) : IVoiceProjectRepository
{
    public void Add(VoiceProject project)
    {
        dbContext.VoiceProjects.Add(project);
    }

    public void Remove(VoiceProject project)
    {
        dbContext.VoiceProjects.Remove(project);
    }

    public async Task<IReadOnlyList<VoiceProject>> ListAsync(CancellationToken cancellationToken)
    {
        return await dbContext.VoiceProjects
            .AsNoTracking()
            .Include(project => project.Jobs)
            .ThenInclude(job => job.VoiceProfile)
            .Include(project => project.Jobs)
            .ThenInclude(job => job.AudioFile)
            .OrderByDescending(project => project.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<VoiceProject?> GetByIdAsync(
        Guid id,
        bool includeJobs,
        bool track,
        CancellationToken cancellationToken)
    {
        IQueryable<VoiceProject> query = dbContext.VoiceProjects;

        if (includeJobs)
        {
            query = query
                .Include(project => project.Jobs)
                .ThenInclude(job => job.VoiceProfile)
                .Include(project => project.Jobs)
                .ThenInclude(job => job.AudioFile);
        }

        if (!track)
        {
            query = query.AsNoTracking();
        }

        return query.FirstOrDefaultAsync(project => project.Id == id, cancellationToken);
    }
}
