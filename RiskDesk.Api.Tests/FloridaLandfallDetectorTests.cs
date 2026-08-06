
using RiskDesk.Api.Models;
using RiskDesk.Api.Services;
using NetTopologySuite.Geometries;

namespace RiskDesk.Api.Tests;

public class FloridaLandfallDetectorTests
{
    [Fact]
    public void DetectsLandfallWhenTrackCrossesBoundary()
    {

        var boundary = new GeometryFactory().CreatePolygon(new[]
        {
            new Coordinate(-83, 24),
            new Coordinate(-80, 24),
            new Coordinate(-80, 31),
            new Coordinate(-83, 31),
            new Coordinate(-83, 24)
        });

        var storm = new Storm
        {
            Id = "TEST01",
            Name = "TEST",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 85
                }
            ]
        };

        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(new List<Storm> { storm }, boundary);

        var landfallEvent = Assert.Single(events);

        Assert.Equal("TEST01", landfallEvent.StormId);
        Assert.Equal("TEST", landfallEvent.StormName);
        Assert.Equal(85, landfallEvent.LandfallWindSpeedKnots);
        Assert.Equal(85, landfallEvent.MaxFloridaWindSpeedKnots);
    }


    [Fact]

    public void LoadsFloridaGeoJsonBoundary()
    {
        var filePath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "../../../../Data/florida.geojson"));

        var loader = new FloridaBoundaryLoader();

        var boundary = loader.Load(filePath);

        Assert.False(boundary.IsEmpty);

        var geometryFactory = new GeometryFactory();
        var floridaPoint = geometryFactory.CreatePoint(
            new Coordinate(-84.29, 30.44));

        Assert.True(boundary.Contains(floridaPoint));
    }

    [Fact]

    public void DateFilteringPost1900()
    {
        var preStorm = new Storm
        {
            Id = "TEST02",
            Name = "TEST2",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(1899, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(1899, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 85
                }
            ]
        };
        var postStorm = new Storm
        {
            Id = "TEST03",
            Name = "TEST3",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(1900, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(1900, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 85
                }
            ]
        };

        var boundary = new GeometryFactory().CreatePolygon(new[]
        {
            new Coordinate(-83, 24),
            new Coordinate(-80, 24),
            new Coordinate(-80, 31),
            new Coordinate(-83, 31),
            new Coordinate(-83, 24)
        });

        var detector = new FloridaLandfallDetector();

        var events = detector.Detect(
            new List<Storm> { preStorm, postStorm },
            boundary);

        Assert.Single(events);
        Assert.Equal("TEST03", events[0].StormId);


    }



}