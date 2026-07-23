using System.Text.Json;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;

public static class ToolJsonOptions
{
    public static readonly JsonSerializerOptions WriteIntendedJsonOptions = new() { WriteIndented = true };
}
