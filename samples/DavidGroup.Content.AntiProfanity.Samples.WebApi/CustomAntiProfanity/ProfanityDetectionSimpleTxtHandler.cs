using DavidGroup.Content.AntiProfanity.DataSources;
using DavidGroup.Content.AntiProfanity.DetectionHandlers;
using DavidGroup.Content.AntiProfanity.Models;

namespace DavidGroup.Content.AntiProfanity.Samples.WebApi.CustomAntiProfanity;

public class ProfanityDetectionSimpleTxtHandler(IEnumerable<IProfanityDataSource> dataSources)
    : IProfanityDetectionHandler
{
    private readonly ProfanitySimpleTxtDataSource _dataSource = dataSources.OfType<ProfanitySimpleTxtDataSource>().Single();

    public Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next)
    {
        foreach (string token in Tokenize(context.Content))
            if (_dataSource.Profanities.Contains(token))
                context.Occurrences.Add(new ProfanityOccurrence(token, 0, context.Content.Length - 1));

        next?.Invoke(context);
        return Task.CompletedTask;
    }

    private static IEnumerable<string> Tokenize(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
