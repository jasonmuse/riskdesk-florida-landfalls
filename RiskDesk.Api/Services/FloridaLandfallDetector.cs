using NetTopologySuite.Geometries;
using RiskDesk.Api.Models;

namespace RiskDesk.Api.Services;

public class FloridaLandfallDetector
{
    private const int HurricaneWindThresholdKnots = 64;
    private const double CoastlineToleranceDegrees = 0.02;
    private const double AdjacentStateApproachDistanceDegrees = 0.01;
    private static readonly TimeSpan SupplementalDuplicateWindow =
        TimeSpan.FromHours(12);

    public List<FloridaLandfallEvent> Detect(
        List<Storm> storms,
        Geometry floridaBoundary,
        Geometry? adjacentStateLand = null)
    {
        var geometryFactory = new GeometryFactory();
        var exactEvents = DetectCrossings(
            storms,
            floridaBoundary,
            floridaBoundary,
            adjacentStateLand,
            geometryFactory,
            isTolerancePass: false);

        // HURDAT2 coordinates are rounded to tenths of a degree. A small
        // supplemental tolerance recovers tiny-island crossings whose track
        // passes just outside the more detailed Census geometry.
        var toleranceBoundary = floridaBoundary.Buffer(CoastlineToleranceDegrees);
        var toleranceCandidates = DetectCrossings(
            storms,
            toleranceBoundary,
            floridaBoundary,
            adjacentStateLand,
            geometryFactory,
            isTolerancePass: true);
        var supplementalEvents = new List<FloridaLandfallEvent>();

        foreach (var candidate in toleranceCandidates)
        {
            var duplicatesExactEvent = exactEvents.Any(existingEvent =>
                IsSameLandfall(existingEvent, candidate));
            var duplicatesSupplementalEvent = supplementalEvents.Any(existingEvent =>
                IsSameLandfall(existingEvent, candidate));

            if (!duplicatesExactEvent && !duplicatesSupplementalEvent)
            {
                supplementalEvents.Add(candidate);
            }
        }

        return exactEvents
            .Concat(supplementalEvents)
            .OrderBy(landfallEvent => landfallEvent.LandfallTimeUtc)
            .ToList();
    }

    private static List<FloridaLandfallEvent> DetectCrossings(
        List<Storm> storms,
        Geometry detectionBoundary,
        Geometry floridaLand,
        Geometry? adjacentStateLand,
        GeometryFactory geometryFactory,
        bool isTolerancePass)
    {
        var events = new List<FloridaLandfallEvent>();

        foreach (var storm in storms)
        {
            for (var pointIndex = 0;
                 pointIndex < storm.TrackPoints.Count - 1;
                 pointIndex++)
            {
                var start = storm.TrackPoints[pointIndex];
                var end = storm.TrackPoints[pointIndex + 1];

                if (end.TimestampUtc.Year < 1900)
                {
                    continue;
                }

                var startCoordinate = new Coordinate(
                    start.Longitude,
                    start.Latitude);
                var endCoordinate = new Coordinate(
                    end.Longitude,
                    end.Latitude);
                var startLocation = geometryFactory.CreatePoint(startCoordinate);
                var segment = geometryFactory.CreateLineString(new[]
                {
                    startCoordinate,
                    endCoordinate
                });

                if (floridaLand.Covers(startLocation) ||
                    (isTolerancePass && segment.Intersects(floridaLand)) ||
                    !segment.Crosses(detectionBoundary))
                {
                    continue;
                }

                var crossingFraction = FindCrossingFraction(
                    segment,
                    detectionBoundary,
                    startCoordinate,
                    endCoordinate);

                if (ApproachesFromAdjacentStateLand(
                    startCoordinate,
                    endCoordinate,
                    crossingFraction,
                    adjacentStateLand,
                    geometryFactory))
                {
                    continue;
                }

                var landfallTime = start.TimestampUtc + TimeSpan.FromTicks(
                    (long)((end.TimestampUtc - start.TimestampUtc).Ticks *
                           crossingFraction));
                var landfallWind = EstimateLandfallWind(
                    start,
                    end,
                    crossingFraction);

                if (landfallTime.Year < 1900 || landfallWind is null)
                {
                    continue;
                }

                events.Add(CreateEvent(
                    storm,
                    pointIndex,
                    landfallTime,
                    landfallWind.Value,
                    floridaLand,
                    geometryFactory));
            }
        }

        return events;
    }

