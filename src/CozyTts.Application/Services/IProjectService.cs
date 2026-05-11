using CozyTts.Application.Contracts;

namespace CozyTts.Application.Services;

public interface IProjectService
{
    Task<ProjectDetailsDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(CancellationToken cancellationToken);

    Task<ProjectDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<ProjectDetailsDto> UpdateAsync(Guid id, UpdateProjectRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
