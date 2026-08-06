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

                var segment = geometryFactory.CreateLineString(new[]
                {
                    new Coordinate(start.Longitude, start.Latitude),
                    new Coordinate(end.Longitude, end.Latitude)
                });

                var startLocation = geometryFactory.CreatePoint(
                    new Coordinate(start.Longitude, start.Latitude));

                var endLocation = geometryFactory.CreatePoint(
                    new Coordinate(end.Longitude, end.Latitude));

                var startsOutside = !floridaBoundary.Contains(startLocation);
                var endsInside = floridaBoundary.Contains(endLocation);
                var crossesBoundary = segment.Intersects(floridaBoundary);

                var isHurricaneAtCrossing =
                    start.Status == "HU" && end.Status == "HU";


                if (startsOutside &&
                    endsInside &&
                    crossesBoundary &&
                    isHurricaneAtCrossing)
                {
                    events.Add(new FloridaLandfallEvent
                    {
                        StormId = storm.Id,
                        StormName = storm.Name,
                        LandfallTimeUtc = end.TimestampUtc,
                        LandfallWindSpeedKnots = end.MaxSustainedWindKnots,
                        MaxFloridaWindSpeedKnots = end.MaxSustainedWindKnots
                    });
                }
            }
        }

        return events;
    }
}