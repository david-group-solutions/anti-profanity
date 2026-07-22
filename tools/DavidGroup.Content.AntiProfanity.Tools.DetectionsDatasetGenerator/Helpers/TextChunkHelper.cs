using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;

/// <summary>
/// Helpers for extracting readable context around a detected profanity occurrence.
/// </summary>
public static class TextChunkHelper
{
    /// <summary>
    /// Returns the start and end indices of a text chunk centered on the occurrence, padded with
    /// up to <paramref name="additionalSize"/> characters on each side (borrowing extra from
    /// the other side when one side runs out of room).
    /// </summary>
    public static (int Start, int End) GetSmallChunkBoundaries(ReadOnlySpan<char> content, ProfanityOccurrence occurrence, int additionalSize = 30)
    {
        int before = Math.Min(additionalSize, occurrence.Index);
        int after = Math.Min(additionalSize, content.Length - (occurrence.Index + occurrence.Length));

        if (before < additionalSize)
        {
            after = Math.Min(
                content.Length - (occurrence.Index + occurrence.Length),
                after + (additionalSize - before));
        }

        if (after < additionalSize)
        {
            before = Math.Min(
                occurrence.Index,
                before + (additionalSize - after));
        }

        int start = occurrence.Index - before;
        int end = occurrence.Index + occurrence.Length + after;

        return (start, end);
    }
}
