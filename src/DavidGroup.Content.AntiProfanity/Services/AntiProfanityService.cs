using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Pipeline;

namespace DavidGroup.Content.AntiProfanity.Services;

public class AntiProfanityService(IProfanityDetectionPipeline pipeline) : IAntiProfanityService
{
    public async Task<ReadOnlyCollection<ProfanityOccurrence>> DetectAsync(
        string text,
        SeverityLevel severityLevel = SeverityLevel.NotSpecified)
    {
        ProfanityDetectionContext context = new()
        {
            Content = text,
            SeverityLevel = severityLevel
        };

        await pipeline.DetectAsync(context);

        return context.Occurrences.AsReadOnly();
    }

    public async Task<string> CensorAsync(
        string text,
        SeverityLevel severityLevel = SeverityLevel.NotSpecified,
        char censorCharacter = '*')
    {
        IReadOnlyList<ProfanityOccurrence> occurrences = await DetectAsync(text, severityLevel);

        return occurrences.Aggregate(text, (current, detection)
            => current[..detection.StartIndex] +
               Enumerable.Repeat(censorCharacter, detection.EndIndex - detection.StartIndex) +
               current[detection.EndIndex..]
        );
    }
}
