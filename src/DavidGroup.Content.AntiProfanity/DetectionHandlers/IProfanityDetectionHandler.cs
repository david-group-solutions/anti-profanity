namespace DavidGroup.Content.AntiProfanity.DetectionHandlers;

public interface IProfanityDetectionHandler
{
    Task DetectAsync(ProfanityDetectionContext context, NextProfanityDetectionHandlerDelegate? next);
}
