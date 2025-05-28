using System.Collections.Generic;
using System.Numerics;

namespace TelemetryAnalyzer.Core.Models
{
    public class TrackData
    {
        public string Name { get; set; }
        public float Length { get; set; }
        public List<Vector3> TrackLayoutPoints { get; set; } // Assuming Vector3 represents points on the track
        // Add other track details
    }
}

