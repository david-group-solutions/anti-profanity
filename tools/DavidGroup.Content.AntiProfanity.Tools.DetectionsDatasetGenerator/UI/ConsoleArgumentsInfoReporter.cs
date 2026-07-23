namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

public static class ConsoleArgumentsInfoReporter
{
    private static int? _degreeOfParallelism;

    public static void PrintDegreeOfParallelism(int degreeOfParallelism)
    {
        _degreeOfParallelism = degreeOfParallelism;

        PrintDegreeOfParallelism();
    }

    public static void PrintDegreeOfParallelism()
    {
        if (_degreeOfParallelism is null)
            throw new NullReferenceException($"Call {nameof(PrintDegreeOfParallelism)} at least once.");

        Console.Write("Running with a degree of parallelism of ");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(_degreeOfParallelism);
        Console.ResetColor();
        Console.WriteLine(".\n");
    }
}
