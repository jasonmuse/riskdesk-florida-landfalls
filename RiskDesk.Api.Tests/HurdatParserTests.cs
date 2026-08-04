using RiskDesk.Api.Services;

namespace RiskDesk.Api.Tests;

public class HurdatParserTests
{
    [Fact]
    public void ParseHeader_ValidHeader_ReturnsParsedStorm()
    {
        var headerLine = "AL011851,            UNNAMED,     14,";

        var parser = new HurdatParser();
        var storm = parser.ParseHeader(headerLine);

        Assert.Equal("AL011851", storm.Id);
        Assert.Equal("UNNAMED", storm.Name);
        Assert.Equal(14, storm.DeclaredObservationCount);
    }

    [Fact]

    public void ParseTrackPoint_ValidObservation_ReturnsTrackPoint()
    {
        var observationLine =
        "18510625, 0000,  , HU, 28.0N, 94.8W, 80, -999";

        var parser = new HurdatParser();
        var trackPoint = parser.ParseTrackPoint(observationLine);

        Assert.Equal(new DateTimeOffset(1851, 6, 25, 0,0,0, TimeSpan.Zero) , trackPoint.TimestampUtc);
        Assert.Equal("HU", trackPoint.Status);
        Assert.Equal(28.0, trackPoint.Latitude);
        Assert.Equal(-94.8, trackPoint.Longitude);
        Assert.Equal(80, trackPoint.MaxWindSpeedKnots);
    }

    [Fact]
    public void ParseLines_OneStorm_ReturnsStormWithAllTrackPoints()
    {
        var lines = new[]
        {
            "AL012000, TEST, 2,",
            "20000801, 0000,  , HU, 25.0N, 80.0W, 70, -999",
            "20000801, 0600,  , HU, 25.5N, 81.0W, 75, -999"
        };

        var parser = new HurdatParser();
        var storms = parser.ParseLines(lines);

        var storm = Assert.Single(storms);
        Assert.Equal("AL012000", storm.Id);
        Assert.Equal(2, storm.TrackPoints.Count);
    }

    [Fact]
    public void ParseLines_MultipleStorms_ReturnsEveryStorm()
    {
        var lines = new[]
        {
            "AL012000, FIRST, 2,",
            "20000801, 0000,  , HU, 25.0N, 80.0W, 70, -999",
            "20000801, 0600,  , HU, 25.5N, 81.0W, 75, -999",

            "AL022000, SECOND, 1,",
            "20000901, 1200,  , TS, 20.0N, 60.0W, 40, -999"
        };

        var parser = new HurdatParser();
        var storms = parser.ParseLines(lines);

        Assert.Equal(2, storms.Count);
        Assert.Equal("AL022000", storms[1].Id);
        Assert.Single(storms[1].TrackPoints);
    }

}
