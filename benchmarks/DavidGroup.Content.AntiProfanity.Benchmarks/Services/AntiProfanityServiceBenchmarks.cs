using System.Collections.ObjectModel;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Order;

using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Content.AntiProfanity.Benchmarks.Services;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[Orderer(SummaryOrderPolicy.Declared)]
public class AntiProfanityServiceBenchmarks
{
    private const string CleanText =
        "It was a calm afternoon, and everyone in the office was focused on finishing their work before the end of the day. The team discussed new ideas, reviewed documents, answered emails, and prepared plans for the upcoming project. Several people gathered in the meeting room to share feedback, while others continued writing code, testing features, and fixing small issues that had been discovered earlier. After lunch, the atmosphere remained productive as conversations shifted toward future improvements, customer satisfaction, application performance, and long-term goals. Although a few unexpected challenges appeared during development, the team collaborated effectively, solved each problem carefully, and documented the results for future reference. By the evening, everyone was satisfied with the progress that had been made and looked forward to continuing the work the next day with fresh ideas, renewed energy, and a clear understanding of the remaining tasks.";

    private const string DirtyText =
        "It was a damn chaotic afternoon, and everyone in the office was fucking frustrated with finishing their shitty work before the end of the damn day. The team discussed bullshit ideas, reviewed damn documents, answered emails, and prepared fucked-up plans for the upcoming project. Several assholes gathered in the meeting room to share bullshit feedback, while others continued writing code, testing shitty features, and fixing damn issues that had been discovered earlier. After lunch, the atmosphere remained fucking tense as conversations shifted toward more bullshit complaints, customer frustration, application failures, and long-term damn problems. Although a few unexpected bullshit challenges appeared during development, the team argued constantly, solved each damn problem reluctantly, and documented the shitty results for future reference. By the evening, everyone was completely pissed with the fucking progress that had been made and looked forward to finishing the damn work.";

    private ServiceProvider _serviceProvider = null!;
    private IAntiProfanityService _service = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);

        services.AddAntiProfanity(configuration);

        _serviceProvider = services.BuildServiceProvider();

        _serviceProvider
            .InitializeAntiProfanityDataSourcesAsync()
            .GetAwaiter()
            .GetResult();

        _service = _serviceProvider.GetRequiredService<IAntiProfanityService>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _serviceProvider.Dispose();
    }

    // ------------------------------------------------------------------
    // Detect
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Detect")]
    public Task<ReadOnlyCollection<ProfanityOccurrence>> AntiProfanityService_Detect_CleanText()
        => _service.DetectAsync(CleanText);

    [Benchmark]
    [BenchmarkCategory("Detect")]
    public Task<ReadOnlyCollection<ProfanityOccurrence>> AntiProfanityService_Detect_DirtyText()
        => _service.DetectAsync(DirtyText);

    // ------------------------------------------------------------------
    // Censor
    // ------------------------------------------------------------------

    [Benchmark(Baseline = true)]
    [BenchmarkCategory("Censor")]
    public Task<string> AntiProfanityService_Censor_CleanText()
        => _service.CensorAsync(CleanText);

    [Benchmark]
    [BenchmarkCategory("Censor")]
    public Task<string> AntiProfanityService_Censor_DirtyText()
        => _service.CensorAsync(DirtyText);
}
