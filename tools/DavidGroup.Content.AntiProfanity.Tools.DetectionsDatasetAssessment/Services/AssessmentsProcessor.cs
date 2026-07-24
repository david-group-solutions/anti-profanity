using System.Text;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Enums;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Options;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Stores;
using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.UI;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Helpers;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Services;

public class AssessmentsProcessor(DatasetReader datasetReader, AssessmentsStore assessmentsStore)
{
    private const int BufferSize = 4096;

    public async Task RunAsync(ApplicationOptions options)
    {
        string resolveSourcesDirectory = PathHelpers.ResolveHomeDirectory(options.SourcesDir);
        string resolveDatasetsDirectory = PathHelpers.ResolveHomeDirectory(options.DetectionsDatasetDir);

        string[] detectionFiles = Directory.GetFiles(resolveDatasetsDirectory)
            .OrderBy(x => x)
            .ToArray();

        foreach (string detectionFile in detectionFiles)
        {
            List<DetectionRow> randomEntries = await datasetReader.ReadRandomEntriesAsync(
                detectionFile, options.MaximumAssessmentRequests);

            foreach (DetectionRow entry in randomEntries)
            {
                string sourcePath = Path.Combine(resolveSourcesDirectory, entry.FileName);
                await using FileStream fs = File.OpenRead(sourcePath);

                long start = Math.Max(0, entry.Position - BufferSize / 2);
                fs.Seek(start, SeekOrigin.Begin);

                byte[] buffer = new byte[BufferSize];
                int bytesRead = await fs.ReadAsync(buffer);

                int localByteOffset = (int)(entry.Position - start);

                int skip = 0;
                while (skip < bytesRead && (buffer[skip] & 0xC0) == 0x80)
                    skip++;

                string text = Encoding.UTF8.GetString(buffer, skip, bytesRead - skip);

                int byteOffsetInText = localByteOffset - skip;
                int charIndex = byteOffsetInText > 0
                    ? Encoding.UTF8.GetCharCount(buffer, skip, byteOffsetInText)
                    : 0;

                string rule = Path.GetFileNameWithoutExtension(detectionFile);

                AssessmentAnswer answer = DetectionAssessmentPrompt.Ask(
                    text, rule, charIndex, entry.Length, options.ContextPaddingSize);

                await assessmentsStore.AddAsync(rule, entry.FileName, entry.Position, entry.Length, answer);
            }
        }
    }
}
