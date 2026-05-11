namespace CozyTts.Application.Contracts;

public sealed record ProjectSummaryDto(
    Guid Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int JobsCount,
    GenerationJobDto? LatestJob);
