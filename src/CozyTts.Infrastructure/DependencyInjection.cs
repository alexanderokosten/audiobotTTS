using CozyTts.Application.Abstractions.Audio;
using CozyTts.Application.Abstractions.Jobs;
using CozyTts.Application.Abstractions.Persistence;
using CozyTts.Application.Abstractions.System;
using CozyTts.Application.Abstractions.Tts;
using CozyTts.Infrastructure.Audio;
using CozyTts.Infrastructure.Jobs;
using CozyTts.Infrastructure.Options;
using CozyTts.Infrastructure.Persistence;
using CozyTts.Infrastructure.Persistence.Repositories;
using CozyTts.Infrastructure.System;
using CozyTts.Infrastructure.Tts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CozyTts.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration["POSTGRES_CONNECTION"]
            ?? configuration.GetConnectionString("Postgres")
            ?? "Host=localhost;Port=5432;Database=cozytts;Username=cozytts;Password=cozytts";

        services.AddDbContext<CozyTtsDbContext>(options => options.UseNpgsql(connectionString));

        services.Configure<PiperTtsOptions>(options =>
        {
            options.PiperBinPath = configuration["PIPER_BIN_PATH"]
                ?? configuration["Tts:PiperBinPath"]
                ?? options.PiperBinPath;
            options.PiperModelsPath = configuration["PIPER_MODELS_PATH"]
                ?? configuration["Tts:PiperModelsPath"]
                ?? options.PiperModelsPath;
            options.FfmpegBinPath = configuration["FFMPEG_BIN_PATH"]
                ?? configuration["Tts:FfmpegBinPath"]
                ?? options.FfmpegBinPath;
            options.TempPath = configuration["TTS_TEMP_PATH"]
                ?? configuration["Tts:TempPath"]
                ?? options.TempPath;

            var chunkValue = configuration["TTS_MAX_CHUNK_CHARACTERS"]
                ?? configuration["Tts:MaxChunkCharacters"];
            if (int.TryParse(chunkValue, out var maxChunkCharacters) && maxChunkCharacters >= 250)
            {
                options.MaxChunkCharacters = maxChunkCharacters;
            }
        });

        services.Configure<AudioStorageOptions>(options =>
        {
            options.OutputPath = configuration["AUDIO_OUTPUT_PATH"]
                ?? configuration["Storage:AudioOutputPath"]
                ?? options.OutputPath;
        });

        services.Configure<QwenTtsOptions>(options =>
        {
            options.Endpoint = configuration["QWEN_TTS_ENDPOINT"]
                ?? configuration["QwenTts:Endpoint"]
                ?? options.Endpoint;
            options.FfmpegBinPath = configuration["FFMPEG_BIN_PATH"]
                ?? configuration["QwenTts:FfmpegBinPath"]
                ?? options.FfmpegBinPath;
            options.TempPath = configuration["TTS_TEMP_PATH"]
                ?? configuration["QwenTts:TempPath"]
                ?? options.TempPath;

            var timeoutValue = configuration["QWEN_TTS_TIMEOUT_MINUTES"]
                ?? configuration["QwenTts:TimeoutMinutes"];
            if (int.TryParse(timeoutValue, out var timeoutMinutes) && timeoutMinutes >= 1)
            {
                options.TimeoutMinutes = timeoutMinutes;
            }

            var chunkValue = configuration["QWEN_MAX_CHUNK_CHARACTERS"]
                ?? configuration["QwenTts:MaxChunkCharacters"];
            if (int.TryParse(chunkValue, out var maxChunkCharacters) && maxChunkCharacters >= 250)
            {
                options.MaxChunkCharacters = maxChunkCharacters;
            }

            var sampleRateValue = configuration["QWEN_OUTPUT_SAMPLE_RATE"]
                ?? configuration["QwenTts:OutputSampleRate"];
            if (int.TryParse(sampleRateValue, out var sampleRate) && sampleRate >= 8000)
            {
                options.OutputSampleRate = sampleRate;
            }
        });

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IAudioStorage, LocalAudioStorage>();
        services.AddScoped<PiperTtsEngine>();
        services.AddScoped<IConcreteTtsEngine>(provider => provider.GetRequiredService<PiperTtsEngine>());
        services.AddHttpClient<QwenTtsEngine>((provider, client) =>
        {
            var qwenOptions = provider.GetRequiredService<IOptions<QwenTtsOptions>>().Value;
            client.BaseAddress = new Uri(qwenOptions.Endpoint.TrimEnd('/') + "/", UriKind.Absolute);
            client.Timeout = TimeSpan.FromMinutes(qwenOptions.TimeoutMinutes);
        });
        services.AddScoped<IConcreteTtsEngine>(provider => provider.GetRequiredService<QwenTtsEngine>());
        services.AddScoped<ITtsEngine, RoutedTtsEngine>();
        services.AddScoped<IVoiceJobQueue, HangfireVoiceJobQueue>();

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<CozyTtsDbContext>());
        services.AddScoped<IVoiceProjectRepository, VoiceProjectRepository>();
        services.AddScoped<IVoiceProfileRepository, VoiceProfileRepository>();
        services.AddScoped<IGenerationJobRepository, GenerationJobRepository>();

        return services;
    }
}
