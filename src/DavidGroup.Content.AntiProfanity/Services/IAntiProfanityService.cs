using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Services;

public interface IAntiProfanityService
{
    Task<ReadOnlyCollection<ProfanityOccurrence>> DetectAsync(
        string text,
        SeverityLevel severityLevel = SeverityLevel.NotSpecified);

    Task<string> CensorAsync(
        string text,
        SeverityLevel severityLevel = SeverityLevel.NotSpecified,
        char censorCharacter = '*');
}
