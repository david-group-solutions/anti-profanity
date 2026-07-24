using DavidGroup.Content.AntiProfanity.DetectionHandlers;

namespace DavidGroup.Content.AntiProfanity.Pipelines;

/// <summary>
/// Represents the default implementation of <see cref="IProfanityDetectionPipeline"/>.
/// </summary>
/// <param name="handlers">
/// The collection of profanity detection handlers that make up the pipeline.
/// Handlers are executed in the order they are registered.
/// </param>
internal sealed class ProfanityDetectionPipeline(IEnumerable<IProfanityDetectionHandler> handlers)
    : IProfanityDetectionPipeline
{
    private readonly IProfanityDetectionHandler[] _handlers = handlers.ToArray();

    /// <inheritdoc />
    public async Task DetectAsync(ProfanityDetectionContext context)
    {
        NextProfanityDetectionHandlerDelegate pipeline = _ => Task.CompletedTask;

        for (int i = _handlers.Length - 1; i >= 0; i--)
        {
            IProfanityDetectionHandler handler = _handlers[i];
            NextProfanityDetectionHandlerDelegate next = pipeline;
            pipeline = ctx => handler.DetectAsync(ctx, next);
        }

        await pipeline(context);
    }
}
