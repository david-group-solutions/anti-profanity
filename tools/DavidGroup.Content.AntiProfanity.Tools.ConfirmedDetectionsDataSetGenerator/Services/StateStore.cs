using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.ConfirmedDetectionsDataSetGenerator.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.ConfirmedDetectionsDataSetGenerator.Services;

/// <summary>
/// Loads and persists the scan <see cref="State"/> to disk as JSON, so a run can resume
/// where it left off.
/// </summary>
public class StateStore(string stateFilePath, JsonSerializerOptions jsonOptions)
{
    public State Load()
    {
        State? state = File.Exists(stateFilePath)
            ? JsonSerializer.Deserialize<State>(File.ReadAllText(stateFilePath), jsonOptions)
            : null;

        return state ?? new State();
    }

    public async Task SaveAsync(State state)
    {
        string serialized = JsonSerializer.Serialize(state, jsonOptions);
        await File.WriteAllTextAsync(stateFilePath, serialized);
    }
}
