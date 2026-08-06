using RiskDesk.Api.Models;
using RiskDesk.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddCors();
var app = builder.Build();
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

app.UseHttpsRedirection();

app.MapGet("/api/landfalls", () =>
{
    // create parser
    var parser = new HurdatParser();
    // parse the HURDAT file
    var dataDirectory = Path.GetFullPath(
        Path.Combine(app.Environment.ContentRootPath, "..", "Data"));

    var hurdatPath = Path.Combine(
        dataDirectory,
        "hurdat2-1851-2025-02272026.txt");

    var boundaryPath = Path.Combine(
        dataDirectory,
        "florida.geojson");

    var storms = parser.ParseFile(hurdatPath);

    // load the Florida boundary
    var loader = new FloridaBoundaryLoader();
    var boundary = loader.Load(boundaryPath);
    // detect landfalls
    var detector = new FloridaLandfallDetector();
    var events = detector.Detect(storms, boundary);
    // create and return a LandfallReport
    var report = new LandfallReport
    {
        StormCount = storms.Count,
        LandfallCount = events.Count,
        Events = events
    };
    return Results.Ok(report);
});


app.Run();
