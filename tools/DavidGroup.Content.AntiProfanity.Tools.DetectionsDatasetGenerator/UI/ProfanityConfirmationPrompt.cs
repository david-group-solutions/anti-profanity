using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

/// <summary>
/// Prompts the user in the console to confirm whether a flagged snippet is genuinely profanity.
/// </summary>
public static class ProfanityConfirmationPrompt
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static bool Ask(ProfanityOccurrence detection, ReadOnlySpan<char> smallChunk, int smallChunkStart)
    {
        Console.ResetColor();
        Console.Write("Possible profanity detected: ");

        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write($"\"{detection.Profanity}\"\n\n");

        Console.ResetColor();

        Console.WriteLine("Context:");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(smallChunk[..(detection.Index - smallChunkStart)]);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(smallChunk[(detection.Index - smallChunkStart)..(detection.Index + detection.Length - smallChunkStart)]);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine(smallChunk[(detection.Index + detection.Length - smallChunkStart)..]);

        if (detection.Details is not null)
        {
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("Detection Metadata:");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(JsonSerializer.Serialize(detection.Details, JsonOptions));
        }

        Console.ResetColor();
        Console.WriteLine();

        while (true)
        {
            Console.Write("Is this actually profanity? (Y/n): ");
            string? answer = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(answer))
                return true;

            switch (answer.ToLowerInvariant())
            {
                case "y":
                case "yes":
                    return true;

                case "n":
                case "no":
                    return false;

                default:
                    Console.WriteLine("Please enter Y, N, or press Enter for Yes.");
                    break;
            }
        }
    }
}
