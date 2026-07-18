using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.Options;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DavidGroup.Content.AntiProfanity.Extensions;

public static class ServiceProviderExtensions
{
    public static async Task LoadAntiProfanityDataSourcesAsync(
        this IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        AntiProfanityOptions? options = services.GetRequiredService<IOptions<AntiProfanityOptions>>().Value;

        IEnumerable<IProfanityDataSource> dataSources
            = services.GetRequiredService<IEnumerable<IProfanityDataSource>>();

        await Parallel.ForEachAsync(options.DataSources, cancellationToken,
            async (fileName, ct) =>
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    options.DataSourcesBasePath,
                    fileName
                );

                string extension = Path.GetExtension(path);

                IProfanityDataSource dataSource = dataSources.FirstOrDefault(x => x.CanLoad(extension))
                                                  ?? throw new InvalidOperationException(
                                                      $"No loader registered for '{extension}'.");

                await dataSource.LoadAsync(path, ct);
            }
        );
    }
}
