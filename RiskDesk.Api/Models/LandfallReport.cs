namespace RiskDesk.Api.Models;

public class LandfallReport
{
    public int StormCount { get; set; }
    public int LandfallCount { get; set; }
    public List<FloridaLandfallEvent> Events { get; set; } = [];
}