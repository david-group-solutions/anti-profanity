using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DataSetGenerator.Helpers;

/// <summary>
/// Helpers for extracting readable context around a detected profanity occurrence.
/// </summary>
public static class TextChunkHelper
{
    /// <summary>
    /// Expands a detection to the full word that contains it (letters/digits on both sides).
    /// </summary>
    public static ReadOnlySpan<char> GetEnclosingWord(ReadOnlySpan<char> content, ProfanityOccurrence occurrence)
    {
        int start = occurrence.Index;
        while (start > 0 && char.IsLetterOrDigit(content[start - 1]))
            start--;

        int end = occurrence.Index + occurrence.Length;
        while (end < content.Length && char.IsLetterOrDigit(content[end]))
            end++;

        return content.Slice(start, end - start);
    }

    /// <summary>
    /// Returns a snippet of <paramref name="content"/> centered on the occurrence, padded with
    /// up to <paramref name="additionalSize"/> characters on each side (borrowing extra from
    /// the other side when one side runs out of room).
    /// </summary>
    public static ReadOnlySpan<char> GetSmallChunk(ReadOnlySpan<char> content, ProfanityOccurrence occurrence, int additionalSize = 30)
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

        return content.Slice(start, end - start);
    }
}
