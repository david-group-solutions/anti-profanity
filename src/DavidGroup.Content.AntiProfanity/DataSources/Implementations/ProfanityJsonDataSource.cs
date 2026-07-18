using System.Collections.Frozen;
using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DataSources.Implementations;

internal class ProfanityJsonDataSource : IProfanityDataSource
{
    public FrozenSet<Profanity> Profanities { get; private set; } = null!;

    public bool CanLoad(string extension) => extension == ".json";

    public async Task LoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"The file '{path}' was not found.");

        await using FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        List<Profanity> profanities = [];
        try
        {
            await foreach (Profanity? profanity in JsonSerializer
                               .DeserializeAsyncEnumerable<Profanity>(fs, JsonSerializerOptions, cancellationToken)
                               .ConfigureAwait(false))
            {
                if (profanity is not null)
                    profanities.Add(profanity);
            }
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Failed to parse profanity data from '{path}'.", ex);
        }

        Profanities = profanities.ToFrozenSet();
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
