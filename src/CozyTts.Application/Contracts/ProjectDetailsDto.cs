namespace CozyTts.Application.Contracts;

public sealed record ProjectDetailsDto(
    Guid Id,
    string Title,
    string SourceText,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<GenerationJobDto> Jobs);
