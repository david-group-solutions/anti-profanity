namespace DavidGroup.Content.AntiProfanity.Tools.DataSetGenerator.UI;

/// <summary>
/// Renders overall + per-file progress bars to the console.
/// </summary>
public static class ConsoleProgressReporter
{
    public static void DrawReport(
        string fileLabel,
        long overallDone,
        long overallTotal,
        long fileDone,
        long fileTotal,
        int barWidth = 50)
    {
        ConsoleHelpers.TryClearConsole();

        double overallRatio = overallTotal <= 0 ? 1 : Math.Clamp(overallDone / (double)overallTotal, 0, 1);
        double fileRatio = fileTotal <= 0 ? 1 : Math.Clamp(fileDone / (double)fileTotal, 0, 1);

        const string overallLabel = "Overall progress";
        int labelWidth = Math.Max(overallLabel.Length, fileLabel.Length);

        DrawProgressBar(overallLabel, ConsoleColor.DarkCyan, overallRatio, overallDone, overallTotal, labelWidth, barWidth);
        DrawProgressBar(fileLabel, ConsoleColor.Cyan, fileRatio, fileDone, fileTotal, labelWidth, barWidth);

        Console.WriteLine();
    }


    private static void DrawProgressBar(
        string label,
        ConsoleColor labelColor,
        double ratio,
        long done,
        long total,
        int labelWidth,
        int barWidth)
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
        Console.WriteLine(" bytes)");
    }
}