    private static int? EstimateLandfallWind(
        TrackPoint start,
        TrackPoint end,
        double crossingFraction)
    {
        if (start.Status == "HU" && end.Status == "HU")
        {
            return (int)Math.Round(
                start.MaxSustainedWindKnots +
                ((end.MaxSustainedWindKnots -
                  start.MaxSustainedWindKnots) * crossingFraction),
                MidpointRounding.AwayFromZero);
        }

        // When status changes between observations, the transition time is
        // unknown. Use the bracketing hurricane observation rather than
        // interpolating the event below hurricane strength.
        if (start.Status == "HU" &&
            start.MaxSustainedWindKnots >= HurricaneWindThresholdKnots)
        {
            return start.MaxSustainedWindKnots;
        }

        if (end.Status == "HU" &&
            end.MaxSustainedWindKnots >= HurricaneWindThresholdKnots)
        {
            return end.MaxSustainedWindKnots;
        }

        return null;
    }

    private static FloridaLandfallEvent CreateEvent(
        Storm storm,
        int pointIndex,
        DateTimeOffset landfallTime,
        int landfallWind,
        Geometry floridaBoundary,
        GeometryFactory geometryFactory)
    {
        var maxFloridaWind = landfallWind;

        for (var laterIndex = pointIndex + 1;
             laterIndex < storm.TrackPoints.Count;
             laterIndex++)
        {
            var laterPoint = storm.TrackPoints[laterIndex];
            var laterLocation = geometryFactory.CreatePoint(
                new Coordinate(
                    laterPoint.Longitude,
                    laterPoint.Latitude));

            if (!floridaBoundary.Covers(laterLocation) ||
                laterPoint.Status != "HU")
            {
                break;
            }

            maxFloridaWind = Math.Max(
                maxFloridaWind,
                laterPoint.MaxSustainedWindKnots);
        }

        return new FloridaLandfallEvent
        {
            StormId = storm.Id,
            StormName = storm.Name,
            LandfallTimeUtc = landfallTime,
            LandfallWindSpeedKnots = landfallWind,
            MaxFloridaWindSpeedKnots = maxFloridaWind
        };
    }

    private static bool IsSameLandfall(
        FloridaLandfallEvent first,
        FloridaLandfallEvent second)
    {
        return first.StormId == second.StormId &&
               (first.LandfallTimeUtc - second.LandfallTimeUtc).Duration() <=
               SupplementalDuplicateWindow;
    }

    private static bool ApproachesFromAdjacentStateLand(
        Coordinate start,
        Coordinate end,
        double crossingFraction,
        Geometry? adjacentStateLand,
        GeometryFactory geometryFactory)
    {
        if (adjacentStateLand is null)
        {
            return false;
        }

        var longitudeChange = end.X - start.X;
        var latitudeChange = end.Y - start.Y;
        var segmentLength = Math.Sqrt(
            (longitudeChange * longitudeChange) +
            (latitudeChange * latitudeChange));
        var approachFraction = segmentLength == 0
            ? 0
            : Math.Max(
                0,
                crossingFraction -
                (AdjacentStateApproachDistanceDegrees / segmentLength));
        var approachPoint = geometryFactory.CreatePoint(new Coordinate(
            start.X + (longitudeChange * approachFraction),
            start.Y + (latitudeChange * approachFraction)));

        return adjacentStateLand.Covers(approachPoint);
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
