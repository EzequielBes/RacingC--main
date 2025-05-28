using System;

namespace TelemetryAnalyzer.Core.Models
{
    public class SessionData
    {
        public string TrackName { get; set; }
        public string CarModel { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public TimeSpan BestLapTime { get; set; }
        public TimeSpan AverageLapTime { get; set; }
        // Add other session details
    }

    public enum SessionType
    {
        Practice,
        Qualifying,
        Race,
        Hotlap
    }
}

