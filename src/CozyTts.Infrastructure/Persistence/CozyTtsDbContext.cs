using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Domain.Entities;
using CozyTts.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CozyTts.Infrastructure.Persistence;

public sealed class CozyTtsDbContext(DbContextOptions<CozyTtsDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<VoiceProject> VoiceProjects => Set<VoiceProject>();

    public DbSet<VoiceGenerationJob> VoiceGenerationJobs => Set<VoiceGenerationJob>();

    public DbSet<VoiceProfile> VoiceProfiles => Set<VoiceProfile>();

    public DbSet<AudioFile> AudioFiles => Set<AudioFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<VoiceProject>(entity =>
        {
            entity.ToTable("voice_projects");
            entity.HasKey(project => project.Id);
            entity.Property(project => project.Title).HasMaxLength(160).IsRequired();
            entity.Property(project => project.SourceText).IsRequired();
            entity.Property(project => project.CreatedAt).IsRequired();
            entity.Property(project => project.UpdatedAt).IsRequired();

            entity.HasMany(project => project.Jobs)
                .WithOne(job => job.Project)
                .HasForeignKey(job => job.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceGenerationJob>(entity =>
        {
            entity.ToTable("voice_generation_jobs");
            entity.HasKey(job => job.Id);
            entity.Property(job => job.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(job => job.Speed).HasConversion<string>().HasMaxLength(32).IsRequired();
            entity.Property(job => job.OutputFormat).HasConversion<string>().HasMaxLength(16).IsRequired();
            entity.Property(job => job.Language).HasMaxLength(64);
            entity.Property(job => job.EmotionPrompt).HasMaxLength(1000);
            entity.Property(job => job.UseDialogueVoices).IsRequired();
            entity.Property(job => job.SpeakerVoiceProfileCodesJson).HasColumnType("jsonb");
            entity.Property(job => job.QueueJobId).HasMaxLength(128);
            entity.Property(job => job.ErrorMessage).HasMaxLength(4000);
            entity.Property(job => job.CreatedAt).IsRequired();

            entity.HasIndex(job => job.ProjectId);
            entity.HasIndex(job => job.Status);
            entity.HasIndex(job => job.CreatedAt);

            entity.HasOne(job => job.VoiceProfile)
                .WithMany(voice => voice.Jobs)
                .HasForeignKey(job => job.VoiceProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(job => job.AudioFile)
                .WithOne(audio => audio.Job)
                .HasForeignKey<AudioFile>(audio => audio.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VoiceProfile>(entity =>
        {
            entity.ToTable("voice_profiles");
            entity.HasKey(voice => voice.Id);
            entity.Property(voice => voice.Code).HasMaxLength(64).IsRequired();
            entity.Property(voice => voice.DisplayName).HasMaxLength(160).IsRequired();
            entity.Property(voice => voice.Engine).HasMaxLength(32).IsRequired();
            entity.Property(voice => voice.PiperModelPath).HasMaxLength(512).IsRequired();
            entity.Property(voice => voice.PiperConfigPath).HasMaxLength(512);
            entity.Property(voice => voice.QwenModel).HasMaxLength(256);
            entity.Property(voice => voice.QwenMode).HasMaxLength(64);
            entity.Property(voice => voice.QwenSpeaker).HasMaxLength(128);
            entity.Property(voice => voice.QwenLanguage).HasMaxLength(64);
            entity.Property(voice => voice.QwenInstruction).HasMaxLength(1000);
            entity.Property(voice => voice.Description).HasMaxLength(1000);
            entity.Property(voice => voice.IsEnabled).IsRequired();
            entity.HasIndex(voice => voice.Code).IsUnique();

            entity.HasData(
                new VoiceProfile
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Code = "cozy_female",
                    DisplayName = "Cozy Female",
                    Engine = "piper",
                    PiperModelPath = "en_US-lessac-medium.onnx",
                    PiperConfigPath = "en_US-lessac-medium.onnx.json",
                    Description = "Warm American English voice for soft listening practice.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Code = "calm_male",
                    DisplayName = "Calm Male",
                    Engine = "piper",
                    PiperModelPath = "en_US-ryan-medium.onnx",
                    PiperConfigPath = "en_US-ryan-medium.onnx.json",
                    Description = "Calm American English male voice for dialogue narration.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Code = "british_soft",
                    DisplayName = "British Soft",
                    Engine = "piper",
                    PiperModelPath = "en_GB-alba-medium.onnx",
                    PiperConfigPath = "en_GB-alba-medium.onnx.json",
                    Description = "Soft British English profile. Replace model file if you prefer another Piper voice.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Code = "narrator",
                    DisplayName = "Narrator",
                    Engine = "piper",
                    PiperModelPath = "en_US-amy-medium.onnx",
                    PiperConfigPath = "en_US-amy-medium.onnx.json",
                    Description = "Neutral narrator voice for monologues and long-form listening.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Code = "qwen_ryan_en",
                    DisplayName = "Qwen Ryan EN",
                    Engine = "qwen3",
                    PiperModelPath = string.Empty,
                    QwenModel = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice",
                    QwenMode = "custom_voice",
                    QwenSpeaker = "Ryan",
                    QwenLanguage = "English",
                    QwenInstruction = "Warm, natural podcast voice with clear articulation.",
                    Description = "Qwen3-TTS English male profile with instruction and emotion control.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
                    Code = "qwen_aiden_en",
                    DisplayName = "Qwen Aiden EN",
                    Engine = "qwen3",
                    PiperModelPath = string.Empty,
                    QwenModel = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice",
                    QwenMode = "custom_voice",
                    QwenSpeaker = "Aiden",
                    QwenLanguage = "English",
                    QwenInstruction = "Friendly, relaxed American podcast voice.",
                    Description = "Qwen3-TTS English male profile for natural dialogues.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
                    Code = "qwen_ru_soft_female",
                    DisplayName = "Qwen RU Soft Female",
                    Engine = "qwen3",
                    PiperModelPath = string.Empty,
                    QwenModel = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice",
                    QwenMode = "custom_voice",
                    QwenSpeaker = "Serena",
                    QwenLanguage = "Russian",
                    QwenInstruction = "Говори мягко, тепло и спокойно, как ведущая уютного подкаста.",
                    Description = "Qwen3-TTS Russian-capable soft female profile.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
                    Code = "qwen_ru_calm_male",
                    DisplayName = "Qwen RU Calm Male",
                    Engine = "qwen3",
                    PiperModelPath = string.Empty,
                    QwenModel = "Qwen/Qwen3-TTS-12Hz-0.6B-CustomVoice",
                    QwenMode = "custom_voice",
                    QwenSpeaker = "Ryan",
                    QwenLanguage = "Russian",
                    QwenInstruction = "Говори спокойно, дружелюбно и естественно, как ведущий подкаста.",
                    Description = "Qwen3-TTS Russian-capable calm male profile.",
                    IsEnabled = true
                },
                new VoiceProfile
                {
                    Id = Guid.Parse("99999999-9999-9999-9999-999999999999"),
                    Code = "qwen_voice_design",
                    DisplayName = "Qwen Voice Design",
                    Engine = "qwen3",
                    PiperModelPath = string.Empty,
                    QwenModel = "Qwen/Qwen3-TTS-12Hz-1.7B-VoiceDesign",
                    QwenMode = "voice_design",
                    QwenLanguage = "Russian",
                    QwenInstruction = "Создай мягкий, эмоциональный голос для уютного подкастового диалога.",
                    Description = "Qwen3-TTS voice design profile. Heavier model, best with GPU.",
                    IsEnabled = true
                });
        });

        modelBuilder.Entity<AudioFile>(entity =>
        {
            entity.ToTable("audio_files");
            entity.HasKey(audio => audio.Id);
            entity.Property(audio => audio.FileName).HasMaxLength(255).IsRequired();
            entity.Property(audio => audio.FilePath).HasMaxLength(1000).IsRequired();
            entity.Property(audio => audio.MimeType).HasMaxLength(128).IsRequired();
            entity.Property(audio => audio.SizeBytes).IsRequired();
            entity.Property(audio => audio.CreatedAt).IsRequired();
            entity.HasIndex(audio => audio.JobId).IsUnique();
        });

        base.OnModelCreating(modelBuilder);
    }
}
