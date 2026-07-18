using DavidGroup.Content.AntiProfanity.DataSources.Implementations;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;

internal sealed class ProfanityDetectionTxtHandler(ProfanityTxtDataSource dataSource) : IProfanityDetectionHandler
{
    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
    {
        foreach (string profanity in dataSource.Profanities)
        {
            int searchStart = 0;
            while (searchStart <= context.Content.Length - profanity.Length)
            {
                int index = context.Content.IndexOf(profanity, searchStart, StringComparison.OrdinalIgnoreCase);
                if (index == -1)
                    break;

                searchStart = index + profanity.Length;

                context.Occurrences.Add(
                    new ProfanityOccurrence(profanity, index, index + profanity.Length - 1));
            }
        }

        next?.Invoke(context);
        return Task.CompletedTask;
    }
}
