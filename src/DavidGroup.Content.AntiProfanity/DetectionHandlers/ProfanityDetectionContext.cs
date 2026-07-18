using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers;

/// <summary>
/// Represents the context shared between handlers during profanity detection.
/// </summary>
public class ProfanityDetectionContext
{
    /// <summary>
    /// Gets the text being analyzed for profanities.
    /// </summary>
    public string Content { get; init; } = string.Empty;

    /// <summary>
    /// Gets the minimum profanity severity level to detect.
    /// </summary>
    public ProfanitySeverityLevel SeverityLevel { get; init; }

    /// <summary>
    /// Gets the collection of detected profanity occurrences.
    /// </summary>
    /// <remarks>
    /// Detection handlers add matches to this collection as the pipeline executes.
    /// </remarks>
    public List<ProfanityOccurrence> Occurrences { get; } = [];
}
