using System.Collections.ObjectModel;

using DavidGroup.Content.AntiProfanity.Models;
using DavidGroup.Content.AntiProfanity.Samples.WebApi.Dtos;
using DavidGroup.Content.AntiProfanity.Services;

using Microsoft.AspNetCore.Mvc;

namespace DavidGroup.Content.AntiProfanity.Samples.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AntiProfanityController(IAntiProfanityService antiProfanityService) : ControllerBase
{
    [HttpPost("detect")]
    public async Task<IActionResult> Detect([FromBody] DetectionsRequestDto dto)
    {
        ReadOnlyCollection<ProfanityOccurrence> detections =
            await antiProfanityService.DetectAsync(dto.Text, dto.SeverityLevel);

        return Ok(detections);
    }

    [HttpPost("censor")]
    public async Task<IActionResult> Censor([FromBody] CensorRequestDto dto)
    {
        string censored =
            await antiProfanityService.CensorAsync(dto.Text, dto.SeverityLevel, dto.CensorChar);

        return Ok(censored);
    }
}
