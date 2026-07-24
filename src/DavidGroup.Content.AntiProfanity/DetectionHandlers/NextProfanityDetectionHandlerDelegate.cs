namespace DavidGroup.Content.AntiProfanity.DetectionHandlers;

/// <summary>
/// Represents the delegate used to invoke the next handler in the profanity
/// detection pipeline.
/// </summary>
/// <param name="context">
/// The context containing the input and state used during profanity detection.
/// </param>
/// <returns>
/// A task that represents the asynchronous execution of the next handler.
/// </returns>
public delegate Task NextProfanityDetectionHandlerDelegate(ProfanityDetectionContext context);
