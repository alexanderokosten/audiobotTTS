namespace CozyTts.Application.Abstractions.Audio;

public interface IAudioStorage
{
    Task<StoredAudioFile> SaveAsync(
        string sourceFilePath,
        string fileName,
        string mimeType,
        double? durationSeconds,
        CancellationToken cancellationToken);

    string ResolveAbsolutePath(string storedPath);
}
