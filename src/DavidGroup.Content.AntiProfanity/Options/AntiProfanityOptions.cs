using System.ComponentModel.DataAnnotations;

using DavidGroup.Content.AntiProfanity.Enums;

namespace DavidGroup.Content.AntiProfanity.Options;

public class AntiProfanityOptions
{
    public const string SectionName = "AntiProfanity";

    [Required]
    public string DataSourcesBasePath { get; init; } = null!;

    [Required]
    public string[] DataSources { get; init; } = [];
}
