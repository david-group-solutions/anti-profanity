using DavidGroup.Content.AntiProfanity.Enums;

namespace DavidGroup.Content.AntiProfanity.Models;

/// <summary>
/// Model representing JSON dataset entity.
/// </summary>
public class Profanity
{
    /// <summary>
    /// Unique identifier.
    /// /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    /// Expression containing '|' separated values with '*' indicating any number of characters in that place.
    /// </summary>
    public string Match { get; init; } = null!;

    /// Indicates whether partial matching is enabled.
    /// If <see langword="false"/>, only whole words are matched.
    /// If <see langword="true"/>, the <see cref="Match"/> value can match anywhere within a word.
    public bool PartialMatch { get; init; } = true;

    /// <summary>
    /// Severity level. See <see cref="ProfanitySeverityLevel"/> enum for more information.
    /// </summary>
    public ProfanitySeverityLevel Severity { get; init; }

    /// <summary>
    /// Associated tags representing the category.
    /// </summary>
    public List<string> Tags { get; init; } = [];

    /// <summary>
    /// Any exceptions specified in the same format as <see cref="Match"/> will be excluded from result.
    /// </summary>
    public List<string> Exceptions { get; init; } = [];
}
