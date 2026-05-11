using CozyTts.Application.Contracts;
using CozyTts.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CozyTts.Api.Controllers;

[ApiController]
[Route("api/voices")]
public sealed class VoicesController(IVoiceProfileService voices) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<VoiceProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<VoiceProfileDto>>> List(CancellationToken cancellationToken)
    {
        return Ok(await voices.ListAsync(cancellationToken));
    }
}
