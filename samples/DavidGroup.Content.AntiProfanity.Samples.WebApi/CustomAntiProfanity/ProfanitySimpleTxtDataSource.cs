using System.Collections.Frozen;

using DavidGroup.Content.AntiProfanity.DataSources;

namespace DavidGroup.Content.AntiProfanity.Samples.WebApi.CustomAntiProfanity;

public sealed class ProfanitySimpleTxtDataSource : IProfanityDataSource
{
    public FrozenSet<string> Profanities { get; private set; } = null!;

    public bool CanLoad(string extension) => extension == ".simpletxt";

    public Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(paths);

        string[] pathArray = paths as string[] ?? paths.ToArray();

        string[] missing = pathArray.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0)
            throw new FileNotFoundException($"The file(s) '{string.Join(", ", missing)}' were not found.");

        Profanities = pathArray
            .SelectMany(path => File.ReadLines(path).First().Split(";"))
            .Where(term => !string.IsNullOrWhiteSpace(term))
            .Select(term => term.Trim())
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        return Task.CompletedTask;
    }
}
