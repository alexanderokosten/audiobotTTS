using CozyTts.Application.Abstractions.Audio;
using CozyTts.Application.Common;
using CozyTts.Application.Contracts;
using CozyTts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CozyTts.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public sealed class JobsController(
    IVoiceGenerationService generations,
    IAudioStorage audioStorage) : ControllerBase
{
    [HttpGet("{jobId:guid}")]
    [ProducesResponseType(typeof(GenerationJobDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<GenerationJobDto>> GetJob(Guid jobId, CancellationToken cancellationToken)
    {
        return Ok(await generations.GetJobAsync(jobId, cancellationToken));
    }

    [HttpGet("{jobId:guid}/audio")]
    public async Task<IActionResult> GetAudio(Guid jobId, CancellationToken cancellationToken)
    {
        var audio = await generations.GetAudioFileAsync(jobId, cancellationToken);
        var path = audioStorage.ResolveAbsolutePath(audio.FilePath);
        EnsureFileExists(path, jobId);

        return PhysicalFile(path, audio.MimeType, enableRangeProcessing: true);
    }

    [HttpGet("{jobId:guid}/download")]
    public async Task<IActionResult> Download(Guid jobId, CancellationToken cancellationToken)
    {
        var audio = await generations.GetAudioFileAsync(jobId, cancellationToken);
        var path = audioStorage.ResolveAbsolutePath(audio.FilePath);
        EnsureFileExists(path, jobId);

        return PhysicalFile(path, audio.MimeType, audio.FileName, enableRangeProcessing: true);
    }

    [HttpPost("{jobId:guid}/retry")]
    [ProducesResponseType(typeof(GenerationJobDto), StatusCodes.Status202Accepted)]
    public async Task<ActionResult<GenerationJobDto>> Retry(Guid jobId, CancellationToken cancellationToken)
    {
        var retry = await generations.RetryAsync(jobId, cancellationToken);
        return AcceptedAtAction(nameof(GetJob), new { jobId = retry.Id }, retry);
    }

    private static void EnsureFileExists(string path, Guid jobId)
    {
        if (!System.IO.File.Exists(path))
        {
            throw new NotFoundException($"Audio file for job '{jobId}' was not found on disk.");
        }
    }
}
