using CozyTts.Application.Abstractions.Audio;
using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Application.Abstractions.System;
using CozyTts.Application.Abstractions.Tts;
using CozyTts.Application.Common;
using CozyTts.Domain.Entities;
using CozyTts.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace CozyTts.Application.Services;

public sealed class VoiceGenerationProcessor(
    IGenerationJobRepository jobs,
    IVoiceProfileRepository voices,
    IAudioStorage audioStorage,
    ITtsEngine ttsEngine,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<VoiceGenerationProcessor> logger) : IVoiceGenerationProcessor
{
    public async Task ProcessAsync(Guid jobId, CancellationToken cancellationToken)
    {
        TtsResult? ttsResult = null;

        try
        {
            var job = await jobs.GetByIdAsync(jobId, includeDetails: true, track: true, cancellationToken);
            if (job is null)
            {
                logger.LogWarning("Voice generation job {JobId} was not found.", jobId);
                return;
            }

            if (job.Status is GenerationStatus.Completed or GenerationStatus.Processing)
            {
                logger.LogInformation("Voice generation job {JobId} is already {Status}.", jobId, job.Status);
                return;
            }

            if (job.Project is null || job.VoiceProfile is null)
            {
                throw new InvalidOperationException("Job details were not loaded.");
            }

            job.Status = GenerationStatus.Processing;
            job.ErrorMessage = null;
            job.StartedAt = clock.UtcNow;
            job.CompletedAt = null;
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var segments = await BuildTtsSegmentsAsync(job, cancellationToken);
            ttsResult = await ttsEngine.GenerateAsync(
                new TtsRequest(
                    job.Id,
                    job.Project.SourceText,
                    job.VoiceProfile,
                    job.Speed,
                    job.OutputFormat,
                    job.Language,
                    job.EmotionPrompt,
                    segments),
                cancellationToken);

            var stored = await audioStorage.SaveAsync(
                ttsResult.FilePath,
                BuildFileName(job),
                ttsResult.MimeType,
                ttsResult.DurationSeconds,
                cancellationToken);

            var audioFile = new AudioFile
            {
                Id = Guid.NewGuid(),
                JobId = job.Id,
                FileName = stored.FileName,
                FilePath = stored.FilePath,
                MimeType = stored.MimeType,
                SizeBytes = stored.SizeBytes,
                DurationSeconds = stored.DurationSeconds,
                CreatedAt = clock.UtcNow
            };

            jobs.AddAudioFile(audioFile);
            job.AudioFile = audioFile;
            job.Status = GenerationStatus.Completed;
            job.CompletedAt = clock.UtcNow;
            job.ErrorMessage = null;

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await MarkFailedAsync(jobId, ex, cancellationToken);
        }
        finally
        {
            TryDeleteTempDirectory(ttsResult?.TempDirectory);
        }
    }

    private async Task MarkFailedAsync(Guid jobId, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Voice generation job {JobId} failed.", jobId);

        var job = await jobs.GetByIdAsync(jobId, includeDetails: false, track: true, cancellationToken);
        if (job is null)
        {
            return;
        }

        job.Status = GenerationStatus.Failed;
        job.CompletedAt = clock.UtcNow;
        job.ErrorMessage = TruncateError(exception.Message);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<TtsSegment>?> BuildTtsSegmentsAsync(
        VoiceGenerationJob job,
        CancellationToken cancellationToken)
    {
        if (!job.UseDialogueVoices || job.Project is null || job.VoiceProfile is null)
        {
            return null;
        }

        var lines = DialogueScriptParser.Parse(job.Project.SourceText);
        if (lines.Count == 0)
        {
            return null;
        }

        var speakerVoiceCodes = ParseSpeakerVoiceCodes(job.SpeakerVoiceProfileCodesJson);
        var voiceCache = new Dictionary<string, VoiceProfile>(StringComparer.OrdinalIgnoreCase)
        {
            [job.VoiceProfile.Code] = job.VoiceProfile
        };

        var segments = new List<TtsSegment>(lines.Count);
        foreach (var line in lines)
        {
            var voice = job.VoiceProfile;
            if (line.Speaker is not null &&
                speakerVoiceCodes.TryGetValue(line.Speaker, out var voiceCode) &&
                !string.IsNullOrWhiteSpace(voiceCode))
            {
                if (!voiceCache.TryGetValue(voiceCode, out voice!))
                {
                    voice = await voices.GetByCodeAsync(voiceCode, track: false, cancellationToken)
                        ?? job.VoiceProfile;
                    voiceCache[voiceCode] = voice;
                }
            }

            segments.Add(new TtsSegment(line.Speaker, line.Text, voice));
        }

        return segments;
    }

    private static IReadOnlyDictionary<string, string> ParseSpeakerVoiceCodes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        var parsed = JsonSerializer.Deserialize<Dictionary<string, string>>(value)
            ?? new Dictionary<string, string>();
        return new Dictionary<string, string>(parsed, StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildFileName(VoiceGenerationJob job)
    {
        var extension = job.OutputFormat is AudioOutputFormat.Mp3 ? "mp3" : "wav";
        var title = job.Project?.Title ?? "cozy-tts";
        var safeTitle = new string(title
                .ToLowerInvariant()
                .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                .ToArray())
            .Replace("--", "-", StringComparison.Ordinal)
            .Trim('-');

        if (string.IsNullOrWhiteSpace(safeTitle))
        {
            safeTitle = "cozy-tts";
        }

        return $"{safeTitle}-{job.Id:N}.{extension}";
    }

    private static string TruncateError(string message)
    {
        return message.Length <= 4000 ? message : message[..4000];
    }

    private static void TryDeleteTempDirectory(string? tempDirectory)
    {
        if (string.IsNullOrWhiteSpace(tempDirectory) || !Directory.Exists(tempDirectory))
        {
            return;
        }

        try
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
        catch
        {
            // Temp cleanup must not hide the actual generation result.
        }
    }
}
