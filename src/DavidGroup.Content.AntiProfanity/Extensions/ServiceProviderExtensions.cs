using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DavidGroup.Content.AntiProfanity.Extensions;

/// <summary>
/// Provides extension methods for initializing AntiProfanity services.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Loads all configured profanity data sources.
    /// </summary>
    /// <param name="serviceProvider">
    /// The service provider used to resolve the required services.
    /// </param>
    /// <param name="cancellationToken">
    /// A token that can be used to cancel the loading operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous loading operation.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no registered <see cref="IProfanityDataSource"/> supports
    /// the extension of a configured data source file.
    /// </exception>
    /// <exception cref="NotSupportedException">
    /// Thrown when a configured data source file does not have a valid file extension.
    /// </exception>
    public static async Task InitializeAntiProfanityDataSourcesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        AntiProfanityOptions options = serviceProvider.GetRequiredService<IOptions<AntiProfanityOptions>>().Value;

        IEnumerable<IProfanityDataSource> dataSources
            = serviceProvider.GetRequiredService<IEnumerable<IProfanityDataSource>>();

        IEnumerable<IGrouping<string?, string>> groupedByExtension = options.DataSources
            .Select(fileName => Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                options.DataSourcesBasePath,
                fileName
            ))
            .GroupBy(Path.GetExtension);

        await Parallel.ForEachAsync(groupedByExtension, cancellationToken,
            async (group, ct) =>
            {
                string? extension = group.Key;

                if (string.IsNullOrWhiteSpace(extension))
                {
                    throw new NotSupportedException(
                        $"The file '{group.First()}' does not have a supported extension.");
                }

                IProfanityDataSource dataSource = dataSources.FirstOrDefault(x => x.CanLoad(extension))
                                                  ?? throw new InvalidOperationException(
                                                      $"No loader registered for '{extension}'.");

                await dataSource.LoadAsync(group, ct);
            }
        );
    }
}
