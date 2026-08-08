using RiskDesk.Api.Models;
using RiskDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();
builder.Services.AddSingleton<LandfallAnalysisService>();
var app = builder.Build();

// Allow the Electron desktop app to call the local API.
app.UseCors(policy =>
    policy
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod());
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/api/landfalls", (LandfallAnalysisService analysisService) =>
{
    // Return the cached analysis report to the desktop client.
    var report = analysisService.GetReport();
    return Results.Ok(report);
});

app.MapGet("/api/health", () => Results.Ok());


app.Run();
