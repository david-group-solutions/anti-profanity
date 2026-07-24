using System.Collections.ObjectModel;
using System.Text;

using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Options;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Stores;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

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
        ApplicationOptions options,
        CancellationToken cancellationToken)
    {
        _reportInterval = 100_000L * options.Parallel;

        string resolvedInputDirectory = PathHelpers.ResolveHomeDirectory(options.InputDir);
        string[] files = Directory.GetFiles(resolvedInputDirectory, "*.txt", SearchOption.TopDirectoryOnly);

        if (options.ResetState)
            await detectionsStore.ResetStateAsync();
        State state = await stateStore.LoadAsync(options.ResetState);

        long totalBytesAllFiles = files.Sum(file => new FileInfo(file).Length);
        long alreadyProcessedBytes = files.Sum(file =>
            state.Statuses.TryGetValue(file, out FileStatus? status) ? status.Position : 0);

        ProgressTracker progressTracker = new(totalBytesAllFiles, alreadyProcessedBytes);
        SemaphoreSlim syncLock = new(1, 1);

        ParallelOptions parallelOptions = new()
        {
            MaxDegreeOfParallelism = Math.Max(1, options.Parallel),
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(files, parallelOptions, async (file, ct) =>
        {
            if (!File.Exists(file))
                throw new FileNotFoundException($"File '{file}' not found.");

            await ScanFileAsync(file, state, progressTracker, syncLock, ct);
        });
    }

    private async Task ScanFileAsync(
        string file,
        State state,
        ProgressTracker progressTracker,
        SemaphoreSlim syncLock,
        CancellationToken cancellationToken)
    {
        FileStatus currentFileStatus = state.GetFileStatus(file);
        string lastIncompleteChunk = currentFileStatus.LastIncompleteChunk;
        long currentFileLength = new FileInfo(file).Length;

        await using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Position = currentFileStatus.Position;

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
                    long absolutePosition = chunkStartPosition + Encoding.UTF8.GetByteCount(testableChunk[..occurrence.Index]);

                    await detectionsStore.AddAsync(occurrence.Profanity, file, absolutePosition, occurrence.Length);
                }

                currentFileStatus.Position = fs.Position;
                currentFileStatus.LastIncompleteChunk = lastIncompleteChunk;
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
                Path.GetFileName(file),
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
            Path.GetFileName(file),
            finalTotalBytesProcessed,
            progressTracker.TotalBytesAllFiles,
            currentFileLength
        );
    }

    private sealed class ProgressTracker(long totalBytesAllFiles, long initialBytesProcessed)
    {
        public long TotalBytesAllFiles { get; } = totalBytesAllFiles;

        private long _totalBytesProcessed = initialBytesProcessed;

        public long AddProcessedBytes(long bytes) => Interlocked.Add(ref _totalBytesProcessed, bytes);
    }
}
