using DavidGroup.Content.AntiProfanity.DetectionHandlers;

namespace DavidGroup.Content.AntiProfanity.Pipeline;

public class ProfanityDetectionPipeline(IEnumerable<IProfanityDetectionHandler> handlers)
    : IProfanityDetectionPipeline
{
    public async Task DetectAsync(ProfanityDetectionContext context)
    {
        NextProfanityDetectionHandlerDelegate pipeline = _ => Task.CompletedTask;

        foreach (IProfanityDetectionHandler handler in handlers.Reverse())
        {
            NextProfanityDetectionHandlerDelegate next = pipeline;
            pipeline = ctx => handler.DetectAsync(ctx, next);
        }

        await pipeline(context);
    }
}
