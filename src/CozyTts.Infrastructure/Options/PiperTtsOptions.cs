namespace CozyTts.Infrastructure.Options;

public sealed class PiperTtsOptions
{
    public string PiperBinPath { get; set; } = "piper";

    public string PiperModelsPath { get; set; } = "/models";

    public string FfmpegBinPath { get; set; } = "ffmpeg";

    public string TempPath { get; set; } = "/tmp/cozytts";

    public int MaxChunkCharacters { get; set; } = 1800;
}
