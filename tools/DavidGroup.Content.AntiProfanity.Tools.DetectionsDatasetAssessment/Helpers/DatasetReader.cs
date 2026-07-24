using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Stores;
using DavidGroup.Content.AntiProfanity.Tools.Shared.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetAssessment.Helpers;

public class DatasetReader(AssessmentsStore assessmentsStore)
{
    public async Task<List<DetectionRow>> ReadRandomEntriesAsync(
        string path,
        int count)
    {
        Random random = Random.Shared;
        List<DetectionRow> reservoir = new(count);

        using StreamReader reader = new(path);
        int lineNumber = 0;

        while (await reader.ReadLineAsync() is { } line)
        {
            DetectionRow row = JsonSerializer.Deserialize<DetectionRow>(line)!;

            if (await assessmentsStore.AlreadyAnsweredAsync(path, row.Position))
                continue;

            if (lineNumber < count)
                reservoir.Add(row);
            else
            {
                int index = random.Next(lineNumber + 1);
                if (index < count)
                    reservoir[index] = row;
            }

            lineNumber++;
        }

        return reservoir;
    }
}
