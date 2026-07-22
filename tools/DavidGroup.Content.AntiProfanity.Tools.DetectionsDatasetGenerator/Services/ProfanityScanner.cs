using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services;

/// <summary>
/// Scans a set of text files for profanity, prompts the user to confirm each hit, records
/// confirmed detections to a stream, and checkpoints progress so a run can resume later.
/// </summary>
public class ProfanityScanner(IAntiProfanityService antiProfanityService, StateStore stateStore)
{
    private const int BufferSize = 4096;
    private const int ContextPaddingSize = 50;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public async Task RunAsync(
        string inputDirectory,
        IReadOnlyList<string> files,
        string confirmedDetectionsFilePath,
        Stream confirmedDetectionsStream,
        Stream wrongDetectionsStream)
    {
        State state = stateStore.Load();

        long totalBytesAllFiles = files.Sum(f => new FileInfo(Path.Combine(inputDirectory, f)).Length);
        long overallBytesProcessed = 0;

        foreach (string file in files)
        {
            string inputPath = Path.Combine(inputDirectory, file);

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"File '{file}' not found in '{inputPath}'.");

            await ScanFileAsync(
                file,
                inputPath,
                state,
                confirmedDetectionsFilePath,
                confirmedDetectionsStream,
                wrongDetectionsStream,
                overallBytesProcessed,
                totalBytesAllFiles
            );

            overallBytesProcessed += new FileInfo(inputPath).Length;
        }
    }

    private async Task ScanFileAsync(
        string file,
        string inputPath,
        State state,
        string confirmedDetectionsFilePath,
        Stream confirmedDetectionsStream,
        Stream wrongDetectionsStream,
        long overallBytesProcessed,
        long totalBytesAllFiles)
    {
        long currentFileLength = new FileInfo(inputPath).Length;
        byte[] buffer = new byte[BufferSize];

        FileStatus? currentFileStatus = state.Statuses.GetValueOrDefault(file);

        await using FileStream fs = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Position = currentFileStatus?.Position ?? 0;

        string lastIncompleteChunk = currentFileStatus?.LastIncompleteChunk ?? string.Empty;
        int bytesRead;

        while ((bytesRead = fs.Read(buffer, 0, BufferSize)) > 0)
        {
            long positionBeforeThisRead = fs.Position - bytesRead;
            long chunkStartPosition = positionBeforeThisRead - Encoding.UTF8.GetByteCount(lastIncompleteChunk);

            ConsoleProgressReporter.DrawReport(file, overallBytesProcessed + fs.Position, totalBytesAllFiles, fs.Position, currentFileLength);

            ReadOnlySpan<char> chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead).AsSpan();

            int lastSpaceIndex = chunk.LastIndexOf(' ');
            if (lastSpaceIndex == -1)
            {
                lastIncompleteChunk += chunk.ToString();
                continue;
            }

            string testableChunk = string.Concat(lastIncompleteChunk, chunk[..(lastSpaceIndex + 1)]);
            lastIncompleteChunk = chunk[lastSpaceIndex..].ToString();

            await ProcessChunkAsync(
                file,
                testableChunk,
                chunkStartPosition,
                confirmedDetectionsFilePath,
                confirmedDetectionsStream,
                wrongDetectionsStream,
                overallBytesProcessed,
                totalBytesAllFiles,
                fs,
                currentFileLength
            );

            currentFileStatus = UpsertFileStatus(state, file, currentFileStatus, fs.Position, lastIncompleteChunk);

            await stateStore.SaveAsync(state);
        }
    }

    private async Task ProcessChunkAsync(
        string file,
        string testableChunk,
        long chunkStartPosition,
        string confirmedDetectionsFilePath,
        Stream confirmedDetectionsStream,
        Stream wrongDetectionsStream,
        long overallBytesProcessed,
        long totalBytesAllFiles,
        FileStream fs,
        long currentFileLength)
    {
        ReadOnlyCollection<ProfanityOccurrence> detections = await antiProfanityService.DetectAsync(testableChunk);

        foreach (ProfanityOccurrence detection in detections)
        {
            string profanityInText = testableChunk.Substring(detection.Index, detection.Length);
            (int Start, int End) smallChunkBoundaries =
                TextChunkHelper.GetSmallChunkBoundaries(testableChunk, detection, ContextPaddingSize);
            ReadOnlySpan<char> smallChunk = testableChunk.AsSpan()
                .Slice(smallChunkBoundaries.Start, smallChunkBoundaries.End - smallChunkBoundaries.Start);

            long absolutePosition = chunkStartPosition + Encoding.UTF8.GetByteCount(testableChunk[..detection.Index]);

            ConsoleProgressReporter.DrawReport(file, overallBytesProcessed + fs.Position, totalBytesAllFiles, fs.Position, currentFileLength);

            if (File.ReadLines(confirmedDetectionsFilePath).Any(line => line == $"detected={profanityInText}"))
                continue;

            if (ProfanityConfirmationPrompt.Ask(detection, smallChunk, smallChunkBoundaries.Start))
            {
                string confirmedLine =
                    $"source={file}\n" +
                    $"position={absolutePosition}\n" +
                    $"detected={profanityInText}\n\n";

                await confirmedDetectionsStream.WriteAsync(Encoding.UTF8.GetBytes(confirmedLine));
                await confirmedDetectionsStream.FlushAsync();
            }
            else
            {
                string metadata = detection.Details is not null
                    ? $"\n{JsonSerializer.Serialize(detection.Details, JsonOptions)}\n\n"
                    : "none\n\n";

                string wrongLine =
                    $"source={file}\n" +
                    $"position={absolutePosition}\n" +
                    $"detected={profanityInText}\n" +
                    $"metadata={metadata}";

                await wrongDetectionsStream.WriteAsync(Encoding.UTF8.GetBytes(wrongLine));
                await wrongDetectionsStream.FlushAsync();
            }
        }
    }

    private static FileStatus UpsertFileStatus(
        State state,
        string file,
        FileStatus? currentFileStatus,
        long position,
        string lastIncompleteChunk)
    {
        if (currentFileStatus is null)
        {
            currentFileStatus = new FileStatus
            {
                Position = position,
                LastIncompleteChunk = lastIncompleteChunk
            };

            state.Statuses.Add(file, currentFileStatus);
        }
        else
        {
            currentFileStatus.Position = position;
            currentFileStatus.LastIncompleteChunk = lastIncompleteChunk;
        }

        return currentFileStatus;
    }
}
