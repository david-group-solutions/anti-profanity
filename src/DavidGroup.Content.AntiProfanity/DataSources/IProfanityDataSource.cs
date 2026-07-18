namespace DavidGroup.Content.AntiProfanity.DataSources;

public interface IProfanityDataSource
{
    bool CanLoad(string extension);
    Task LoadAsync(string path, CancellationToken cancellationToken = default);
}
