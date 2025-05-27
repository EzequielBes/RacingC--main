public class iRacingConfig
{
    public static readonly Dictionary<string, string> TelemetryVariables = new()
    {
        ["Speed"] = "Speed",
        ["RPM"] = "RPM", 
        ["Gear"] = "Gear",
        ["Throttle"] = "Throttle",
        ["Brake"] = "Brake",
        ["SteeringWheelAngle"] = "SteeringWheelAngle",
        ["LapCurrentLapTime"] = "LapCurrentLapTime",
        ["LapBestLapTime"] = "LapBestLapTime",
        ["CarIdxLap"] = "CarIdxLap",
        ["CarIdxPosition"] = "CarIdxPosition",
        
        // Tire data
        ["LFtempCL"] = "LFtempCL", // Left Front Center Left
        ["LFtempCM"] = "LFtempCM", // Left Front Center Middle  
        ["LFtempCR"] = "LFtempCR", // Left Front Center Right
        
        // Track position
        ["CarIdxLapDistPct"] = "CarIdxLapDistPct", // % around track
    };

    public static readonly int UPDATE_FREQUENCY = 60; // Hz
    public static readonly int TIMEOUT_MS = 5000;
}