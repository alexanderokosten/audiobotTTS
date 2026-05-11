using CozyTts.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CozyTts.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IVoiceGenerationService, VoiceGenerationService>();
        services.AddScoped<IVoiceGenerationProcessor, VoiceGenerationProcessor>();
        services.AddScoped<IVoiceProfileService, VoiceProfileService>();

        return services;
    }
}
