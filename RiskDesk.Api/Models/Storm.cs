namespace RiskDesk.Api.Models;

public class Storm
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int DeclaredObservationCount { get; set; }
    public List<TrackPoint> TrackPoints { get; set; } = [];

}