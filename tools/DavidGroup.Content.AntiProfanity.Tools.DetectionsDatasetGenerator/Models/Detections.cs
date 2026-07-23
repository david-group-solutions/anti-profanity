namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

public class Detections
{
    public Dictionary<string, Dictionary<string, Detection>> Profanities { get; set; } = [];
}

public class Detection
{
    public required List<long> AbsolutePositions { get; set; }
    public object? Metadata { get; set; }
}
