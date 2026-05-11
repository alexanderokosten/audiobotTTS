using CozyTts.Application.Contracts;

namespace CozyTts.Application.Services;

public interface IVoiceProfileService
{
    Task<IReadOnlyList<VoiceProfileDto>> ListAsync(CancellationToken cancellationToken);
}
