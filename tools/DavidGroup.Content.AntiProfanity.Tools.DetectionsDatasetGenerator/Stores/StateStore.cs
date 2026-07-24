using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Stores;

/// <summary>
/// Loads and persists the scan <see cref="State"/> to disk as JSON, so a run can resume
/// where it left off.
/// </summary>
public class StateStore(string outputDirectory, JsonSerializerOptions jsonOptions)
{
    public string StateFilePath { get; } = Path.Combine(outputDirectory, "state.json");

    public async Task<State> LoadAsync(bool resetState)
    {
        if (!File.Exists(StateFilePath))
            return new State();

        if (resetState)
        {
            File.Delete(StateFilePath);
            return new State();
        }

        await using FileStream stream = new(StateFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return await JsonSerializer.DeserializeAsync<State>(stream, jsonOptions)
               ?? throw new NullReferenceException($"Failed to deserialize state from file {StateFilePath}.");
    }

    public async Task SaveAsync(State state)
    {
        await using FileStream stream = new(StateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(stream, state, jsonOptions);
    }
}
