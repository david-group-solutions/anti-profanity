using CommandLine;

using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Options;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Stores;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

services.AddAntiProfanity(configuration)
    .AddHandler<ProfanityDetectionJsonHandler>()
    .AddHandler<ProfanityDetectionTxtHandler>();

IServiceProvider serviceProvider = services.BuildServiceProvider();

await serviceProvider.InitializeAntiProfanityDataSourcesAsync();

CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();

    Console.WriteLine("Cancellation requested...");
};

ParserResult<ApplicationOptions> result =
    Parser.Default.ParseArguments<ApplicationOptions>(args);

return await result.MapResult(
    async options =>
    {
        try
        {
            string resolveOutputDirectory = PathHelpers.ResolveHomeDirectory(options.OutputDir);

            IAntiProfanityService antiProfanityService = serviceProvider.GetRequiredService<IAntiProfanityService>();

            DetectionsStore detectionsStore = new(resolveOutputDirectory);
            StateStore stateStore = new(resolveOutputDirectory, ToolJsonOptions.WriteIndentedJsonOptions);

            ProfanityScanner scanner = new(antiProfanityService, detectionsStore, stateStore);

            await scanner.RunAsync(options, cts.Token);

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation cancelled.");
            return 1;
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
        {
            Console.WriteLine("Operation cancelled.");
            return 1;
        }
    },
    _ => Task.FromResult(1)
);
