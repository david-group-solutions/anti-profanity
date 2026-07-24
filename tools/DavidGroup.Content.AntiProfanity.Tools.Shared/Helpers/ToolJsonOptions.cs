using System.Text.Json;

namespace DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;

public static class ToolJsonOptions
{
    public static readonly JsonSerializerOptions WriteIndentedJsonOptions = new() { WriteIndented = true };
}
