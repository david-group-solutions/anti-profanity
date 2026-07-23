using System.Text.Json;

using DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Models;

namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Services.Stores;

/// <summary>
/// Provides methods for loading and saving detection data to a JSON file.
/// </summary>
public class DetectionsStore(string outputDirectory, JsonSerializerOptions jsonOptions)
{
    public async Task<Detections> LoadAsync()
    {
        Detections detections = new();

        string[] detectionPaths =
            Directory.GetFiles(outputDirectory, "*.detection.json", SearchOption.TopDirectoryOnly);

        foreach (string detectionPath in detectionPaths)
        {
            await using FileStream fs = new(detectionPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            fs.Seek(0, SeekOrigin.Begin);

            Dictionary<string, Detection> detection =
                await JsonSerializer.DeserializeAsync<Dictionary<string, Detection>>(fs, jsonOptions)
                ?? throw new NullReferenceException(
                    $"Failed to deserialize detection from file {detectionPath}.");

            string fileName = Path.GetFileNameWithoutExtension(
                Path.GetFileNameWithoutExtension(detectionPath));

            detections.Profanities.Add(fileName, detection);
        }

        return detections;
    }

    public async Task SaveAsync(string profanityInText, Dictionary<string, Detection> fileDetection)
    {
        string detectionPath = Path.Combine(outputDirectory, $"{profanityInText}.detection.json");
        await using FileStream fs = new(detectionPath, FileMode.Create, FileAccess.Write, FileShare.Read);
        await JsonSerializer.SerializeAsync(fs, fileDetection, jsonOptions);
    }
}
