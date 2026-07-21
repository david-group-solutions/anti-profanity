using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;

/// <summary>
/// A profanity detection handler that detects plain-text profanities loaded
/// from a <see cref="ProfanityTxtDataSource"/>.
/// </summary>
/// <param name="dataSources">
/// The collection of registered profanity data sources.
/// </param>
public sealed class ProfanityDetectionTxtHandler(IEnumerable<IProfanityDataSource> dataSources)
    : IProfanityDetectionHandler
{
    private readonly ProfanityTxtDataSource _dataSource = dataSources.OfType<ProfanityTxtDataSource>().Single();

    /// <summary>
    /// Detects profanities in the specified text using the configured text data source
    /// and appends all detected occurrences to the provided context.
    /// </summary>
    /// <param name="context">
    /// The context containing the input and state used during profanity detection.
    /// </param>
    /// <param name="next">
    /// The delegate that invokes the next handler in the pipeline.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous detection operation.
    /// </returns>
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate next)
    {
        ReadOnlySpan<char> text = context.Content.AsSpan();

        foreach (string profanity in _dataSource.Profanities.OrderByDescending(x => x.Length))
        {
            int searchStart = 0;
            while (searchStart <= context.Content.Length - profanity.Length)
            {
                int index = context.Content.IndexOf(profanity, searchStart, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    break;

                searchStart = index + profanity.Length;

                if (!IsWholeWord(text, index, profanity.Length))
                    continue;

                context.Occurrences.Add(
                    new ProfanityOccurrence(profanity, index, profanity.Length));
            }
        }

        return next.Invoke(context);
    }

    /// <summary>
    /// Determines whether the specified match represents a whole word.
    /// </summary>
    /// <param name="text">The text being analyzed.</param>
    /// <param name="index">The zero-based starting index of the match.</param>
    /// <param name="length">The length of the matched word.</param>
    /// <returns>
    /// <see langword="true"/> if the match is surrounded by non-alphanumeric
    /// characters or text boundaries; otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsWholeWord(ReadOnlySpan<char> text, int index, int length)
    {
        bool startBoundary = index == 0 ||
                             !char.IsLetterOrDigit(text[index - 1]);

        bool endBoundary = index + length == text.Length ||
                           !char.IsLetterOrDigit(text[index + length]);

        return startBoundary && endBoundary;
    }
}
