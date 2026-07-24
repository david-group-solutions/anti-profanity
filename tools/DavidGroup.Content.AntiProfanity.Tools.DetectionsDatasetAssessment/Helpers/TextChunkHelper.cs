namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Helpers;

public static class TextChunkHelper
{
    public static (int StartIndex, int EndIndex) GetSmallChunkBoundaries(
        ReadOnlySpan<char> content,
        int detectionIndex,
        int detectionLength,
        int additionalSize)
    {
        int before = Math.Min(additionalSize, detectionIndex);
        int after = Math.Min(additionalSize, content.Length - (detectionIndex + detectionLength));

        if (before < additionalSize)
        {
            after = Math.Min(
                content.Length - (detectionIndex + detectionLength),
                after + (additionalSize - before));
        }

        if (after < additionalSize)
        {
            before = Math.Min(
                detectionIndex,
                before + (additionalSize - after));
        }

        int start = detectionIndex - before;
        int end = detectionIndex + detectionLength + after;

        return (start, end);
    }
}
