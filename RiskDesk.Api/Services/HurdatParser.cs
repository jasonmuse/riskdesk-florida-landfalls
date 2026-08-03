using RiskDesk.Api.Models;
using System.Globalization;

namespace RiskDesk.Api.Services;

public class HurdatParser
{
    public Storm ParseHeader(string headerLine)
    {
        var fields = headerLine.Split(',');
        var storm = new Storm();

        storm.Id = fields[0].Trim();
        storm.Name = fields[1].Trim();
        storm.DeclaredObservationCount = int.Parse(fields[2].Trim());

        return storm;
    }

    public TrackPoint ParseTrackPoint(string observationLine)
    {
        var fields = observationLine.Split(',');
        var trackPoint = new TrackPoint();

        var timestampText = $"{fields[0].Trim()} {fields[1].Trim()}";
        trackPoint.TimestampUtc = DateTimeOffset.ParseExact(
            timestampText,
            "yyyyMMdd HHmm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        trackPoint.Status = fields[3].Trim();

        trackPoint.Latitude = ParseCoordinate(fields[4]);
        trackPoint.Longitude = ParseCoordinate(fields[5]);
  
        
        trackPoint.MaxWindSpeedKnots = int.Parse(fields[6].Trim());

        return trackPoint;
    }

    private static double ParseCoordinate(string coordinateText)
    {
        
        var trimmedCoordinate = coordinateText.Trim();

        var coordinateDirection = trimmedCoordinate[^1];
        var coordinateMagnitude = double.Parse(
            trimmedCoordinate[..^1],
            CultureInfo.InvariantCulture);
        
        var isNegative = coordinateDirection == 'S' || coordinateDirection == 'W';

        return isNegative ? -coordinateMagnitude : coordinateMagnitude;

    }
}