using System.Reflection;

using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Options;
using DavidGroup.Content.AntiProfanity.Pipeline;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Content.AntiProfanity.Extensions;

/// <summary>
/// Provides extension methods for registering AntiProfanity services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the AntiProfanity services, options, data sources, and detection pipeline.
    /// </summary>
    /// <param name="services">
    /// The service collection to add the AntiProfanity services to.
    /// </param>
    /// <param name="configuration">
    /// The application configuration containing the
    /// <see cref="AntiProfanityOptions"/> section.
    /// </param>
    /// <param name="assembly">
    /// An optional assembly to scan for implementations of
    /// <see cref="IProfanityDataSource"/> and
    /// <see cref="IProfanityDetectionHandler"/>.
    /// If <see langword="null"/>, only the AntiProfanity assembly is scanned.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that additional
    /// service registrations can be chained.
    /// </returns>
    public static IServiceCollection AddAntiProfanity(
        this IServiceCollection services,
        IConfiguration configuration,
        Assembly? assembly = null)
    {
        services.AddOptions<AntiProfanityOptions>()
            .Bind(configuration.GetSection(AntiProfanityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        List<Assembly> assemblies = [typeof(ServiceCollectionExtensions).Assembly];
        if (assembly is not null)
            assemblies.Add(assembly);

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<IProfanityDataSource>(), publicOnly: false)
            .As<IProfanityDataSource>()
            .WithSingletonLifetime());

        services.Scan(scan => scan
            .FromAssemblies(assemblies)
            .AddClasses(classes => classes.AssignableTo<IProfanityDetectionHandler>(), publicOnly: false)
            .As<IProfanityDetectionHandler>()
            .WithTransientLifetime());

        services.AddTransient<IProfanityDetectionPipeline, ProfanityDetectionPipeline>();
        services.AddTransient<IAntiProfanityService, AntiProfanityService>();

        return services;
    }
}
