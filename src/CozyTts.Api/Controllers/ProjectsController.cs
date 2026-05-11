using CozyTts.Application.Contracts;
using CozyTts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CozyTts.Api.Controllers;

[ApiController]
[Route("api/projects")]
public sealed class ProjectsController(
    IProjectService projects,
    IVoiceGenerationService generations) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status201Created)]
    public async Task<ActionResult<ProjectDetailsDto>> Create(
        CreateProjectRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectSummaryDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await projects.ListAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDetailsDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await projects.GetAsync(id, cancellationToken));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProjectDetailsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProjectDetailsDto>> Update(
        Guid id,
        UpdateProjectRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await projects.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await projects.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{projectId:guid}/generate")]
    [ProducesResponseType(typeof(GenerationJobDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<GenerationJobDto>> Generate(
        Guid projectId,
        GenerateVoiceRequest request,
        CancellationToken cancellationToken)
    {
        var job = await generations.CreateJobAsync(projectId, request, cancellationToken);
        return AcceptedAtAction(nameof(JobsController.GetJob), "Jobs", new { jobId = job.Id }, job);
    }
}
