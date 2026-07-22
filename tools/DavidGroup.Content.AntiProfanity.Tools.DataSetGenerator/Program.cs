using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

string[] files =
[
    "Romeo_and_Juliet.txt",
    "Wuthering_Heights.txt",
    "Pride_and_Prejudice.txt"
];

JsonSerializerOptions jsonOptions = new()
{
    WriteIndented = true
};

ServiceCollection services = new();
IConfiguration configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        {
            "AntiProfanity:DataSourcesBasePath", "DataSources"
        },
        {
            "AntiProfanity:DataSources:0", "en.json"
        },
        {
            "AntiProfanity:DataSources:1", "emoji.json"
        },
        {
            "AntiProfanity:DataSources:2", "ru.txt"
        }
    })
    .Build();

services.AddAntiProfanity(configuration)
    .AddHandler<ProfanityDetectionJsonHandler>()
    .AddHandler<ProfanityDetectionTxtHandler>();

IServiceProvider serviceProvider = services.BuildServiceProvider();

await serviceProvider.InitializeAntiProfanityDataSourcesAsync();

IAntiProfanityService antiProfanityService = serviceProvider.GetRequiredService<IAntiProfanityService>();

string projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

Directory.CreateDirectory(Path.Combine(projectDirectory, "Output"));

string confirmedDetectionsFilePath = Path.Combine(projectDirectory, "Output", "confirmed_detections.txt");
await using FileStream confirmedDetectionsFileStream = new(confirmedDetectionsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
confirmedDetectionsFileStream.Seek(0, SeekOrigin.End);

string stateFilePath = Path.Combine(projectDirectory, "Output", "state.txt");
State? state = null;
if (File.Exists(stateFilePath))
    state = JsonSerializer.Deserialize<State>(File.ReadAllText(stateFilePath), jsonOptions);

state ??= new State();

foreach (string file in files)
{
    string inputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input", file);

    if (!File.Exists(inputPath))
        throw new FileNotFoundException($"File '{file}' not found in '{inputPath}'.");

    const int bufferSize = 4096;
    byte[] buffer = new byte[bufferSize];

    FileStatus? currentFileStatus = state.Statuses.GetValueOrDefault(file);

    await using FileStream fs = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    if (currentFileStatus is null)
        fs.Seek(0, SeekOrigin.Begin);
    else
        fs.Position = currentFileStatus.Position;
    int bytesRead;

    string lastIncompleteChunk = currentFileStatus?.LastIncompleteChunk ?? string.Empty;

    while ((bytesRead = fs.Read(buffer, 0, bufferSize)) > 0)
    {
        long positionBeforeThisRead = fs.Position - bytesRead;
        long chunkStartPosition = positionBeforeThisRead - Encoding.UTF8.GetByteCount(lastIncompleteChunk);

        string chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        int lastSpaceIndex = chunk.LastIndexOf(' ');
        if (lastSpaceIndex == -1)
        {
            lastIncompleteChunk += chunk;
            continue;
        }

        string testableChunk = string.Concat(lastIncompleteChunk, chunk.Substring(0, lastSpaceIndex + 1));
        lastIncompleteChunk = chunk.Substring(lastSpaceIndex);

        ReadOnlyCollection<ProfanityOccurrence> detections = await antiProfanityService.DetectAsync(testableChunk);
        foreach (ProfanityOccurrence detection in detections)
        {
            string profanityInText = testableChunk.Substring(detection.Index, detection.Length);
            string enclosingWord = GetEnclosingWord(testableChunk, detection).ToString();
            string context = GetSmallChunk(testableChunk, detection, additionalSize: 50);

            if (AskIfActuallyProfanity(file, profanityInText, enclosingWord, context))
            {
                Console.Clear();

                long absolutePosition = chunkStartPosition
                                        + Encoding.UTF8.GetByteCount(testableChunk[..detection.Index]);

                string confirmedLine =
                    $"source={file}\n" +
                    $"position={absolutePosition}\n" +
                    $"detected={profanityInText}\n\n";

                await confirmedDetectionsFileStream.WriteAsync(Encoding.UTF8.GetBytes(confirmedLine));
                await confirmedDetectionsFileStream.FlushAsync();
            }
        }

        if (currentFileStatus == null)
        {
            currentFileStatus = new FileStatus
            {
                Position = fs.Position,
                LastIncompleteChunk = lastIncompleteChunk
            };

            state.Statuses.Add(file, currentFileStatus);
        }
        else
        {
            currentFileStatus.Position = fs.Position;
            currentFileStatus.LastIncompleteChunk = lastIncompleteChunk;
        }

        string serializedState = JsonSerializer.Serialize(state, jsonOptions);
        await File.WriteAllTextAsync(stateFilePath, serializedState);
    }
}

return;

ReadOnlySpan<char> GetEnclosingWord(string content, ProfanityOccurrence occurrence)
{
    int start = occurrence.Index;
    while (start > 0 && char.IsLetterOrDigit(content[start - 1]))
        start--;

    int end = occurrence.Index + occurrence.Length;
    while (end < content.Length && char.IsLetterOrDigit(content[end]))
        end++;

    return content.AsSpan(start, end - start);
}

#pragma warning disable CS8321 // Local function is declared but never used
string GetSmallChunk(string content, ProfanityOccurrence occurrence, int additionalSize = 30)
#pragma warning restore CS8321 // Local function is declared but never used
{
    int before = Math.Min(additionalSize, occurrence.Index);
    int after = Math.Min(additionalSize, content.Length - (occurrence.Index + occurrence.Length));

    if (before < additionalSize)
    {
        after = Math.Min(
            content.Length - (occurrence.Index + occurrence.Length),
            after + (additionalSize - before));
    }

    if (after < additionalSize)
    {
        before = Math.Min(
            occurrence.Index,
            before + (additionalSize - after));
    }

    int start = occurrence.Index - before;
    int end = occurrence.Index + occurrence.Length + after;

    return content[start..end];
}

bool AskIfActuallyProfanity(string fileName, string profanityInText, string enclosingWord, string context)
{
    Console.Write($"[{fileName}] Possible profanity detected: ");

    Console.ForegroundColor = ConsoleColor.Red;
    Console.Write($"\"{profanityInText}\"");

    Console.ResetColor();
    Console.Write(" (in word: ");

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write($"\"{enclosingWord}\"");

    Console.ResetColor();
    Console.WriteLine(")");
    Console.WriteLine();

    Console.WriteLine("Context:");
    Console.ForegroundColor = ConsoleColor.Gray;
    int index = context.IndexOf(enclosingWord, StringComparison.OrdinalIgnoreCase);
    if (index >= 0)
    {
        Console.Write(context[..index]);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(context.AsSpan(index, enclosingWord.Length));

        Console.ResetColor();
        Console.WriteLine(context[(index + enclosingWord.Length)..]);
    }
    else
        Console.WriteLine(context);

    Console.WriteLine();

    while (true)
    {
        Console.Write("Is this actually profanity? (Y/n): ");
        string? answer = Console.ReadLine()?.Trim();

        return answer?.ToLowerInvariant() switch
        {
            "y" or "yes" => true,
            "n" or "no" => false,
            _ => true
        };
    }
}

public class State
{
    public Dictionary<string, FileStatus> Statuses { get; set; } = [];
}

public class FileStatus
{
    public long Position { get; set; }
    public string LastIncompleteChunk { get; set; } = string.Empty;
}
