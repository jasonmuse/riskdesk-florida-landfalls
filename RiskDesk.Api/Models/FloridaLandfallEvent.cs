namespace RiskDesk.Api.Models;

public class FloridaLandfallEvent
{
    public string StormId { get; set; } = string.Empty;
    public string StormName { get; set; } = string.Empty;
    public DateTimeOffset LandfallTimeUtc { get; set; }
    public int LandfallWindSpeedKnots { get; set; }
    public int MaxFloridaWindSpeedKnots { get; set; }

}