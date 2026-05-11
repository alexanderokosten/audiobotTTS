namespace CozyTts.Domain.Entities;

public sealed class VoiceProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Code { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Engine { get; set; } = "piper";

    public string PiperModelPath { get; set; } = string.Empty;

    public string? PiperConfigPath { get; set; }

    public string? QwenModel { get; set; }

    public string? QwenMode { get; set; }

    public string? QwenSpeaker { get; set; }

    public string? QwenLanguage { get; set; }

    public string? QwenInstruction { get; set; }

    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    public ICollection<VoiceGenerationJob> Jobs { get; set; } = new List<VoiceGenerationJob>();
}
