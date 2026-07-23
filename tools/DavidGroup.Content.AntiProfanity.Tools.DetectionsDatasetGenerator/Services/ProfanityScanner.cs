using System.Collections.ObjectModel;
using System.Text;

using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services.Stores;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services;

/// <summary>
/// Scans a set of text files for profanity, stores profanities
/// and checkpoints progress so a run can resume later.
/// </summary>
public class ProfanityScanner(
    IAntiProfanityService antiProfanityService,
    DetectionsStore detectionsStore,
    StateStore stateStore)
{
    private const int BufferSize = 4096;

    private long _reportInterval;

    public async Task RunAsync(
        string inputDirectory,
        IReadOnlyList<string> files,
        int degreeOfParallelism,
        CancellationToken cancellationToken)
    {
        _reportInterval = 100_000L * degreeOfParallelism;

        Detections detections = await detectionsStore.LoadAsync();
        State state = await stateStore.LoadAsync();

        long totalBytesAllFiles = files.Sum(file => new FileInfo(Path.Combine(inputDirectory, file)).Length);
        long alreadyProcessedBytes = files.Sum(file =>
            state.Statuses.TryGetValue(file, out FileStatus? status) ? status.Position : 0);

        ProgressTracker progressTracker = new(totalBytesAllFiles, alreadyProcessedBytes);
        SemaphoreSlim syncLock = new(1, 1);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, degreeOfParallelism),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
        {
            string inputPath = Path.Combine(inputDirectory, file);

            if (!File.Exists(inputPath))
                throw new FileNotFoundException($"File '{file}' not found in '{inputPath}'.");

            await ScanFileAsync(file, inputPath, detections, state, progressTracker, syncLock, ct);
        });
    }

    private async Task ScanFileAsync(
        string file,
        string inputPath,
        Detections detections,
        State state,
        ProgressTracker progressTracker,
        SemaphoreSlim syncLock,
        CancellationToken cancellationToken)
    {
        long currentFileLength = new FileInfo(inputPath).Length;

        await using FileStream fs = new(inputPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        FileStatus? currentFileStatus = state.Statuses.GetValueOrDefault(file);
        fs.Position = currentFileStatus?.Position ?? 0;
        string lastIncompleteChunk = currentFileStatus?.LastIncompleteChunk ?? string.Empty;

        byte[] buffer = new byte[BufferSize];
        int bytesRead;

        long nextReportAt = _reportInterval;

        while ((bytesRead = fs.Read(buffer, 0, BufferSize)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            long positionBeforeThisRead = fs.Position - bytesRead;
            long chunkStartPosition = positionBeforeThisRead - Encoding.UTF8.GetByteCount(lastIncompleteChunk);

            ReadOnlySpan<char> chunk = Encoding.UTF8.GetString(buffer, 0, bytesRead).AsSpan();

            int lastSpaceIndex = chunk.LastIndexOf(' ');
            if (lastSpaceIndex == -1)
            {
                lastIncompleteChunk += chunk.ToString();
                continue;
            }

            string testableChunk = string.Concat(lastIncompleteChunk, chunk[..(lastSpaceIndex + 1)]);
            lastIncompleteChunk = chunk[lastSpaceIndex..].ToString();

            ReadOnlyCollection<ProfanityOccurrence> occurrences = await antiProfanityService.DetectAsync(testableChunk);

            await syncLock.WaitAsync(cancellationToken);
            try
            {
                foreach (ProfanityOccurrence occurrence in occurrences)
                {
                    string profanityInText = testableChunk.Substring(occurrence.Index, occurrence.Length);
                    long absolutePosition = chunkStartPosition + Encoding.UTF8.GetByteCount(testableChunk[..occurrence.Index]);

                    Dictionary<string, Detection> fileDetection =
                        UpsertDetection(detections, profanityInText, file, absolutePosition, occurrence.Details);
                    await detectionsStore.SaveAsync(profanityInText, fileDetection);
                }

                currentFileStatus = UpsertFileStatus(state, currentFileStatus, file, fs.Position, lastIncompleteChunk);
                await stateStore.SaveAsync(state);
            }
            finally
            {
                syncLock.Release();
            }

            long totalBytesProcessedSoFar = progressTracker.AddProcessedBytes(bytesRead);
            if (totalBytesProcessedSoFar < nextReportAt)
                continue;

            ConsoleProgressReporter.DrawReport(
                file,
                totalBytesProcessedSoFar,
                progressTracker.TotalBytesAllFiles,
                fs.Position,
                currentFileLength
            );

            while (totalBytesProcessedSoFar >= nextReportAt)
                nextReportAt += _reportInterval;
        }

        long finalTotalBytesProcessed = progressTracker.AddProcessedBytes(0);
        ConsoleProgressReporter.CompleteFile(
            file,
            finalTotalBytesProcessed,
            progressTracker.TotalBytesAllFiles,
            currentFileLength
        );
    }

    private static Dictionary<string, Detection> UpsertDetection(
        Detections detections,
        string profanity,
        string file,
        long position,
        object? metadata)
    {
        if (!detections.Profanities.TryGetValue(profanity, out Dictionary<string, Detection>? fileDetections))
        {
            fileDetections = [];
            detections.Profanities.Add(profanity, fileDetections);
        }

        if (!fileDetections.TryGetValue(file, out Detection? detection))
        {
            detection = new Detection
            {
                AbsolutePositions = [position],
                Metadata = metadata
            };

            fileDetections.Add(file, detection);
        }
        else
        {
            detection.AbsolutePositions.Add(position);
            detection.Metadata = metadata;
        }

        return fileDetections;
    }

    private static FileStatus UpsertFileStatus(
        State state,
        FileStatus? currentFileStatus,
        string file,
        long position,
        string lastIncompleteChunk)
    {
        if (currentFileStatus is null)
        {
            currentFileStatus = new FileStatus();
            state.Statuses.Add(file, currentFileStatus);
        }

        currentFileStatus.Position = position;
        currentFileStatus.LastIncompleteChunk = lastIncompleteChunk;

        return currentFileStatus;
    }

    private sealed class ProgressTracker(long totalBytesAllFiles, long initialBytesProcessed)
    {
        public long TotalBytesAllFiles { get; } = totalBytesAllFiles;

        private long _totalBytesProcessed = initialBytesProcessed;

        public long AddProcessedBytes(long bytes) => Interlocked.Add(ref _totalBytesProcessed, bytes);
    }
}
