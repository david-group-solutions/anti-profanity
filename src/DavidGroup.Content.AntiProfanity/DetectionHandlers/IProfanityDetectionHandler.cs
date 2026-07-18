namespace DavidGroup.Content.AntiProfanity.DetectionHandlers;

/// <summary>
/// Represents a handler in the profanity detection.
/// </summary>
public interface IProfanityDetectionHandler
{
    /// <summary>
    /// Performs profanity detection on the specified <paramref name="context"/>
    /// and optionally passes execution to the next handler in the pipeline.
    /// </summary>
    /// <param name="context">
    /// The context containing the input and state used during profanity detection.
    /// </param>
    /// <param name="next">
    /// The delegate that invokes the next handler in the pipeline, or <see langword="null"/>
    /// if this is the last handler.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous detection operation.
    /// </returns>
    Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next);
}
