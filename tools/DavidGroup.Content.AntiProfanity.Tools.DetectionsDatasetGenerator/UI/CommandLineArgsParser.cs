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
                return TryParsePositiveInt(arg["--parallel=".Length..], defaultValue);

            bool isNamedFlag = arg.Equals("--parallel", StringComparison.OrdinalIgnoreCase)
                               || arg.Equals("-p", StringComparison.OrdinalIgnoreCase);

            if (isNamedFlag && i + 1 < commandLineArgs.Length)
                return TryParsePositiveInt(commandLineArgs[i + 1], defaultValue);
        }

        return defaultValue;
    }

    private static int TryParsePositiveInt(string text, int defaultValue)
        => int.TryParse(text, out int value) && value > 0 ? value : defaultValue;
}
