using System.ComponentModel.DataAnnotations;

namespace DavidGroup.Content.AntiProfanity.Options;

/// <summary>
/// Represents the configuration options for the AntiProfanity library.
/// </summary>
public class AntiProfanityOptions
{
    /// <summary>
    /// The name of the configuration section containing the AntiProfanity settings.
    /// </summary>
    public const string SectionName = "AntiProfanity";

    /// <summary>
    /// Gets the base directory containing the configured profanity data source files.
    /// </summary>
    /// <remarks>
    /// This path is resolved relative to the application's base directory.
    /// </remarks>
    [Required]
    public string DataSourcesBasePath { get; init; } = null!;

    /// <summary>
    /// Gets the collection of profanity data source file names to load.
    /// </summary>
    /// <remarks>
    /// Each file is resolved relative to <see cref="DataSourcesBasePath"/>.
    /// </remarks>
    [Required]
    public string[] DataSources { get; init; } = [];
}
