namespace DavidGroup.Content.AntiProfanity.Enums;

/// <summary>
/// Represents the severity level of detected profanity.
/// </summary>
public enum SeverityLevel
{
    /// <summary>
    /// Not specified.
    /// </summary>
    NotSpecified = 0,

    /// <summary>
    /// Mild profanity that is generally considered low impact.
    /// </summary>
    Mild = 1,

    /// <summary>
    /// Moderate profanity that may be inappropriate in some contexts.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// Strong profanity that is offensive and should typically be filtered.
    /// </summary>
    Strong = 3,

    /// <summary>
    /// Extremely offensive or abusive profanity requiring the strictest filtering.
    /// </summary>
    Severe = 4
}
