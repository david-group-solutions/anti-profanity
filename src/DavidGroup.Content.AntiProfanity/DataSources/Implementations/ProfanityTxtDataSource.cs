using System.Collections.Frozen;

namespace DavidGroup.Content.AntiProfanity.DataSources.Implementations;

internal sealed class ProfanityTxtDataSource : IProfanityDataSource
{
    public FrozenSet<string> Profanities { get; private set; } = null!;

    public bool CanLoad(string extension) => extension == ".txt";

    public Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        string[] pathArray = paths as string[] ?? paths.ToArray();

        string[] missing = pathArray.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0)
            throw new FileNotFoundException($"The file(s) '{string.Join(", ", missing)}' were not found.");

        Profanities = pathArray
            .SelectMany(File.ReadLines)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Select(line => line.Trim())
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return Task.CompletedTask;
    }
}
