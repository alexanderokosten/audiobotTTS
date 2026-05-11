using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Application.Contracts;
using CozyTts.Application.Mapping;

namespace CozyTts.Application.Services;

public sealed class VoiceProfileService(IVoiceProfileRepository voices) : IVoiceProfileService
{
    public async Task<IReadOnlyList<VoiceProfileDto>> ListAsync(CancellationToken cancellationToken)
    {
        var items = await voices.ListEnabledAsync(cancellationToken);
        return items.Select(voice => voice.ToDto()).ToList();
    }
}
