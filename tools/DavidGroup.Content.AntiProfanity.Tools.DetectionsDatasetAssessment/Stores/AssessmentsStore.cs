using System.Text.Json;
using System.Text.Json.Serialization;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Enums;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Stores;

public class AssessmentsStore : IAsyncDisposable
{
    private readonly string _directory;

    private readonly Dictionary<string, StreamWriter> _writers =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HashSet<long>> _answeredPositions =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly JsonSerializerOptions _jsonSerializerOptions = new();

    public AssessmentsStore(string outputDirectory)
    {
        _directory = outputDirectory;
        Directory.CreateDirectory(_directory);

        _jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    }

    public async ValueTask AddAsync(
        string rule,
        string fileName,
        long position,
        int length,
        AssessmentAnswer answer)
    {
        StreamWriter writer = GetWriter(rule);

        AssessmentRow row = new()
        {
            FileName = fileName,
            Position = position,
            Length = length,
            Answer = answer
        };

        await writer.WriteLineAsync(JsonSerializer.Serialize(row, _jsonSerializerOptions));
        await writer.FlushAsync();

        if (_answeredPositions.TryGetValue(rule, out HashSet<long>? positions))
            positions.Add(position);
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

    public async Task<bool> AlreadyAnsweredAsync(string file, long position)
    {
        HashSet<long> positions = await GetAnsweredPositionsAsync(file);
        return positions.Contains(position);
    }

    private async Task<HashSet<long>> GetAnsweredPositionsAsync(string file)
    {
        if (_answeredPositions.TryGetValue(file, out HashSet<long>? positions))
            return positions;

        positions = [];

        string path = Path.Combine(_directory, $"{Path.GetFileNameWithoutExtension(file)}.jsonl");

        if (File.Exists(path))
        {
            using StreamReader reader = File.OpenText(path);

            while (await reader.ReadLineAsync() is { } line)
            {
                AssessmentRow? row = JsonSerializer.Deserialize<AssessmentRow>(line, _jsonSerializerOptions);

                if (row is not null)
                    positions.Add(row.Position);
            }
        }

        _answeredPositions.Add(file, positions);

        return positions;
    }

    public async ValueTask DisposeAsync()
    {
        foreach (StreamWriter writer in _writers.Values)
            await writer.DisposeAsync();
    }
}
