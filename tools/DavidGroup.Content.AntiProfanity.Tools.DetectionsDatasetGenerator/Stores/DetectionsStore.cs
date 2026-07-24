using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Stores;

/// <summary>
/// Provides methods for loading and saving detection data to a JSON file.
/// </summary>
public class DetectionsStore : IAsyncDisposable
{
    private readonly string _directory;

    private readonly Dictionary<string, StreamWriter> _writers =
        new(StringComparer.OrdinalIgnoreCase);

    public DetectionsStore(string outputDirectory)
    {
        _directory = Path.Combine(outputDirectory, "Detections");
        Directory.CreateDirectory(_directory);
    }

    public async Task ResetStateAsync()
    {
        foreach (StreamWriter writer in _writers.Values)
            await writer.DisposeAsync();

        _writers.Clear();

        Directory.Delete(_directory, recursive: true);
        Directory.CreateDirectory(_directory);
    }

    public async ValueTask AddAsync(
        string profanity,
        string fileName,
        long position,
        int length)
    {
        StreamWriter writer = GetWriter(profanity);

        DetectionRow row = new()
        {
            FileName = fileName,
            Position = position,
            Length = length
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(row));
        await writer.FlushAsync();
    }

    private StreamWriter GetWriter(string profanity)
    {
        if (_writers.TryGetValue(profanity, out StreamWriter? writer))
            return writer;

        string path = Path.Combine(_directory, $"{profanity}.jsonl");

        writer = new StreamWriter(
            new FileStream(
                path,
                FileMode.Append,
                FileAccess.Write,
                FileShare.Read
            )
        );

        _writers.Add(profanity, writer);

        return writer;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (StreamWriter writer in _writers.Values)
            await writer.DisposeAsync();
    }
}
