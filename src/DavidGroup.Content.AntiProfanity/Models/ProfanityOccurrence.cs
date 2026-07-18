namespace DavidGroup.Content.AntiProfanity.Models;

/// <summary>
/// Detected result.
/// </summary>
/// <param name="Profanity">Detected profanity.</param>
/// <param name="StartIndex">Where the detected word starts.</param>
/// <param name="EndIndex">Where the detected word ends.</param>
/// <param name="Details">Any additional details worth adding.</param>
public record ProfanityOccurrence(string Profanity, int StartIndex, int EndIndex, object? Details = null);
