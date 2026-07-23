using CommandLine;
using CommandLine.Text;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Options;

public sealed class ApplicationOptions
{
    [Option('i', "input-dir", Required = true, HelpText = "Input directory to be processed.")]
    public required string InputDir { get; init; }

    [Option('o', "output-dir", Required = true, HelpText = "Output directory to store dataset.")]
    public required string OutputDir { get; init; }

    [Option('p', "parallel", Required = false, HelpText = "Parallel files to be processed.")]
    public int Parallel { get; init; } = 4;

    [Option("reset-state", Required = false, HelpText = "Discard any saved progress and restart processing from the beginning.")]
    public bool ResetState { get; init; }

    [Usage(ApplicationAlias = "profanity-detections-dataset-generator")]
    public static IEnumerable<Example> Examples =>
        new List<Example>
        {
            new("Run parallel processing of input files",
                new ApplicationOptions
                {
                    InputDir = "~/DetectionsDataSet/Input",
                    OutputDir = "~/DetectionsDataSet/Output",
                    Parallel = 4
                }
            )
        };
}
