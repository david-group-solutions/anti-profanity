namespace DavidGroup.Content.AntiProfanity.Models;

/// <summary>
/// Detected result.
/// </summary>
/// <param name="Profanity">Detected profanity.</param>
/// <param name="Index">The index of the detected word.</param>
/// <param name="Length">The lenght of the detected word.</param>
/// <param name="Details">Any additional details worth adding.</param>
public record ProfanityOccurrence(string Profanity, int Index, int Length, object? Details = null);
