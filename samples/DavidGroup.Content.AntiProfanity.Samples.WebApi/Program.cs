using System.Reflection;
using System.Text.Json.Serialization;

using DavidGroup.Content.AntiProfanity.DetectionHandlers.Implementations;
using DavidGroup.Content.AntiProfanity.Extensions;
using DavidGroup.Content.AntiProfanity.Samples.WebApi.CustomAntiProfanity;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddSwaggerGen();

builder.Services.AddAntiProfanity(builder.Configuration, Assembly.GetExecutingAssembly())
    .AddHandler<ProfanityDetectionJsonHandler>()
    .AddHandler<ProfanityDetectionTxtHandler>()
    .AddHandler<ProfanityDetectionSimpleTxtHandler>();

WebApplication app = builder.Build();

await app.Services.InitializeAntiProfanityDataSourcesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
