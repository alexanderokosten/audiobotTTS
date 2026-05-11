using CozyTts.Application.Abstractions.Jobs;
using CozyTts.Application.Services;
using Hangfire;

namespace CozyTts.Infrastructure.Jobs;

public sealed class HangfireVoiceJobQueue(IBackgroundJobClient backgroundJobs) : IVoiceJobQueue
{
    public string Enqueue(Guid jobId)
    {
        return backgroundJobs.Enqueue<IVoiceGenerationProcessor>(
            processor => processor.ProcessAsync(jobId, CancellationToken.None));
    }
}
