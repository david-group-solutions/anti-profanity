namespace DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

public sealed class DetectionRow
{
    public required string File { get; init; }

    public required long Position { get; init; }

    public required int Length { get; init; }
}
