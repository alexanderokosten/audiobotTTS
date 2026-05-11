using CozyTts.Application.Abstractions.Jobs;
using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Application.Abstractions.System;
using CozyTts.Application.Common;
using CozyTts.Application.Contracts;
using CozyTts.Application.Mapping;
using CozyTts.Domain.Entities;
using CozyTts.Domain.Enums;
using System.Text.Json;

namespace CozyTts.Application.Services;

public sealed class VoiceGenerationService(
    IVoiceProjectRepository projects,
    IVoiceProfileRepository voices,
    IGenerationJobRepository jobs,
    IUnitOfWork unitOfWork,
    IVoiceJobQueue queue,
    IClock clock) : IVoiceGenerationService
{
    public async Task<GenerationJobDto> CreateJobAsync(
        Guid projectId,
        GenerateVoiceRequest request,
        CancellationToken cancellationToken)
    {
        var project = await projects.GetByIdAsync(projectId, includeJobs: false, track: true, cancellationToken)
            ?? throw new NotFoundException($"Project '{projectId}' was not found.");

        var voice = await voices.GetByCodeAsync(request.VoiceProfileCode, track: true, cancellationToken)
            ?? throw new AppValidationException($"Voice profile '{request.VoiceProfileCode}' was not found.");

        ValidateLanguageAndEmotion(request);

        var speakerVoiceProfileCodes = await ValidateSpeakerVoiceProfilesAsync(request, voice, cancellationToken);
        var job = CreateJob(project.Id, voice.Id, request);
        job.SpeakerVoiceProfileCodesJson = speakerVoiceProfileCodes.Count > 0
            ? JsonSerializer.Serialize(speakerVoiceProfileCodes)
            : null;
        job.Project = project;
        job.VoiceProfile = voice;
        project.UpdatedAt = clock.UtcNow;

        jobs.Add(job);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        job.QueueJobId = queue.Enqueue(job.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return job.ToDto();
    }

    public async Task<GenerationJobDto> GetJobAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(jobId, includeDetails: true, track: false, cancellationToken);
        return job?.ToDto() ?? throw new NotFoundException($"Job '{jobId}' was not found.");
    }

    public async Task<AudioFileDto> GetAudioFileAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var job = await jobs.GetByIdAsync(jobId, includeDetails: true, track: false, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        if (job.Status != GenerationStatus.Completed || job.AudioFile is null)
        {
            throw new AppConflictException("Audio is not ready yet.");
        }

        return job.AudioFile.ToDto();
    }

    public async Task<GenerationJobDto> RetryAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var original = await jobs.GetByIdAsync(jobId, includeDetails: true, track: false, cancellationToken)
            ?? throw new NotFoundException($"Job '{jobId}' was not found.");

        if (original.Status is GenerationStatus.Pending or GenerationStatus.Processing)
        {
            throw new AppConflictException("Only completed or failed jobs can be retried.");
        }

        var project = await projects.GetByIdAsync(original.ProjectId, includeJobs: false, track: true, cancellationToken)
            ?? throw new NotFoundException($"Project '{original.ProjectId}' was not found.");
        var voice = await voices.GetByIdAsync(original.VoiceProfileId, track: true, cancellationToken)
            ?? throw new NotFoundException($"Voice profile '{original.VoiceProfileId}' was not found.");

        var retry = new VoiceGenerationJob
        {
            Id = Guid.NewGuid(),
            ProjectId = original.ProjectId,
            VoiceProfileId = original.VoiceProfileId,
            Speed = original.Speed,
            OutputFormat = original.OutputFormat,
            Language = original.Language,
            EmotionPrompt = original.EmotionPrompt,
            UseDialogueVoices = original.UseDialogueVoices,
            SpeakerVoiceProfileCodesJson = original.SpeakerVoiceProfileCodesJson,
            Status = GenerationStatus.Pending,
            CreatedAt = clock.UtcNow,
            Project = project,
            VoiceProfile = voice
        };

        project.UpdatedAt = clock.UtcNow;
        jobs.Add(retry);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        retry.QueueJobId = queue.Enqueue(retry.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return retry.ToDto();
    }

    private VoiceGenerationJob CreateJob(Guid projectId, Guid voiceProfileId, GenerateVoiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.VoiceProfileCode))
        {
            throw new AppValidationException("Voice profile code is required.");
        }

        return new VoiceGenerationJob
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            VoiceProfileId = voiceProfileId,
            Status = GenerationStatus.Pending,
            Speed = OptionFormatter.ParseSpeed(request.Speed),
            OutputFormat = OptionFormatter.ParseFormat(request.OutputFormat),
            Language = NormalizeOptional(request.Language),
            EmotionPrompt = NormalizeOptional(request.EmotionPrompt),
            UseDialogueVoices = request.UseDialogueVoices,
            CreatedAt = clock.UtcNow
        };
    }

    private async Task<IReadOnlyDictionary<string, string>> ValidateSpeakerVoiceProfilesAsync(
        GenerateVoiceRequest request,
        VoiceProfile defaultVoice,
        CancellationToken cancellationToken)
    {
        if (!request.UseDialogueVoices)
        {
            return new Dictionary<string, string>();
        }

        var rawMap = request.SpeakerVoiceProfileCodes ?? new Dictionary<string, string>();
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (speaker, voiceCode) in rawMap)
        {
            var normalizedSpeaker = DialogueScriptParser.NormalizeSpeaker(speaker);
            var normalizedVoiceCode = (voiceCode ?? string.Empty).Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(normalizedSpeaker) || string.IsNullOrWhiteSpace(normalizedVoiceCode))
            {
                continue;
            }

            if (normalizedSpeaker.Length > 64)
            {
                throw new AppValidationException("Speaker name must be 64 characters or less.");
            }

            var voice = await voices.GetByCodeAsync(normalizedVoiceCode, track: false, cancellationToken);
            if (voice is null)
            {
                throw new AppValidationException(
                    $"Voice profile '{normalizedVoiceCode}' for speaker '{normalizedSpeaker}' was not found.");
            }

            if (!string.Equals(voice.Engine, defaultVoice.Engine, StringComparison.OrdinalIgnoreCase))
            {
                throw new AppValidationException(
                    $"Speaker '{normalizedSpeaker}' uses engine '{voice.Engine}', but the default voice uses '{defaultVoice.Engine}'. Dialogue generation must use one TTS engine per job.");
            }

            normalized[normalizedSpeaker] = normalizedVoiceCode;
        }

        if (normalized.Count > 24)
        {
            throw new AppValidationException("Dialogue generation supports up to 24 speaker voice mappings.");
        }

        return normalized;
    }

    private static void ValidateLanguageAndEmotion(GenerateVoiceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Language) && request.Language.Trim().Length > 64)
        {
            throw new AppValidationException("Language must be 64 characters or less.");
        }

        if (!string.IsNullOrWhiteSpace(request.EmotionPrompt) && request.EmotionPrompt.Trim().Length > 1000)
        {
            throw new AppValidationException("Emotion prompt must be 1000 characters or less.");
        }
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }
}
