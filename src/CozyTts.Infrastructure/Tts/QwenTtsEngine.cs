using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CozyTts.Application.Abstractions.Tts;
using CozyTts.Domain.Entities;
using CozyTts.Domain.Enums;
using CozyTts.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CozyTts.Infrastructure.Tts;

public sealed partial class QwenTtsEngine(
    HttpClient httpClient,
    IOptions<QwenTtsOptions> options,
    ILogger<QwenTtsEngine> logger) : IConcreteTtsEngine
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private readonly QwenTtsOptions _options = options.Value;

    public string EngineCode => "qwen3";

    public async Task<TtsResult> GenerateAsync(TtsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new InvalidOperationException("TTS text is empty.");
        }

        ValidateVoiceProfile(request.VoiceProfile);

        var tempDirectory = Path.Combine(_options.TempPath, $"qwen-{request.JobId:N}");
        Directory.CreateDirectory(tempDirectory);

        var sourceTextPath = Path.Combine(tempDirectory, "source.txt");
        await File.WriteAllTextAsync(sourceTextPath, request.Text, Utf8NoBom, cancellationToken);

        var chunkWavs = await RenderWavChunksAsync(request, tempDirectory, cancellationToken);
        var mergedWavPath = chunkWavs.Count == 1
            ? chunkWavs[0]
            : Path.Combine(tempDirectory, "merged.wav");

        if (chunkWavs.Count > 1)
        {
            await MergeWavChunksAsync(chunkWavs, mergedWavPath, tempDirectory, cancellationToken);
        }

        var durationSeconds = TryReadWavDuration(mergedWavPath);
        var finalPath = mergedWavPath;
        var mimeType = "audio/wav";

        if (request.OutputFormat == AudioOutputFormat.Mp3)
        {
            finalPath = Path.Combine(tempDirectory, "output.mp3");
            await ConvertWavToMp3Async(mergedWavPath, finalPath, cancellationToken);
            mimeType = "audio/mpeg";
        }

        var sizeBytes = new FileInfo(finalPath).Length;
        logger.LogInformation("Generated {Format} Qwen3 audio for job {JobId}: {SizeBytes} bytes.",
            request.OutputFormat,
            request.JobId,
            sizeBytes);

        return new TtsResult(finalPath, mimeType, sizeBytes, durationSeconds, tempDirectory);
    }

    private async Task<List<string>> RenderWavChunksAsync(
        TtsRequest request,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        var chunkWavs = new List<string>();
        var wavIndex = 0;

        if (request.Segments is { Count: > 0 })
        {
            var segments = request.Segments
                .Where(segment => !string.IsNullOrWhiteSpace(segment.Text))
                .ToList();

            for (var segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
            {
                var segment = segments[segmentIndex];
                ValidateVoiceProfile(segment.VoiceProfile);

                await RenderTextChunksAsync(
                    segment.Text,
                    segment.VoiceProfile,
                    request,
                    tempDirectory,
                    $"segment-{segmentIndex:000}",
                    chunkWavs,
                    () => wavIndex++,
                    cancellationToken);

                if (segmentIndex < segments.Count - 1)
                {
                    var silencePath = Path.Combine(tempDirectory, $"silence-{wavIndex++:000}.wav");
                    await CreateSegmentPauseAsync(silencePath, cancellationToken);
                    chunkWavs.Add(silencePath);
                }
            }
        }
        else
        {
            await RenderTextChunksAsync(
                request.Text,
                request.VoiceProfile,
                request,
                tempDirectory,
                "chunk",
                chunkWavs,
                () => wavIndex++,
                cancellationToken);
        }

        if (chunkWavs.Count == 0)
        {
            throw new InvalidOperationException("Qwen TTS produced no audio chunks.");
        }

        return chunkWavs;
    }

    private async Task RenderTextChunksAsync(
        string text,
        VoiceProfile voiceProfile,
        TtsRequest request,
        string tempDirectory,
        string prefix,
        ICollection<string> chunkWavs,
        Func<int> nextIndex,
        CancellationToken cancellationToken)
    {
        var chunks = SplitText(text, _options.MaxChunkCharacters);
        for (var index = 0; index < chunks.Count; index++)
        {
            var wavIndex = nextIndex();
            var rawWavPath = Path.Combine(tempDirectory, $"{prefix}-{wavIndex:000}.raw.wav");
            var chunkWavPath = Path.Combine(tempDirectory, $"{prefix}-{wavIndex:000}.wav");
            await RenderQwenWavAsync(chunks[index], voiceProfile, request, rawWavPath, cancellationToken);
            await NormalizeWavAsync(rawWavPath, chunkWavPath, cancellationToken);
            chunkWavs.Add(chunkWavPath);
        }
    }

    private async Task RenderQwenWavAsync(
        string text,
        VoiceProfile voiceProfile,
        TtsRequest request,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var payload = BuildPayload(text, voiceProfile, request);
        try
        {
            using var response = await httpClient.PostAsJsonAsync(
                "synthesize",
                payload,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Qwen TTS endpoint returned {(int)response.StatusCode} {response.ReasonPhrase}. {Truncate(errorBody, 1000)}".Trim());
            }

            await using var file = File.Create(outputPath);
            await response.Content.CopyToAsync(file, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Qwen TTS endpoint '{httpClient.BaseAddress}' is not reachable. Start it with 'docker compose --profile qwen up -d qwen-tts api frontend'.",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"Qwen TTS endpoint '{httpClient.BaseAddress}' timed out after {_options.TimeoutMinutes} minutes.",
                ex);
        }
    }

    private QwenSynthesizeRequest BuildPayload(string text, VoiceProfile voiceProfile, TtsRequest request)
    {
        var model = RequireValue(voiceProfile.QwenModel, "Qwen model", voiceProfile.Code);
        var mode = string.IsNullOrWhiteSpace(voiceProfile.QwenMode)
            ? "custom_voice"
            : voiceProfile.QwenMode.Trim();
        var speaker = NormalizeOptional(voiceProfile.QwenSpeaker);

        if (mode.Equals("custom_voice", StringComparison.OrdinalIgnoreCase) && speaker is null)
        {
            throw new InvalidOperationException($"Qwen voice profile '{voiceProfile.Code}' requires QwenSpeaker.");
        }

        return new QwenSynthesizeRequest(
            text,
            model,
            mode,
            speaker,
            ResolveLanguage(request, voiceProfile),
            BuildInstruction(request, voiceProfile));
    }

    private static string BuildInstruction(TtsRequest request, VoiceProfile voiceProfile)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(voiceProfile.QwenInstruction))
        {
            parts.Add(voiceProfile.QwenInstruction.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.EmotionPrompt))
        {
            parts.Add(request.EmotionPrompt.Trim());
        }

        var speedInstruction = request.Speed switch
        {
            SpeechSpeed.Slow => "Speak slowly and clearly.",
            SpeechSpeed.VerySlow => "Speak very slowly, calmly, and clearly.",
            _ => null
        };

        if (speedInstruction is not null)
        {
            parts.Add(speedInstruction);
        }

        return string.Join(" ", parts);
    }

    private static string ResolveLanguage(TtsRequest request, VoiceProfile voiceProfile)
    {
        return NormalizeOptional(request.Language)
            ?? NormalizeOptional(voiceProfile.QwenLanguage)
            ?? "Auto";
    }

    private static void ValidateVoiceProfile(VoiceProfile voiceProfile)
    {
        if (!string.Equals(voiceProfile.Engine, "qwen3", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Voice profile '{voiceProfile.Code}' uses engine '{voiceProfile.Engine}', but QwenTtsEngine handles only 'qwen3'.");
        }

        _ = RequireValue(voiceProfile.QwenModel, "Qwen model", voiceProfile.Code);
    }

    private async Task NormalizeWavAsync(string inputPath, string outputPath, CancellationToken cancellationToken)
    {
        await RunProcessAsync(
            _options.FfmpegBinPath,
            new[]
            {
                "-y",
                "-i", inputPath,
                "-ar", _options.OutputSampleRate.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                "-ac", "1",
                "-acodec", "pcm_s16le",
                outputPath
            },
            standardInput: null,
            cancellationToken);
    }

    private async Task MergeWavChunksAsync(
        IReadOnlyList<string> chunkWavs,
        string outputPath,
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        var listPath = Path.Combine(tempDirectory, "concat.txt");
        var lines = chunkWavs.Select(path => $"file '{EscapeFfmpegConcatPath(path)}'");
        await File.WriteAllLinesAsync(listPath, lines, Utf8NoBom, cancellationToken);

        await RunProcessAsync(
            _options.FfmpegBinPath,
            new[]
            {
                "-y",
                "-f", "concat",
                "-safe", "0",
                "-i", listPath,
                "-acodec", "pcm_s16le",
                outputPath
            },
            standardInput: null,
            cancellationToken);
    }

    private async Task ConvertWavToMp3Async(string wavPath, string mp3Path, CancellationToken cancellationToken)
    {
        await RunProcessAsync(
            _options.FfmpegBinPath,
            new[]
            {
                "-y",
                "-i", wavPath,
                "-codec:a", "libmp3lame",
                "-b:a", "192k",
                mp3Path
            },
            standardInput: null,
            cancellationToken);
    }

    private async Task CreateSegmentPauseAsync(string outputPath, CancellationToken cancellationToken)
    {
        await RunProcessAsync(
            _options.FfmpegBinPath,
            new[]
            {
                "-y",
                "-f", "lavfi",
                "-i", $"anullsrc=r={_options.OutputSampleRate}:cl=mono",
                "-t", "0.28",
                "-acodec", "pcm_s16le",
                outputPath
            },
            standardInput: null,
            cancellationToken);
    }

    private static async Task RunProcessAsync(
        string fileName,
        IEnumerable<string> arguments,
        string? standardInput,
        CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardInput = standardInput is not null,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Failed to start process '{fileName}'.");
            }

            var stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

            if (standardInput is not null)
            {
                await process.StandardInput.WriteAsync(standardInput.AsMemory(), cancellationToken);
                process.StandardInput.Close();
            }

            await process.WaitForExitAsync(cancellationToken);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Process '{fileName}' exited with code {process.ExitCode}. {stderr} {stdout}".Trim());
            }
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException($"Failed to run process '{fileName}': {ex.Message}", ex);
        }
    }

    private static List<string> SplitText(string text, int maxChunkCharacters)
    {
        var normalized = text.Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        var sentences = SentenceSplitter().Split(normalized).Where(part => !string.IsNullOrWhiteSpace(part));
        var chunks = new List<string>();
        var current = new StringBuilder();

        foreach (var sentence in sentences)
        {
            var trimmed = sentence.Trim();
            if (trimmed.Length > maxChunkCharacters)
            {
                FlushCurrent();
                foreach (var hardChunk in HardSplit(trimmed, maxChunkCharacters))
                {
                    chunks.Add(hardChunk);
                }

                continue;
            }

            if (current.Length > 0 && current.Length + trimmed.Length + 1 > maxChunkCharacters)
            {
                FlushCurrent();
            }

            if (current.Length > 0)
            {
                current.Append(' ');
            }

            current.Append(trimmed);
        }

        FlushCurrent();
        return chunks.Count == 0 ? new List<string> { normalized } : chunks;

        void FlushCurrent()
        {
            if (current.Length == 0)
            {
                return;
            }

            chunks.Add(current.ToString());
            current.Clear();
        }
    }

    private static IEnumerable<string> HardSplit(string text, int maxChunkCharacters)
    {
        for (var offset = 0; offset < text.Length; offset += maxChunkCharacters)
        {
            var length = Math.Min(maxChunkCharacters, text.Length - offset);
            yield return text.Substring(offset, length).Trim();
        }
    }

    private static double? TryReadWavDuration(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);

            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "RIFF")
            {
                return null;
            }

            stream.Position += 4;
            if (Encoding.ASCII.GetString(reader.ReadBytes(4)) != "WAVE")
            {
                return null;
            }

            short channels = 0;
            int sampleRate = 0;
            short bitsPerSample = 0;
            int dataSize = 0;

            while (stream.Position < stream.Length - 8)
            {
                var chunkId = Encoding.ASCII.GetString(reader.ReadBytes(4));
                var chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    stream.Position += 2;
                    channels = reader.ReadInt16();
                    sampleRate = reader.ReadInt32();
                    stream.Position += 6;
                    bitsPerSample = reader.ReadInt16();
                    stream.Position += chunkSize - 16;
                }
                else if (chunkId == "data")
                {
                    dataSize = chunkSize;
                    break;
                }
                else
                {
                    stream.Position += chunkSize;
                }
            }

            var byteRate = sampleRate * channels * bitsPerSample / 8.0;
            return byteRate > 0 && dataSize > 0 ? dataSize / byteRate : null;
        }
        catch
        {
            return null;
        }
    }

    private static string EscapeFfmpegConcatPath(string path)
    {
        return path.Replace("\\", "/", StringComparison.Ordinal).Replace("'", "'\\''", StringComparison.Ordinal);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Cancellation cleanup is best-effort.
        }
    }

    private static string RequireValue(string? value, string label, string profileCode)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{label} is not configured for voice profile '{profileCode}'.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    [GeneratedRegex(@"(?<=[.!?。！？])\s+|\n{2,}", RegexOptions.Compiled)]
    private static partial Regex SentenceSplitter();

    private sealed record QwenSynthesizeRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("mode")] string Mode,
        [property: JsonPropertyName("speaker")] string? Speaker,
        [property: JsonPropertyName("language")] string Language,
        [property: JsonPropertyName("instruction")] string Instruction);
}
