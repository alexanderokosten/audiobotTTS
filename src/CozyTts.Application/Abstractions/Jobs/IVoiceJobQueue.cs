namespace CozyTts.Application.Abstractions.Jobs;

public interface IVoiceJobQueue
{
    string Enqueue(Guid jobId);
}
