using CozyTts.Application.Abstractions.Audio;
using CozyTts.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace CozyTts.Infrastructure.Audio;

public sealed class LocalAudioStorage : IAudioStorage
{
    private readonly string _rootPath;

    public LocalAudioStorage(IOptions<AudioStorageOptions> options)
    {
        _rootPath = Path.GetFullPath(options.Value.OutputPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<StoredAudioFile> SaveAsync(
        string sourceFilePath,
        string fileName,
        string mimeType,
        double? durationSeconds,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(sourceFilePath))
        {
            throw new FileNotFoundException("Generated audio file was not found.", sourceFilePath);
        }

        var safeFileName = SanitizeFileName(fileName);
        var datePath = DateTimeOffset.UtcNow.ToString("yyyy/MM/dd");
        var directory = Path.Combine(_rootPath, datePath);
        Directory.CreateDirectory(directory);

        var destinationPath = Path.Combine(directory, safeFileName);
        await using (var source = File.OpenRead(sourceFilePath))
        await using (var destination = File.Create(destinationPath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        var info = new FileInfo(destinationPath);
        var storedPath = Path.GetRelativePath(_rootPath, destinationPath).Replace('\\', '/');

        return new StoredAudioFile(
            safeFileName,
            storedPath,
            mimeType,
            info.Length,
            durationSeconds);
    }

    public string ResolveAbsolutePath(string storedPath)
    {
        var absolutePath = Path.GetFullPath(Path.Combine(_rootPath, storedPath));
        if (!absolutePath.StartsWith(_rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Stored audio path is outside of configured storage root.");
        }

        return absolutePath;
    }

    private static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var safe = new string(fileName.Select(ch => invalid.Contains(ch) ? '-' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(safe) ? $"audio-{Guid.NewGuid():N}.mp3" : safe;
    }
}
