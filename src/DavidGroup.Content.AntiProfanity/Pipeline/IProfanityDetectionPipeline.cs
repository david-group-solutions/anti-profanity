using DavidGroup.Content.AntiProfanity.DetectionHandlers;

namespace DavidGroup.Content.AntiProfanity.Pipeline;

public interface IProfanityDetectionPipeline
{
    Task DetectAsync(ProfanityDetectionContext context);
}
