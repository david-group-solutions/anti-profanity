using DavidGroup.Content.AntiProfanity.Enums;

namespace DavidGroup.Content.AntiProfanity.Samples.WebApi.Dtos;

public record DetectionsRequestDto(string Text, ProfanitySeverityLevel SeverityLevel);
