namespace DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

public class DetectionRow
{
    public required string FileName { get; init; }

    public required long Position { get; init; }

    public required int Length { get; init; }
}
