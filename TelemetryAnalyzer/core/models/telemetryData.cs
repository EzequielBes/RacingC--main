using System;

namespace TelemetryAnalyzer.Core.Models
{
    public class TelemetryData
    {
        public DateTime Timestamp { get; set; }
        public float Speed { get; set; }
        public float Throttle { get; set; }
        public float Brake { get; set; }
        public float Clutch { get; set; }
        public float SteeringAngle { get; set; }
        public int Gear { get; set; }
        // Add other relevant telemetry points as needed
        public System.Numerics.Vector3 Position { get; set; } // Assuming Vector3 is needed here too
    }
}

