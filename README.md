# DavidGroup.Content.AntiProfanity

#### [![Release](https://github.com/david-group-solutions/anti-profanity/actions/workflows/release.yml/badge.svg)](https://github.com/david-group-solutions/anti-profanity/actions/workflows/release.yml) [![Nuget](https://img.shields.io/nuget/v/DavidGroup.Content.AntiProfanity)](https://www.nuget.org/packages/DavidGroup.Content.AntiProfanity/)

Detects profanity in text using predefined word lists in advanced JSON or simple TXT formats.

---

## 🚀 Getting Started

### Install NuGet Package

Using the .NET CLI:

```bash
dotnet add package DavidGroup.Content.AntiProfanity
```

Or via the Package Manager Console:

```bash
Install-Package DavidGroup.Content.AntiProfanity
```

### How to use it?

Feel free to explore the [samples](https://github.com/david-group-solutions/anti-profanity/tree/main/samples) to find
practical examples for each feature.
New samples are added continuously as more features are developed.

## 📦 Key Features

### Registration

#### Program.sc

```csharp
WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddAntiProfanity(builder.Configuration, Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

await app.Services.InitializeAntiProfanityDataSourcesAsync();

app.Run();
```

#### appsettings.json

```json
{
    "AntiProfanity": {
        "DataSourcesBasePath": "Data/ProfanityDataSources",
        "DataSources": [
            "en.json",
            "ru.txt"
        ]
    }
}
```

---

### Detection & Censoring

```csharp
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

```

## 🤝 Contributing

Found a bug? Have an idea? Want to contribute?

* Submit an issue:
  https://github.com/david-group-solutions/anti-profanity/issues
* Create a pull request:
  https://github.com/david-group-solutions/anti-profanity/pulls

Contributions of any size are appreciated!

## 📝 License

Distributed under the **MIT license**.
See [License](https://github.com/david-group-solutions/anti-profanity/blob/main/LICENSE.txt) for more information.

Copyright © 2025-2026 David Khachatryan (David Group Solutions)
