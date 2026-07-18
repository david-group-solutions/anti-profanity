using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Options;
using DavidGroup.Content.AntiProfanity.Pipeline;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DavidGroup.Content.AntiProfanity.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAntiProfanity(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<AntiProfanityOptions>()
            .Bind(configuration.GetSection(AntiProfanityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<IProfanityDetectionPipeline, ProfanityDetectionPipeline>();
        services.AddTransient<IAntiProfanityService, AntiProfanityService>();

        return services;
    }

    public static IServiceCollection AddAntiProfanityDataSource<TSource>(
        this IServiceCollection services)
        where TSource : class, IProfanityDataSource
    {
        services.AddSingleton<IProfanityDataSource, TSource>();

        return services;
    }

    public static IServiceCollection AddAntiProfanityHandler<THandler>(
        this IServiceCollection services)
        where THandler : class, IProfanityDetectionHandler
    {
        services.AddTransient<IProfanityDetectionHandler, THandler>();

        return services;
    }
}
