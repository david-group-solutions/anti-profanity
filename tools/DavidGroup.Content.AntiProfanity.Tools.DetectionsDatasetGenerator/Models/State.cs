using System.Collections.Concurrent;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

public class State
{
    // Key = file name
    public ConcurrentDictionary<string, FileStatus> Statuses { get; init; } = [];

    public FileStatus GetFileStatus(string file) =>
        Statuses.GetOrAdd(file, _ => new FileStatus());
}

public class FileStatus
{
    public long Position { get; set; }
    public string LastIncompleteChunk { get; set; } = string.Empty;
}
