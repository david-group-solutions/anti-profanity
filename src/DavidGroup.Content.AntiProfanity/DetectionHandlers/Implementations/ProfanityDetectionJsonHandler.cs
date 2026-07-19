using System.Text.RegularExpressions;

using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;

internal sealed class ProfanityDetectionJsonHandler(IEnumerable<IProfanityDataSource> dataSources)
    : IProfanityDetectionHandler
{
    private readonly ProfanityJsonDataSource _dataSource = dataSources.OfType<ProfanityJsonDataSource>().Single();

    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
    {
        foreach (Profanity profanity in _dataSource.Profanities)
        {
            if (profanity.Severity < context.SeverityLevel)
                continue;

            foreach (ValueMatch match in profanity.MatchRegex.EnumerateMatches(context.Content))
            {
                bool isException = false;

                if (profanity.Exceptions.Count != 0)
                {
                    ReadOnlySpan<char> word = GetEnclosingWord(context.Content, match);

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
                        new ProfanityOccurrence(profanity.Id, match.Index, match.Index + match.Length - 1, profanity));
            }
        }

        next?.Invoke(context);
        return Task.CompletedTask;
    }

    private static ReadOnlySpan<char> GetEnclosingWord(string content, ValueMatch match)
    {
        int start = match.Index;
        while (start > 0 && (char.IsLetter(content[start - 1]) || char.IsDigit(content[start - 1])))
            start--;

        int end = match.Index + match.Length;
        while (end < content.Length && (char.IsLetter(content[end]) || char.IsDigit(content[end])))
            end++;

        return content.AsSpan(start, end - start);
    }
}
