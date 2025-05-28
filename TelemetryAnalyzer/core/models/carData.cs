using System.Numerics;

namespace TelemetryAnalyzer.Core.Models
{
    public class CarData
    {
        public string Model { get; set; }
        public float MaxRpm { get; set; }
        public float IdleRpm { get; set; }
        // Add other car specific data
        public Vector3 Position { get; set; }
        public Vector3 Velocity { get; set; }
        public Vector3 Acceleration { get; set; }
        // Tire data might be complex, consider a separate TireData class or structure
        public float[] TirePressures { get; set; } // Example: FrontLeft, FrontRight, RearLeft, RearRight
        public float[] TireTemperatures { get; set; }
    }
}

