using System.Collections.Frozen;
using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DataSources.Implementations;

internal sealed class ProfanityJsonDataSource : IProfanityDataSource
{
    public FrozenSet<JsonProfanity> Profanities { get; private set; } = null!;

    public bool CanLoad(string extension) => extension == ".json";

    public async Task LoadAsync(IEnumerable<string> paths, CancellationToken cancellationToken = default)
    {
        string[] pathArray = paths as string[] ?? paths.ToArray();

        string[] missing = pathArray.Where(p => !File.Exists(p)).ToArray();
        if (missing.Length > 0)
            throw new FileNotFoundException($"The file(s) '{string.Join(", ", missing)}' were not found.");

        List<JsonProfanity> profanities = [];
        foreach (string path in pathArray)
        {
            await using FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);

            try
            {
                await foreach (JsonProfanity? profanity in JsonSerializer
                                   .DeserializeAsyncEnumerable<JsonProfanity>(fs, JsonSerializerOptions, cancellationToken)
                                   .ConfigureAwait(false))
                {
                    if (profanity is not null)
                    {
                        profanity.PrecompileValues();
                        profanities.Add(profanity);
                    }
                }
            }
            catch (JsonException ex)
            {
                throw new InvalidDataException($"Failed to parse profanity data from '{path}'.", ex);
            }
        }

        Profanities = profanities.ToFrozenSet();
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
