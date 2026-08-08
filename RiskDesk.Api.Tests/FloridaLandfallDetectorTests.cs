
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
        Assert.Equal(83, landfallEvent.LandfallWindSpeedKnots);
        Assert.Equal(85, landfallEvent.MaxFloridaWindSpeedKnots);
    }

    [Fact]
    public void UsesHigherHurricaneWindAfterLandfall()
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
            Id = "TEST04",
            Name = "TEST4",
            DeclaredObservationCount = 3,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 75
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 12, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -81,
                    MaxSustainedWindKnots = 100
                }
            ]
        };

        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(new List<Storm> { storm }, boundary);

        var landfallEvent = Assert.Single(events);

        Assert.Equal(78, landfallEvent.LandfallWindSpeedKnots);
        Assert.Equal(100, landfallEvent.MaxFloridaWindSpeedKnots);
    }

    [Fact]
    public void IgnoresTropicalStormCrossing()
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
            Id = "TEST05",
            Name = "TEST5",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "TS",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 60
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "TS",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 60
                }
            ]
        };

        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(new List<Storm> { storm }, boundary);

        Assert.Empty(events);
    }

    [Fact]
    public void DetectsStrengtheningStormAtHurricaneWindCrossing()
    {
        var storm = CreateTwoPointStorm("TS", 60, "HU", 70);

        var events = new FloridaLandfallDetector().Detect(
            [storm],
            CreateTestBoundary());

        var landfallEvent = Assert.Single(events);
        Assert.Equal(70, landfallEvent.LandfallWindSpeedKnots);
    }

    [Fact]
    public void DetectsWeakeningStormAtHurricaneWindCrossing()
    {
        var storm = CreateTwoPointStorm("HU", 70, "TS", 60);

        var events = new FloridaLandfallDetector().Detect(
            [storm],
            CreateTestBoundary());

        var landfallEvent = Assert.Single(events);
        Assert.Equal(70, landfallEvent.LandfallWindSpeedKnots);
    }

    [Fact]
    public void UsesHurricaneObservationWhenStatusChangesAcrossBoundary()
    {
        var storm = CreateTwoPointStorm("TS", 55, "HU", 65);

        var events = new FloridaLandfallDetector().Detect(
            [storm],
            CreateTestBoundary());

        var landfallEvent = Assert.Single(events);
        Assert.Equal(65, landfallEvent.LandfallWindSpeedKnots);
    }

    [Fact]
    public void IgnoresTrackEnteringFloridaThroughAdjacentStateLand()
    {
        var adjacentStateLand = new GeometryFactory().CreatePolygon(new[]
        {
            new Coordinate(-86, 24),
            new Coordinate(-83, 24),
            new Coordinate(-83, 31),
            new Coordinate(-86, 31),
            new Coordinate(-86, 24)
        });
        var storm = CreateTwoPointStorm("HU", 80, "HU", 85);

        var events = new FloridaLandfallDetector().Detect(
            [storm],
            CreateTestBoundary(),
            adjacentStateLand);

        Assert.Empty(events);
    }

    [Fact]
    public void IgnoresTrackThatOnlyTouchesBoundary()
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
            Id = "TEST07",
            Name = "TEST7",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 30,
                    Longitude = -84,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 32,
                    Longitude = -82,
                    MaxSustainedWindKnots = 85
                }
            ]
        };

        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(new List<Storm> { storm }, boundary);

        Assert.Empty(events);
    }

    [Fact]
    public void DetectsRoundedTrackWithinCoastlineTolerance()
    {
        var boundary = CreateTestBoundary();
        var storm = new Storm
        {
            Id = "TOLERANCE01",
            Name = "TOLERANCE",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 23.99,
                    Longitude = -84,
                    MaxSustainedWindKnots = 80
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 23.99,
                    Longitude = -82,
                    MaxSustainedWindKnots = 80
                }
            ]
        };

        var events = new FloridaLandfallDetector().Detect([storm], boundary);

        Assert.Single(events);
    }

    [Fact]
    public void DetectsTwoSeparateLandfallsForOneStorm()
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
            Id = "TEST06",
            Name = "TEST6",
            DeclaredObservationCount = 4,
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
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 12, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = 85
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 18, 0, 0, TimeSpan.Zero),
                    Status = "HU",
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = 90
                }
            ]
        };

        var detector = new FloridaLandfallDetector();
        var events = detector.Detect(new List<Storm> { storm }, boundary);

        Assert.Equal(2, events.Count);
        Assert.All(events, landfallEvent =>
            Assert.Equal("TEST06", landfallEvent.StormId));
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

    [Fact]
    public void DetectsRepresentativeFloridaLandfallsFromRealData()
    {
        var dataDirectory = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "../../../../Data"));
        var hurdatPath = Path.Combine(
            dataDirectory,
            "hurdat2-1851-2025-02272026.txt");
        var boundaryPath = Path.Combine(dataDirectory, "florida.geojson");

        var storms = new HurdatParser().ParseFile(hurdatPath);
        var boundary = new FloridaBoundaryLoader().Load(boundaryPath);
        var adjacentStateLand = new FloridaBoundaryLoader().LoadCombined(
            Path.Combine(dataDirectory, "florida-adjacent-states.geojson"));
        var events = new FloridaLandfallDetector().Detect(
            storms,
            boundary,
            adjacentStateLand);

        Assert.Equal(104, events.Count);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "ANDREW" &&
            landfallEvent.LandfallWindSpeedKnots == 145);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "IRMA" &&
            landfallEvent.LandfallWindSpeedKnots == 115);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "MICHAEL" &&
            landfallEvent.LandfallWindSpeedKnots == 140);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "ELOISE" &&
            landfallEvent.LandfallWindSpeedKnots == 110);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "AGNES" &&
            landfallEvent.LandfallWindSpeedKnots == 65);
        Assert.Contains(events, landfallEvent =>
            landfallEvent.StormName == "NICOLE" &&
            landfallEvent.LandfallWindSpeedKnots == 65);
        Assert.DoesNotContain(events, landfallEvent =>
            landfallEvent.StormName == "SALLY");
        var ianWinds = events
            .Where(landfallEvent => landfallEvent.StormName == "IAN")
            .Select(landfallEvent => landfallEvent.LandfallWindSpeedKnots)
            .ToList();
        Assert.True(
            ianWinds.Any(wind => Math.Abs(wind - 130) <= 1),
            $"Expected a 130 kt Ian landfall, but found: {string.Join(", ", ianWinds)}");
        Assert.Contains(110, ianWinds);
        var miltonWinds = events
            .Where(landfallEvent => landfallEvent.StormName == "MILTON")
            .Select(landfallEvent => landfallEvent.LandfallWindSpeedKnots)
            .ToList();
        Assert.True(
            miltonWinds.Any(wind => Math.Abs(wind - 100) <= 1),
            $"Expected a 100 kt Milton landfall, but found: {string.Join(", ", miltonWinds)}");
    }

    private static Polygon CreateTestBoundary()
    {
        return new GeometryFactory().CreatePolygon(new[]
        {
            new Coordinate(-83, 24),
            new Coordinate(-80, 24),
            new Coordinate(-80, 31),
            new Coordinate(-83, 31),
            new Coordinate(-83, 24)
        });
    }

    private static Storm CreateTwoPointStorm(
        string startStatus,
        int startWind,
        string endStatus,
        int endWind)
    {
        return new Storm
        {
            Id = "MIXED01",
            Name = "MIXED",
            DeclaredObservationCount = 2,
            TrackPoints =
            [
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 0, 0, 0, TimeSpan.Zero),
                    Status = startStatus,
                    Latitude = 27,
                    Longitude = -84,
                    MaxSustainedWindKnots = startWind
                },
                new TrackPoint
                {
                    TimestampUtc = new DateTimeOffset(2022, 9, 1, 6, 0, 0, 0, TimeSpan.Zero),
                    Status = endStatus,
                    Latitude = 27,
                    Longitude = -82,
                    MaxSustainedWindKnots = endWind
                }
            ]
        };
    }

}
