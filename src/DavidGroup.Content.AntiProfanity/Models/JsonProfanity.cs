using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

using DavidGroup.Content.AntiProfanity.Enums;

namespace DavidGroup.Content.AntiProfanity.Models;

/// <summary>
/// Model representing JSON dataset entity.
/// </summary>
public class JsonProfanity
{
    /// <summary>
    /// Unique identifier.
    /// /// </summary>
    public string Id { get; init; } = null!;

    /// <summary>
    /// Expression containing '|' separated values with '*' indicating any number of characters in that place.
    /// </summary>
    public string Match { get; init; } = null!;

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

    /// <summary>
    /// Complied <see cref="Match"/> pattern.
    /// </summary>
    [JsonIgnore]
    public Regex MatchRegex { get; private set; } = null!;

    /// <summary>
    /// Complied <see cref="ExceptionRegexes"/> patterns.
    /// </summary>
    [JsonIgnore]
    public List<Regex> ExceptionRegexes { get; } = [];

    /// <summary>
    /// Pre-complies resources once when loading data source.
    /// </summary>
    public void PrecompileValues()
    {
        string pattern = string.Concat(@"\b(", Match.Replace("*", "+"), @")\b");

        MatchRegex = new Regex(pattern,
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant
        );

        foreach (string exceptionPattern in Exceptions.Select(exception => string.Concat(
                     "^", Regex.Escape(exception).Replace(@"\*", @"\w*"), "$")))
        {
            ExceptionRegexes.Add(
                new Regex(exceptionPattern,
                    RegexOptions.Compiled |
                    RegexOptions.IgnoreCase |
                    RegexOptions.CultureInvariant
                )
            );
        }
    }
}
