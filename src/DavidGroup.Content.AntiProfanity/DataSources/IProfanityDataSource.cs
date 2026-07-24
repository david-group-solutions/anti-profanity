namespace DavidGroup.Content.AntiProfanity.DataSources;

/// <summary>
/// Data source reader to be used later in handler.
/// </summary>
public interface IProfanityDataSource
{
    /// <summary>
    /// Checks if the current implementation is able to load data source.
    /// </summary>
    /// <param name="extension">The extensions of the file which must be loaded.</param>
    /// <returns><see langword="true"/> if can load, otherwise <see langword="false"/>.</returns>
    bool CanLoad(string extension);

    /// <summary>
    /// Loads the data source.
    /// </summary>
    /// <param name="paths">Paths where the data source files are located.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default);
}
