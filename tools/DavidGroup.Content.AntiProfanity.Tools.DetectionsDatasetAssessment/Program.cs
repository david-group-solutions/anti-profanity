using CommandLine;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Options;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Stores;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

ParserResult<ApplicationOptions> result =
    Parser.Default.ParseArguments<ApplicationOptions>(args);

return await result.MapResult(
    async options =>
    {
        try
        {
            string resolveOutputDirectory = PathHelpers.ResolveHomeDirectory(options.OutputDir);

            AssessmentsStore assessmentsStore = new(resolveOutputDirectory);
            DatasetReader datasetReader = new(assessmentsStore);

            AssessmentsProcessor processor = new(datasetReader, assessmentsStore);

            await processor.RunAsync(options);

            return 0;
        }
        catch
        {
            return 1;
        }
    },
    _ => Task.FromResult(1)
);
