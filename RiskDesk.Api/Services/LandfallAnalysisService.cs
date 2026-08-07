using RiskDesk.Api.Models;

namespace RiskDesk.Api.Services;

public class LandfallAnalysisService
{
    private LandfallReport? _cachedReport;
    private readonly string _dataDirectory;
    public LandfallAnalysisService(IHostEnvironment environment)
    {
        _dataDirectory = Path.GetFullPath(
            Path.Combine(environment.ContentRootPath, "..", "Data"));
    }
    public LandfallReport GetReport()
    {
        // Reuse the report after the first request instead of parsing the dataset again.
        if (_cachedReport is not null)
        {
            return _cachedReport;
        }

        // Run the analysis pipeline once: parse, load the boundary, then detect events.
        var parser = new HurdatParser();
        var hurdatPath = Path.Combine(
            _dataDirectory,
            "hurdat2-1851-2025-02272026.txt");
        var boundaryPath = Path.Combine(
            _dataDirectory,
            "florida.geojson");

        var storms = parser.ParseFile(hurdatPath);
        var loader = new FloridaBoundaryLoader();
        var boundary = loader.Load(boundaryPath);
        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(storms, boundary);

        _cachedReport = new LandfallReport
        {
            StormCount = storms.Count,
            LandfallCount = events.Count,
            Events = events
        };

        return _cachedReport;
    }
}
