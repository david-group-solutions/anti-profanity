using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services.Stores;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.UI;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

ServiceCollection services = new();
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .Build();

string[] files = configuration.GetSection("Input").Get<string[]>()
                 ?? throw new Exception("Input files config not found.");

services.AddAntiProfanity(configuration)
    .AddHandler<ProfanityDetectionJsonHandler>()
    .AddHandler<ProfanityDetectionTxtHandler>();

IServiceProvider serviceProvider = services.BuildServiceProvider();
await serviceProvider.InitializeAntiProfanityDataSourcesAsync();

string projectDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
string inputDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Input");
string outputDirectory = Path.Combine(projectDirectory, "Output");
string detectionsDirectory = Path.Combine(outputDirectory, "Detections");

Directory.CreateDirectory(outputDirectory);
Directory.CreateDirectory(detectionsDirectory);

string stateFilePath = Path.Combine(outputDirectory, "state.json");

IAntiProfanityService antiProfanityService = serviceProvider.GetRequiredService<IAntiProfanityService>();
DetectionsStore detectionsStore = new(detectionsDirectory, ToolJsonOptions.WriteIntendedJsonOptions);
StateStore stateStore = new(stateFilePath, ToolJsonOptions.WriteIntendedJsonOptions);
ProfanityScanner scanner = new(antiProfanityService, detectionsStore, stateStore);

ConsoleHelpers.TryClearConsole();
int degreeOfParallelism = CommandLineArgsParser.ParseDegreeOfParallelism(args);

CancellationTokenSource cts = new();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();

    Console.WriteLine("Cancellation requested...");
};

try
{
    await scanner.RunAsync(inputDirectory, files, degreeOfParallelism, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Operation cancelled.");
}
catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
{
    Console.WriteLine("Operation cancelled.");
}
