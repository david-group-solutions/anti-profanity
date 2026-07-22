namespace DavidGroup.Content.AntiProfanity.Tools.ConfirmedDetectionsDataSetGenerator.UI;

/// <summary>
/// Prompts the user in the console to confirm whether a flagged snippet is genuinely profanity.
/// </summary>
public static class ProfanityConfirmationPrompt
{
    public static bool Ask(string profanityInText, string enclosingWord, ReadOnlySpan<char> context)
    {
        Console.ResetColor();
        Console.Write("Possible profanity detected: ");

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
        int index = context.IndexOf(enclosingWord, StringComparison.OrdinalIgnoreCase);
        if (index >= 0)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(context[..index]);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(context[index..(index + enclosingWord.Length)]);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(context[(index + enclosingWord.Length)..]);
        }
        else
            Console.WriteLine(context);

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
