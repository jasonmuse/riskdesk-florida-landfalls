namespace RiskDesk.Api.Models;

public class TrackPoint
{
    public DateTimeOffset TimestampUtc { get; set; }
    public string Status { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int MaxSustainedWindKnots { get; set; }
    
}