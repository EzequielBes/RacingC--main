public class SessionData
{
    public SessionType Type { get; set; }
    public TimeSpan SessionTime { get; set; }
    public TimeSpan CurrentLapTime { get; set; }
    public TimeSpan BestLapTime { get; set; }
    public int CurrentLap { get; set; }
    public int Position { get; set; }
}

public enum SessionType
{
    Practice,
    Qualifying,
    Race,
    Hotlap
}