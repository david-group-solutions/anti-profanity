namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

public static class CommandLineArgsParser
{
    public static int ParseDegreeOfParallelism(string[] commandLineArgs)
    {
        const int defaultValue = 1;

        for (int i = 0; i < commandLineArgs.Length; i++)
        {
            string arg = commandLineArgs[i];

            if (arg.StartsWith("--parallel=", StringComparison.OrdinalIgnoreCase))
                return PrintDegreeOfParallelism(
                    TryParsePositiveInt(arg["--parallel=".Length..], defaultValue));

            bool isNamedFlag = arg.Equals("--parallel", StringComparison.OrdinalIgnoreCase)
                               || arg.Equals("-p", StringComparison.OrdinalIgnoreCase);

            if (isNamedFlag && i + 1 < commandLineArgs.Length)
                return PrintDegreeOfParallelism(
                    TryParsePositiveInt(commandLineArgs[i + 1], defaultValue));
        }

        return PrintDegreeOfParallelism(defaultValue);
    }

    private static int PrintDegreeOfParallelism(int degreeOfParallelism)
    {
        Console.Write("Running with a degree of parallelism of ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(degreeOfParallelism);
        Console.ResetColor();
        Console.WriteLine('.');

        return degreeOfParallelism;
    }

    private static int TryParsePositiveInt(string text, int defaultValue)
        => int.TryParse(text, out int value) && value > 0 ? value : defaultValue;
}
