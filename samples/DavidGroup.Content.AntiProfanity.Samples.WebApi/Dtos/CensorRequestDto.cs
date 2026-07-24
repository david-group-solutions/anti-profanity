using DavidGroup.Content.AntiProfanity.Enums;

namespace DavidGroup.Content.AntiProfanity.Samples.WebApi.Dtos;

public record CensorRequestDto(string Text, ProfanitySeverityLevel SeverityLevel, char CensorChar);
