using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

/// <summary>
/// Renders overall + per-file progress bars to the console.
/// </summary>
public static class ConsoleProgressReporter
{
    private enum FileState
    {
        Processing,
        Completed
    }

    private sealed class FileProgress
    {
        public long Done;
        public long Total;
        public FileState State;
    }

    private static readonly Lock SyncRoot = new();

    private static readonly Dictionary<string, FileProgress> Files = new();
    private static readonly List<string> FileOrder = [];

    public static void DrawReport(
        string fileLabel,
        long overallDone,
        long overallTotal,
        long fileDone,
        long fileTotal,
        int barWidth = 50)
    {
        lock (SyncRoot)
        {
            UpdateFileProgress(fileLabel, fileDone, fileTotal);
            Render(overallDone, overallTotal, barWidth);
        }
    }

    public static void CompleteFile(
        string fileLabel,
        long overallDone,
        long overallTotal,
        long fileTotal,
        int barWidth = 50)
    {
        lock (SyncRoot)
        {
            UpdateFileProgress(fileLabel, fileTotal, fileTotal);
            Render(overallDone, overallTotal, barWidth);
        }
    }

    private static void UpdateFileProgress(string fileLabel, long fileDone, long fileTotal)
    {
        if (!Files.TryGetValue(fileLabel, out FileProgress? progress))
        {
            progress = new FileProgress();
            Files[fileLabel] = progress;
            FileOrder.Add(fileLabel);
        }

        progress.Done = fileDone;
        progress.Total = fileTotal;
        progress.State = fileTotal <= 0 || fileDone >= fileTotal
            ? FileState.Completed
            : FileState.Processing;
    }

    private static void Render(long overallDone, long overallTotal, int barWidth)
    {
        ConsoleHelpers.TryClearConsole();

        double overallRatio = overallTotal <= 0 ? 1 : Math.Clamp(overallDone / (double)overallTotal, 0, 1);

        const string overallLabel = "Overall progress";
        int labelWidth = Math.Max(overallLabel.Length, FileOrder.Max(f => f.Length));

        DrawProgressBar(
            overallLabel,
            ConsoleColor.DarkCyan,
            overallRatio,
            overallDone,
            overallTotal,
            labelWidth,
            barWidth,
            status: null
        );
        Console.WriteLine();

        foreach (string file in FileOrder)
        {
            FileProgress progress = Files[file];
            double fileRatio = progress.Total <= 0 ? 1 : Math.Clamp(progress.Done / (double)progress.Total, 0, 1);
            ConsoleColor barColor = progress.State == FileState.Completed ? ConsoleColor.DarkGreen : ConsoleColor.Cyan;

            DrawProgressBar(
                file,
                barColor,
                fileRatio,
                progress.Done,
                progress.Total,
                labelWidth,
                barWidth,
                progress.State
            );
        }

        Console.WriteLine();
    }

    private static void DrawProgressBar(
        string label,
        ConsoleColor labelColor,
        double ratio,
        long done,
        long total,
        int labelWidth,
        int barWidth,
        FileState? status)
    {
        int filled = (int)Math.Round(barWidth * ratio);

        Console.ForegroundColor = labelColor;
        Console.Write(label.PadRight(labelWidth));
        Console.ResetColor();

        Console.Write(": [");

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(new string('#', filled));

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('-', barWidth - filled));

        Console.ResetColor();
        Console.Write("] ");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"{ratio,6:P1}");

        Console.ResetColor();
        Console.Write(" (");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{done:N0}");

        Console.ResetColor();
        Console.Write(" / ");

        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{total:N0}");

        Console.ResetColor();
        Console.Write(" bytes)");

        if (status is not null)
            WriteRightAlignedStatus(status.Value);

        Console.WriteLine();
    }

    private static void WriteRightAlignedStatus(FileState status)
    {
        const int rightPadding = 2;

        (string text, ConsoleColor color) = status switch
        {
            FileState.Completed => ("[done]", ConsoleColor.DarkGreen),
            FileState.Processing => ("[processing]", ConsoleColor.Cyan),
            _ => (string.Empty, ConsoleColor.Gray)
        };

        int left = Console.WindowWidth - text.Length - rightPadding;

        if (Console.CursorLeft < left)
            Console.SetCursorPosition(left, Console.CursorTop);
        else
            Console.Write("  ");

        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ResetColor();
    }
}
