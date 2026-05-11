using CozyTts.Domain.Enums;

namespace CozyTts.Application.Common;

public static class OptionFormatter
{
    public static string ToApiString(this GenerationStatus status) => status switch
    {
        GenerationStatus.Pending => "Pending",
        GenerationStatus.Processing => "Processing",
        GenerationStatus.Completed => "Completed",
        GenerationStatus.Failed => "Failed",
        _ => status.ToString()
    };

    public static string ToApiString(this SpeechSpeed speed) => speed switch
    {
        SpeechSpeed.VerySlow => "very_slow",
        SpeechSpeed.Slow => "slow",
        SpeechSpeed.Normal => "normal",
        _ => speed.ToString()
    };

    public static string ToApiString(this AudioOutputFormat format) => format switch
    {
        AudioOutputFormat.Mp3 => "mp3",
        AudioOutputFormat.Wav => "wav",
        _ => format.ToString()
    };

    public static SpeechSpeed ParseSpeed(string? value)
    {
        return Normalize(value) switch
        {
            "very_slow" => SpeechSpeed.VerySlow,
            "slow" => SpeechSpeed.Slow,
            "normal" => SpeechSpeed.Normal,
            _ => throw new AppValidationException("Speed must be one of: very_slow, slow, normal.")
        };
    }

    public static AudioOutputFormat ParseFormat(string? value)
    {
        return Normalize(value) switch
        {
            "mp3" => AudioOutputFormat.Mp3,
            "wav" => AudioOutputFormat.Wav,
            _ => throw new AppValidationException("Output format must be one of: mp3, wav.")
        };
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant().Replace("-", "_");
    }
}
