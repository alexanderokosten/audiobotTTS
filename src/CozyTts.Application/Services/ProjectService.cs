using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Application.Abstractions.System;
using CozyTts.Application.Common;
using CozyTts.Application.Contracts;
using CozyTts.Application.Mapping;
using CozyTts.Domain.Entities;

namespace CozyTts.Application.Services;

public sealed class ProjectService(
    IVoiceProjectRepository projects,
    IUnitOfWork unitOfWork,
    IClock clock) : IProjectService
{
    private const int MaxTitleLength = 160;
    private const int MaxSourceTextLength = 120_000;

    public async Task<ProjectDetailsDto> CreateAsync(CreateProjectRequest request, CancellationToken cancellationToken)
    {
        var title = ValidateTitle(request.Title);
        var sourceText = ValidateSourceText(request.SourceText);
        var now = clock.UtcNow;

        var project = new VoiceProject
        {
            Id = Guid.NewGuid(),
            Title = title,
            SourceText = sourceText,
            CreatedAt = now,
            UpdatedAt = now
        };

        projects.Add(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return project.ToDetailsDto();
    }

    public async Task<IReadOnlyList<ProjectSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await projects.ListAsync(cancellationToken);
        return items.Select(project => project.ToSummaryDto()).ToList();
    }

    public async Task<ProjectDetailsDto> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, includeJobs: true, track: false, cancellationToken);
        return project?.ToDetailsDto() ?? throw new NotFoundException($"Project '{id}' was not found.");
    }

    public async Task<ProjectDetailsDto> UpdateAsync(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, includeJobs: true, track: true, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{id}' was not found.");
        }

        project.Title = ValidateTitle(request.Title);
        project.SourceText = ValidateSourceText(request.SourceText);
        project.UpdatedAt = clock.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return project.ToDetailsDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(id, includeJobs: false, track: true, cancellationToken);
        if (project is null)
        {
            throw new NotFoundException($"Project '{id}' was not found.");
        }

        projects.Remove(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string ValidateTitle(string? title)
    {
        title = (title ?? string.Empty).Trim();
        if (title.Length == 0)
        {
            throw new AppValidationException("Project title is required.");
        }

        if (title.Length > MaxTitleLength)
        {
            throw new AppValidationException($"Project title must be {MaxTitleLength} characters or less.");
        }

        return title;
    }

    private static string ValidateSourceText(string? sourceText)
    {
        sourceText = (sourceText ?? string.Empty).Trim();
        if (sourceText.Length == 0)
        {
            throw new AppValidationException("Source text is required.");
        }

        if (sourceText.Length > MaxSourceTextLength)
        {
            throw new AppValidationException($"Source text must be {MaxSourceTextLength} characters or less.");
        }

        return sourceText;
    }
}
