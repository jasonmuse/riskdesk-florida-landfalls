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
}
