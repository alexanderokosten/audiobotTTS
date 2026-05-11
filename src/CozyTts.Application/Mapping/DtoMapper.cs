using CozyTts.Application.Common;
using CozyTts.Application.Contracts;
using CozyTts.Domain.Entities;
using System.Text.Json;

namespace CozyTts.Application.Mapping;

public static class DtoMapper
{
    public static ProjectSummaryDto ToSummaryDto(this VoiceProject project)
    {
        var latestJob = project.Jobs
            .OrderByDescending(job => job.CreatedAt)
            .FirstOrDefault();

        return new ProjectSummaryDto(
            project.Id,
            project.Title,
            project.CreatedAt,
            project.UpdatedAt,
            project.Jobs.Count,
            latestJob?.ToDto());
    }

    public static ProjectDetailsDto ToDetailsDto(this VoiceProject project)
    {
        return new ProjectDetailsDto(
            project.Id,
            project.Title,
            project.SourceText,
            project.CreatedAt,
            project.UpdatedAt,
            project.Jobs
                .OrderByDescending(job => job.CreatedAt)
                .Select(job => job.ToDto())
                .ToList());
    }

    public static GenerationJobDto ToDto(this VoiceGenerationJob job)
    {
        return new GenerationJobDto(
            job.Id,
            job.ProjectId,
            job.Status.ToApiString(),
            job.VoiceProfileId,
            job.VoiceProfile?.Code ?? string.Empty,
            job.VoiceProfile?.DisplayName ?? string.Empty,
            job.Speed.ToApiString(),
            job.OutputFormat.ToApiString(),
            job.Language,
            job.EmotionPrompt,
            job.UseDialogueVoices,
            ParseSpeakerVoiceProfileCodes(job.SpeakerVoiceProfileCodesJson),
            job.ErrorMessage,
            job.CreatedAt,
            job.StartedAt,
            job.CompletedAt,
            job.AudioFile?.ToDto());
    }

    public static AudioFileDto ToDto(this AudioFile audioFile)
    {
        return new AudioFileDto(
            audioFile.Id,
            audioFile.JobId,
            audioFile.FileName,
            audioFile.FilePath,
            audioFile.MimeType,
            audioFile.SizeBytes,
            audioFile.DurationSeconds,
            audioFile.CreatedAt);
    }

    public static VoiceProfileDto ToDto(this VoiceProfile voiceProfile)
    {
        return new VoiceProfileDto(
            voiceProfile.Id,
            voiceProfile.Code,
            voiceProfile.DisplayName,
            voiceProfile.Engine,
            voiceProfile.PiperModelPath,
            voiceProfile.PiperConfigPath,
            voiceProfile.QwenModel,
            voiceProfile.QwenMode,
            voiceProfile.QwenSpeaker,
            voiceProfile.QwenLanguage,
            voiceProfile.QwenInstruction,
            voiceProfile.Description);
    }

    private static IReadOnlyDictionary<string, string> ParseSpeakerVoiceProfileCodes(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(value)
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}
