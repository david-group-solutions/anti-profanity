using System.Text.RegularExpressions;

using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;

/// <summary>
/// A profanity detection handler that detects profanities defined in a
/// <see cref="ProfanityJsonDataSource"/> using regular expressions.
/// </summary>
/// <param name="dataSources">
/// The collection of registered profanity data sources.
/// </param>
public sealed partial class ProfanityDetectionJsonHandler(IEnumerable<IProfanityDataSource> dataSources)
    : IProfanityDetectionHandler
{
    private readonly ProfanityJsonDataSource _dataSource = dataSources.OfType<ProfanityJsonDataSource>().Single();

    /// <summary>
    /// Matches contiguous word characters; used to find the words surrounding a
    /// profanity match when checking multi-word exceptions.
    /// </summary>
    [GeneratedRegex(@"\w+", RegexOptions.Compiled | RegexOptions.CultureInvariant)]
    private static partial Regex WordRegex();

    /// <summary>
    /// Detects profanities in the specified text using the configured JSON data source
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
        List<(int Index, int Length)> words = [];

        foreach (ValueMatch wordMatch in WordRegex().EnumerateMatches(context.Content))
            words.Add((wordMatch.Index, wordMatch.Length));

        foreach (JsonProfanity profanity in _dataSource.Profanities)
        {
            if (profanity.Severity < context.SeverityLevel)
                continue;

            foreach (ValueMatch match in profanity.MatchRegex.EnumerateMatches(context.Content))
            {
                bool isException = false;

                if (profanity.Exceptions.Count != 0)
                {
                    ReadOnlySpan<char> word = context.Content.AsSpan(match.Index, match.Length);

                    for (int i = 0; i < profanity.ExceptionRegexes.Count; i++)
                    {
                        int extraWordsCount = profanity.Exceptions[i].Count(x => x == ' ');
                        if (extraWordsCount == 0)
                        {
                            if (profanity.ExceptionRegexes[i].IsMatch(word))
                            {
                                isException = true;
                                break;
                            }

                            continue;
                        }

                        {
                            bool isMultiWordException = false;

                            for (int wordsBefore = 0; wordsBefore <= extraWordsCount; wordsBefore++)
                            {
                                int wordsAfter = extraWordsCount - wordsBefore;

                                if (TryGetNeighbourPhrase(
                                        context.Content, words, match.Index, match.Length, wordsBefore, wordsAfter,
                                        out ReadOnlySpan<char> phrase) &&
                                    profanity.ExceptionRegexes[i].IsMatch(phrase))
                                {
                                    isMultiWordException = true;
                                    break;
                                }
                            }

                            if (isMultiWordException)
                            {
                                isException = true;
                                break;
                            }
                        }
                    }
                }

                if (!isException)
                    context.Occurrences.Add(
                        new ProfanityOccurrence(profanity.Id, match.Index, match.Length, profanity));
            }
        }

        return next.Invoke(context);
    }

    /// <summary>
    /// Attempts to build the span covering the matched word plus the requested number of
    /// neighboring words immediately before/after it, using pre-computed word boundaries.
    /// </summary>
    private static bool TryGetNeighbourPhrase(
        string content,
        List<(int Index, int Length)> words,
        int matchIndex,
        int matchLength,
        int wordsBefore,
        int wordsAfter,
        out ReadOnlySpan<char> phrase)
    {
        int centerIndex = words.FindIndex(w => w.Index == matchIndex && w.Length == matchLength);

        if (centerIndex == -1)
        {
            phrase = default;
            return false;
        }

        int startIndex = centerIndex - wordsBefore;
        int endIndex = centerIndex + wordsAfter;

        if (startIndex < 0 || endIndex >= words.Count)
        {
            phrase = default;
            return false;
        }

        int start = words[startIndex].Index;
        int end = words[endIndex].Index + words[endIndex].Length;

        phrase = content.AsSpan(start, end - start);
        return true;
    }
}
