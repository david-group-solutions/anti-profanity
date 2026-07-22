using System.Text.Json;

using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Services;
using DavidGroup.Content.AntiProfanity.Tools.ConfirmedDetectionsDataSetGenerator.Services;

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

Directory.CreateDirectory(outputDirectory);

string confirmedDetectionsFilePath = Path.Combine(outputDirectory, "confirmed_detections.txt");
string wrongDetectionsFilePath = Path.Combine(outputDirectory, "wrong_detections.txt");
string stateFilePath = Path.Combine(outputDirectory, "state.txt");

await using FileStream confirmedDetectionsFileStream =
    new(confirmedDetectionsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
confirmedDetectionsFileStream.Seek(0, SeekOrigin.End);

await using FileStream wrongDetectionsFileStream =
    new(wrongDetectionsFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
wrongDetectionsFileStream.Seek(0, SeekOrigin.End);

IAntiProfanityService antiProfanityService = serviceProvider.GetRequiredService<IAntiProfanityService>();
StateStore stateStore = new(stateFilePath, new JsonSerializerOptions { WriteIndented = true });
ProfanityScanner scanner = new(antiProfanityService, stateStore);

await scanner.RunAsync(inputDirectory, files, confirmedDetectionsFileStream, wrongDetectionsFileStream);
