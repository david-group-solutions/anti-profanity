namespace DavidGroup.Content.AntiProfanity.Tools.ConfirmedDetectionsDataSetGenerator.Models;

public class FileStatus
{
    public long Position { get; set; }
    public string LastIncompleteChunk { get; set; } = string.Empty;
}
