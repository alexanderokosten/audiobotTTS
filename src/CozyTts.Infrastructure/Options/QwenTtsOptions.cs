namespace CozyTts.Infrastructure.Options;

public sealed class QwenTtsOptions
{
    public string Endpoint { get; set; } = "http://qwen-tts:8090";

    public int TimeoutMinutes { get; set; } = 30;

    public string FfmpegBinPath { get; set; } = "/usr/bin/ffmpeg";

    public string TempPath { get; set; } = "/tmp/cozytts";

    public int MaxChunkCharacters { get; set; } = 1200;

    public int OutputSampleRate { get; set; } = 24000;
}
