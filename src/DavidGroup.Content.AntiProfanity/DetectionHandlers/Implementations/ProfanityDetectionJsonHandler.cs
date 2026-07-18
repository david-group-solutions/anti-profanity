using System.Text.RegularExpressions;

using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;

internal sealed class ProfanityDetectionJsonHandler(ProfanityJsonDataSource dataSource)
    : IProfanityDetectionHandler
{
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
    {
        foreach (Profanity profanity in dataSource.Profanities)
        {
            if (profanity.Severity < context.SeverityLevel)
                continue;

            string pattern = profanity.Match.Replace("*", "+");
            if (!profanity.PartialMatch)
                pattern = string.Concat(@"\b(", pattern, @")\b");

            MatchCollection matches = Regex.Matches(context.Content, pattern, RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                bool isException = false;

                string word = GetEnclosingWord(context.Content, match);

                foreach (string exception in profanity.Exceptions)
                {
                    string exceptionPattern = string.Concat(
                        "^", Regex.Escape(exception).Replace(@"\*", @"\w*"), "$");

                    if (Regex.IsMatch(word, exceptionPattern, RegexOptions.IgnoreCase))
                    {
                        isException = true;
                        break;
                    }
                }

                if (!isException)
                    context.Occurrences.Add(
                        new ProfanityOccurrence(profanity.Id, match.Index, match.Index + match.Length - 1));
            }
        }

        next?.Invoke(context);
        return Task.CompletedTask;
    }

    private static string GetEnclosingWord(string content, Match match)
    {
        int start = match.Index;
        while (start > 0 && (char.IsLetter(content[start - 1]) || char.IsDigit(content[start - 1])))
            start--;

        int end = match.Index + match.Length;
        while (end < content.Length && (char.IsLetter(content[end]) || char.IsDigit(content[end])))
            end++;

        return content.Substring(start, end - start);
    }
}
