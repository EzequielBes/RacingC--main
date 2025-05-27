using System;
using System.Collections.Generic;

namespace TelemetryAnalyzer.Core.Models
{
    public class TelemetrySession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime ImportedAt { get; set; } = DateTime.Now;
        public ProcessedTelemetryData Data { get; set; } = new();
        public string Source { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public List<TelemetryDataPoint> DataPoints { get; set; } = new();
    }

    public class TelemetryDataPoint
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; }
        public string JsonData { get; set; } = string.Empty; // Dados serializados
        public TelemetrySession Session { get; set; }
    }

    public class ProcessedTelemetryData
    {
        public List<TelemetryData> RawData { get; set; } = new();
        public List<LapData> Laps { get; set; } = new();
        public TrackMap TrackMap { get; set; } = new();
        public SessionStatistics Statistics { get; set; } = new();
    }

    public class SessionStatistics
    {
        public int TotalLaps { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public TimeSpan FastestLap { get; set; }
        public float AverageSpeed { get; set; }
        public float MaxSpeed { get; set; }
        public int ValidLaps { get; set; }
        public float ConsistencyScore { get; set; }
    }

    public class LapData
    {
        public int LapNumber { get; set; }
        public TimeSpan LapTime { get; set; }
        public List<TelemetryData> Data { get; set; } = new();
        public bool IsValid { get; set; } = true;
        public bool IsPersonalBest { get; set; }
    }
}
