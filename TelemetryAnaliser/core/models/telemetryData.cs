public class TelemetryData
{
    public DateTime Timestamp { get; set; }
    public string SimulatorName { get; set; }
    public CarData Car { get; set; }
    public TrackData Track { get; set; }
    public SessionData Session { get; set; }
}