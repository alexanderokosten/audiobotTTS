using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using CozyTts.Application.Abstractions.Tts;
using CozyTts.Domain.Enums;
using CozyTts.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CozyTts.Infrastructure.Tts;

public sealed partial class PiperTtsEngine(
    IOptions<PiperTtsOptions> options,
    ILogger<PiperTtsEngine> logger) : IConcreteTtsEngine
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly PiperTtsOptions _options = options.Value;

    public string EngineCode => "piper";

    public async Task<TtsResult> GenerateAsync(TtsRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            throw new InvalidOperationException("TTS text is empty.");
        }

        var modelPath = ResolveModelPath(request.VoiceProfile.PiperModelPath);
        var configPath = ResolveOptionalModelPath(request.VoiceProfile.PiperConfigPath);

        var tempDirectory = Path.Combine(_options.TempPath, request.JobId.ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var sourceTextPath = Path.Combine(tempDirectory, "source.txt");
        await File.WriteAllTextAsync(sourceTextPath, request.Text, Utf8NoBom, cancellationToken);

        var chunkWavs = await RenderWavChunksAsync(request, modelPath, configPath, tempDirectory, cancellationToken);

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
        logger.LogInformation("Generated {Format} audio for job {JobId}: {SizeBytes} bytes.",
            request.OutputFormat,
            request.JobId,
            sizeBytes);

        return new TtsResult(finalPath, mimeType, sizeBytes, durationSeconds, tempDirectory);
    }

    private async Task<List<string>> RenderWavChunksAsync(
        TtsRequest request,
        string defaultModelPath,
        string? defaultConfigPath,
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
                var modelPath = ResolveModelPath(segment.VoiceProfile.PiperModelPath);
                var configPath = ResolveOptionalModelPath(segment.VoiceProfile.PiperConfigPath);

                await RenderTextChunksAsync(
                    segment.Text,
                    modelPath,
                    configPath,
                    request.Speed,
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
                defaultModelPath,
                defaultConfigPath,
                request.Speed,
                tempDirectory,
                "chunk",
                chunkWavs,
                () => wavIndex++,
                cancellationToken);
        }

        if (chunkWavs.Count == 0)
        {
            throw new InvalidOperationException("TTS produced no audio chunks.");
        }

        return chunkWavs;
    }

    private async Task RenderTextChunksAsync(
        string text,
        string modelPath,
        string? configPath,
        SpeechSpeed speed,
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
            var chunkTextPath = Path.Combine(tempDirectory, $"{prefix}-{wavIndex:000}.txt");
            var chunkWavPath = Path.Combine(tempDirectory, $"{prefix}-{wavIndex:000}.wav");
            await File.WriteAllTextAsync(chunkTextPath, chunks[index], Utf8NoBom, cancellationToken);
            await RunPiperAsync(chunks[index], modelPath, configPath, speed, chunkWavPath, cancellationToken);
            chunkWavs.Add(chunkWavPath);
        }
    }

    private string ResolveModelPath(string path)
    {
        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(_options.PiperModelsPath, path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Piper model '{resolved}' was not found. Mount models to PIPER_MODELS_PATH or update the voice profile.",
                resolved);
        }

        return resolved;
    }

    private string? ResolveOptionalModelPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var resolved = Path.IsPathRooted(path) ? path : Path.Combine(_options.PiperModelsPath, path);
        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Piper config '{resolved}' was not found. Download the matching .onnx.json file or update the voice profile.",
                resolved);
        }

        return resolved;
    }

    private async Task RunPiperAsync(
        string text,
        string modelPath,
        string? configPath,
        SpeechSpeed speed,
        string outputPath,
        CancellationToken cancellationToken)
    {
        var arguments = new List<string>
        {
            "--model", modelPath,
            "--output_file", outputPath,
            "--length_scale", GetLengthScale(speed).ToString("0.00", global::System.Globalization.CultureInfo.InvariantCulture)
        };

        if (!string.IsNullOrWhiteSpace(configPath))
        {
            arguments.Add("--config");
            arguments.Add(configPath);
        }

        await RunProcessAsync(_options.PiperBinPath, arguments, text, cancellationToken);
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
                "-i", "anullsrc=r=22050:cl=mono",
                "-t", "0.28",
                "-acodec", "pcm_s16le",
                outputPath
            },
            standardInput: null,
            cancellationToken);
    }

    private async Task RunProcessAsync(
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

    private static double GetLengthScale(SpeechSpeed speed) => speed switch
    {
        SpeechSpeed.VerySlow => 1.35,
        SpeechSpeed.Slow => 1.15,
        SpeechSpeed.Normal => 1.0,
        _ => 1.0
    };

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

    [GeneratedRegex(@"(?<=[.!?])\s+|\n{2,}", RegexOptions.Compiled)]
    private static partial Regex SentenceSplitter();
}
