using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Enums;
using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Pipelines;

namespace DavidGroup.Content.AntiProfanity.Services;

/// <summary>
/// Default implementation of <see cref="IAntiProfanityService"/>.
/// </summary>
/// <param name="pipeline">
/// The profanity detection pipeline used to analyze text.
/// </param>
public class AntiProfanityService(IProfanityDetectionPipeline pipeline) : IAntiProfanityService
{
    /// <inheritdoc />
    public async Task<ReadOnlyCollection<ProfanityOccurrence>> DetectAsync(
        string text,
        ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.NotSpecified)
    {
        ProfanityDetectionContext context = new()
        {
            Content = text,
            SeverityLevel = severityLevel
        };

        await pipeline.DetectAsync(context);

        return context.Occurrences.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<string> CensorAsync(
        string text,
        ProfanitySeverityLevel severityLevel = ProfanitySeverityLevel.NotSpecified,
        char censorCharacter = '*')
    {
        ReadOnlyCollection<ProfanityOccurrence> occurrences = await DetectAsync(text, severityLevel);

        return string.Create(text.Length, (text, occurrences, censorCharacter), (span, state) =>
        {
            state.text.AsSpan().CopyTo(span);

            foreach (ProfanityOccurrence occurrence in state.occurrences)
            {
                span.Slice(occurrence.Index, occurrence.Length)
                    .Fill(state.censorCharacter);
            }
        });
    }
}
