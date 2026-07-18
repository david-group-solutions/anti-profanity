using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers;

public class ProfanityDetectionContext
{
    public string Content { get; init; } = string.Empty;

    public SeverityLevel SeverityLevel { get; init; }

    public List<ProfanityOccurrence> Occurrences { get; } = [];
}
