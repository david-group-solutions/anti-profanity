using DavidGroup.Content.AntiProfanity.DetectionHandlers;

namespace DavidGroup.Content.AntiProfanity.Pipelines;

/// <summary>
/// Represents a pipeline that executes a sequence of profanity detection handlers.
/// </summary>
public interface IProfanityDetectionPipeline
{
    /// <summary>
    /// Executes the profanity detection pipeline using the specified context.
    /// </summary>
    /// <param name="context">
    /// The context containing the input and state used throughout the detection process.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous pipeline execution.
    /// </returns>
    Task DetectAsync(ProfanityDetectionContext context);
}
