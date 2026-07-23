using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services.Stores;

/// <summary>
/// Loads and persists the scan <see cref="State"/> to disk as JSON, so a run can resume
/// where it left off.
/// </summary>
public class StateStore(string stateFilePath, JsonSerializerOptions jsonOptions)
{
    public async Task<State> LoadAsync()
    {
        if (!File.Exists(stateFilePath))
            return new State();

        await using FileStream stream = new(stateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        stream.Seek(0, SeekOrigin.Begin);

        return await JsonSerializer.DeserializeAsync<State>(stream, jsonOptions)
               ?? throw new NullReferenceException($"Failed to deserialize state from file {stateFilePath}.");
    }

    public async Task SaveAsync(State state)
    {
        await using FileStream stream = new(stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, state, jsonOptions);
    }
}

