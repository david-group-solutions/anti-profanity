namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

public class State
{
    public Dictionary<string, FileStatus> Statuses { get; set; } = [];
}

public class FileStatus
{
    public long Position { get; set; }
    public string LastIncompleteChunk { get; set; } = string.Empty;
}
