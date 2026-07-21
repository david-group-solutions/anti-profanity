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
public sealed class ProfanityDetectionJsonHandler(IEnumerable<IProfanityDataSource> dataSources)
    : IProfanityDetectionHandler
{
    private readonly ProfanityJsonDataSource _dataSource = dataSources.OfType<ProfanityJsonDataSource>().Single();

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

                    foreach (Regex exception in profanity.ExceptionRegexes)
                    {
                        if (exception.IsMatch(word))
                        {
                            isException = true;
                            break;
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
}
