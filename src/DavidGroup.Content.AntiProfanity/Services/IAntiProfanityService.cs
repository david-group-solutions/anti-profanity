using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Services;

/// <summary>
/// Provides methods for detecting and censoring profanities in text.
/// </summary>
public interface IAntiProfanityService
{
    /// <summary>
    /// Detects all profanities in the specified text.
    /// </summary>
    /// <param name="text">
    /// The text to analyze.
    /// </param>
    /// <param name="severityLevel">
    /// The minimum profanity severity level to detect.
    /// </param>
    /// <returns>
    /// A read-only collection containing the detected profanity occurrences.
    /// </returns>
    Task<ReadOnlyCollection<ProfanityOccurrence>> DetectAsync(
        string text,
        ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.NotSpecified);

    /// <summary>
    /// Replaces detected profanities in the specified text with the provided censor character.
    /// </summary>
    /// <param name="text">
    /// The text to censor.
    /// </param>
    /// <param name="severityLevel">
    /// The minimum profanity severity level to censor.
    /// </param>
    /// <param name="censorCharacter">
    /// The character used to replace each character of a detected profanity.
    /// </param>
    /// <returns>
    /// The censored text.
    /// </returns>
    Task<string> CensorAsync(
        string text,
        ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.NotSpecified,
        char censorCharacter = '*');
}
