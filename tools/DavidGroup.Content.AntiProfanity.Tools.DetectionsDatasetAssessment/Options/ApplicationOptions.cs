using CommandLine;
using CommandLine.Text;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Options;

public sealed class ApplicationOptions
{
    [Option('s', "sources-dir", Required = true, HelpText = "Directory containing the source files.")]
    public required string SourcesDir { get; init; }

    [Option('d', "dataset-dir", Required = true, HelpText = "Directory containing the dataset files.")]
    public required string DetectionsDatasetDir { get; init; }

    [Option('o', "output-dir", Required = true, HelpText = "Output directory to store assessment results.")]
    public required string OutputDir { get; init; }

    [Option("max-assessment-requests", Required = false, HelpText = "Maximum assessment requests from single detection result.")]
    public int MaximumAssessmentRequests { get; init; } = 10;

    [Option("context-padding-size", Required = false, HelpText = "Number of characters to include before and after each detection in the output context.")]
    public int ContextPaddingSize { get; init; } = 50;

    [Usage(ApplicationAlias = "profanity-detections-dataset-assessment")]
    public static IEnumerable<Example> Examples =>
        new List<Example>
        {
            new("Run assessment",
                new ApplicationOptions
                {
                    SourcesDir = "~/Sources",
                    DetectionsDatasetDir = "~/DetectionsDataset",
                    OutputDir = "~/AssessmentResults",
                    MaximumAssessmentRequests = 10,
                    ContextPaddingSize = 30
                }
            )
        };
}
