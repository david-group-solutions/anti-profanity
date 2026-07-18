using System.Collections.Frozen;

namespace DavidGroup.Content.AntiProfanity.DataSources.Implementations;

internal class ProfanityTxtDataSource : IProfanityDataSource
{
    public FrozenSet<string> Profanities { get; private set; } = null!;

    public bool CanLoad(string extension) => extension == ".txt";

    public Task LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"The file '{path}' was not found.");

        Profanities = File
            .ReadLines(path)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .OrderByDescending(line => line.Length)
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return Task.CompletedTask;
    }
}
