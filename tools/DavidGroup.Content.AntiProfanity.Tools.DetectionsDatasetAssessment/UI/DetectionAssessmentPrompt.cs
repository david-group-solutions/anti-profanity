using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Enums;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.UI;

/// <summary>
/// Prompts the user in the console to confirm whether a flagged snippet is genuinely profanity.
/// </summary>
public static class DetectionAssessmentPrompt
{
    public static AssessmentAnswer Ask(
        ReadOnlySpan<char> text,
        string rule,
        int detectionIndex,
        int detectionLength,
        int contextPaddingSize)
    {
        ConsoleHelpers.TryClearConsole();

        (int StartIndex, int EndIndex) contextBoundaries =
            TextChunkHelper.GetSmallChunkBoundaries(text, detectionIndex, detectionLength, contextPaddingSize);

        ReadOnlySpan<char> context = text[contextBoundaries.StartIndex..contextBoundaries.EndIndex];
        ReadOnlySpan<char> matchedProfanity = text[detectionIndex..(detectionIndex + detectionLength)];

        int localStart = detectionIndex - contextBoundaries.StartIndex;
        int localEnd = localStart + detectionLength;

        Console.Clear();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, 80)));
        Console.ResetColor();

        WriteLabel("Rule");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine(rule);

        WriteLabel("Matched");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(matchedProfanity);

        Console.ResetColor();
        Console.WriteLine();

        WriteLabel("Context");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("...");
        Console.Write(context[..localStart].TrimStart());

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(context[localStart..localEnd]);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(context[localEnd..].TrimEnd());
        Console.WriteLine("...");
        Console.ResetColor();

        Console.WriteLine();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(new string('─', Math.Min(Console.WindowWidth - 1, 80)));
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("[Y] True positive");

        Console.ResetColor();
        Console.Write("  ");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("[N] False positive");

        Console.ResetColor();
        Console.Write("  ");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("[S] Skip");

        Console.ResetColor();
        Console.WriteLine("   (Enter = True positive)");

        Console.Write("> ");

        while (true)
        {
            ConsoleKeyInfo keyInfo = Console.ReadKey();

            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                case ConsoleKey.Y:
                    return AssessmentAnswer.Yes;

                case ConsoleKey.N:
                    return AssessmentAnswer.No;

                case ConsoleKey.S:
                    return AssessmentAnswer.Skip;

                default:
                    Console.WriteLine();
                    Console.WriteLine("Please press Y, N, or S, or press Enter for Y.");
                    Console.Write("> ");
                    break;
            }
        }

        static void WriteLabel(string label)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write($"{label,-8}: ");
            Console.ResetColor();
        }
    }
}
