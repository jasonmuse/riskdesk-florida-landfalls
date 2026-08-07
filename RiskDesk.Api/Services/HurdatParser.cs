using RiskDesk.Api.Models;
using System.Globalization;

namespace RiskDesk.Api.Services;

public class HurdatParser
{
    public Storm ParseHeader(string headerLine)
    {
        // Split the header into the storm ID, name, and observation count.
        var fields = headerLine.Split(',');
        var storm = new Storm();

        storm.Id = fields[0].Trim();
        storm.Name = fields[1].Trim();
        storm.DeclaredObservationCount = int.Parse(fields[2].Trim());

        return storm;
    }

    public TrackPoint ParseTrackPoint(string observationLine)
    {
        // Split one HURDAT2 observation into its individual fields.
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


        trackPoint.MaxSustainedWindKnots = int.Parse(fields[6].Trim());

        return trackPoint;
    }

    private static double ParseCoordinate(string coordinateText)
    {
        // Convert the HURDAT2 direction suffix into a signed coordinate.
        var trimmedCoordinate = coordinateText.Trim();

        var coordinateDirection = trimmedCoordinate[^1];
        var coordinateMagnitude = double.Parse(
            trimmedCoordinate[..^1],
            CultureInfo.InvariantCulture);

        var isNegative = coordinateDirection == 'S' || coordinateDirection == 'W';

        return isNegative ? -coordinateMagnitude : coordinateMagnitude;

    }

    public List<Storm> ParseFile(string filePath)
    {
        // Read the source file and pass its lines to the record parser.
        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines);
    }

    public List<Storm> ParseLines(IReadOnlyList<string> lines)
    {
        var storms = new List<Storm>();
        var fileIndex = 0;

        // Each header tells us how many observation lines belong to that storm.
        while (fileIndex < lines.Count)
        {
            var storm = ParseHeader(lines[fileIndex]);

            fileIndex++;

            for (var observationIndex = 0;
                observationIndex < storm.DeclaredObservationCount;
                observationIndex++)
            {
                storm.TrackPoints.Add(ParseTrackPoint(lines[fileIndex]));
                fileIndex++;
            }

            storms.Add(storm);
        }

        return storms;
    }
}
