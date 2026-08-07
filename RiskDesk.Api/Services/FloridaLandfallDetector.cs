using NetTopologySuite.Geometries;
using RiskDesk.Api.Models;

namespace RiskDesk.Api.Services;

public class FloridaLandfallDetector
{
    public List<FloridaLandfallEvent> Detect(
        List<Storm> storms,
        Geometry floridaBoundary)
    {
        var events = new List<FloridaLandfallEvent>();
        var geometryFactory = new GeometryFactory();

        // Check each pair of track points to find where the storm crosses Florida.
        foreach (var storm in storms)
        {
            for (var pointIndex = 0;
                pointIndex < storm.TrackPoints.Count - 1;
                pointIndex++)
            {
                var start = storm.TrackPoints[pointIndex];
                var end = storm.TrackPoints[pointIndex + 1];

                // Filter by only landfalls from 1900 onward.
                if (end.TimestampUtc.Year < 1900)
                {
                    continue;
                }

                // Create a line between the two track points.
                var segment = geometryFactory.CreateLineString(new[]
                {
                    new Coordinate(start.Longitude, start.Latitude),
                    new Coordinate(end.Longitude, end.Latitude)
                });

                var startLocation = geometryFactory.CreatePoint(
                    new Coordinate(start.Longitude, start.Latitude));

                var endLocation = geometryFactory.CreatePoint(
                    new Coordinate(end.Longitude, end.Latitude));

                // Covers includes points directly on the coastline boundary.
                var startsOutside = !floridaBoundary.Covers(startLocation);
                var crossesIntoFlorida = segment.Intersects(floridaBoundary);

                var isHurricaneAtCrossing =
                    start.Status == "HU" && end.Status == "HU";

                if (startsOutside &&
                    crossesIntoFlorida &&
                    isHurricaneAtCrossing)
                {
                    // Find where the segment first reaches Florida and interpolate that moment.
                    var crossingFraction = FindCrossingFraction(
                        segment,
                        floridaBoundary,
                        startLocation.Coordinate,
                        endLocation.Coordinate);
                    var landfallTime = start.TimestampUtc + TimeSpan.FromTicks(
                        (long)((end.TimestampUtc - start.TimestampUtc).Ticks * crossingFraction));
                    var landfallWind = (int)Math.Round(
                        start.MaxSustainedWindKnots +
                        ((end.MaxSustainedWindKnots - start.MaxSustainedWindKnots) * crossingFraction),
                        MidpointRounding.AwayFromZero);

                    if (landfallTime.Year < 1900)
                    {
                        continue;
                    }

                    var maxFloridaWind = landfallWind;

                    // Check later points while the storm is still a hurricane in Florida.
                    // Keep the highest wind found there.
                    for (var laterIndex = pointIndex + 1;
                         laterIndex < storm.TrackPoints.Count;
                         laterIndex++)
                    {
                        var laterPoint = storm.TrackPoints[laterIndex];

                        var laterLocation = geometryFactory.CreatePoint(
                            new Coordinate(laterPoint.Longitude, laterPoint.Latitude));

                        var laterIsInside = floridaBoundary.Covers(laterLocation);
                        var laterIsHurricane = laterPoint.Status == "HU";

                        if (!laterIsInside || !laterIsHurricane)
                        {
                            break;
                        }

                        maxFloridaWind = Math.Max(
                            maxFloridaWind,
                            laterPoint.MaxSustainedWindKnots);
                    }

                    events.Add(new FloridaLandfallEvent
                    {
                        StormId = storm.Id,
                        StormName = storm.Name,
                        LandfallTimeUtc = landfallTime,
                        LandfallWindSpeedKnots = landfallWind,
                        MaxFloridaWindSpeedKnots = maxFloridaWind
                    });
                }
            }
        }

        return events;
    }

    private static double FindCrossingFraction(
        LineString segment,
        Geometry floridaBoundary,
        Coordinate start,
        Coordinate end)
    {
        var floridaPartOfSegment = segment.Intersection(floridaBoundary);
        var longitudeChange = end.X - start.X;
        var latitudeChange = end.Y - start.Y;
        var segmentLengthSquared =
            (longitudeChange * longitudeChange) +
            (latitudeChange * latitudeChange);

        if (segmentLengthSquared == 0)
        {
            return 0;
        }

        return floridaPartOfSegment.Coordinates
            .Select(coordinate =>
                (((coordinate.X - start.X) * longitudeChange) +
                 ((coordinate.Y - start.Y) * latitudeChange)) /
                segmentLengthSquared)
            .Select(fraction => Math.Clamp(fraction, 0, 1))
            .Min();
    }
}
