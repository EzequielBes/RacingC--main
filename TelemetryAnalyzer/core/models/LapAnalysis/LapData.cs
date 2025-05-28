using System;
using System.Collections.Generic;
using System.Numerics;

namespace TelemetryAnalyzer.Core.Models.LapAnalysis
{
    public class LapData
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public int LapNumber { get; set; }
        public TimeSpan LapTime { get; set; }
        public DateTime Timestamp { get; set; }
        public string DriverName { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public string TrackName { get; set; } = string.Empty;
        public string CarModel { get; set; } = string.Empty;
        public List<TelemetryData> TelemetryPoints { get; set; } = new();
        public List<SectorData> Sectors { get; set; } = new();
        public LapValidation Validation { get; set; } = new();
        public PerformanceMetrics Performance { get; set; } = new();
        public bool IsValid => Validation.IsValid;
        public bool IsPersonalBest { get; set; }
        public LapConditions Conditions { get; set; } = new();
    }

    public class SectorData
    {
        public int SectorNumber { get; set; }
        public TimeSpan SectorTime { get; set; }
        public float StartDistance { get; set; }
        public float EndDistance { get; set; }
        public List<TelemetryData> TelemetryPoints { get; set; } = new();
        public SectorAnalysis Analysis { get; set; } = new();
    }

    public class LapValidation
    {
        public bool IsValid { get; set; } = true;
        public List<string> Violations { get; set; } = new();
        public bool HasTrackLimitsViolation { get; set; }
        public bool HasCollision { get; set; }
        public bool HasInvalidSector { get; set; }
        public float TrackLimitsPercentage { get; set; }
    }

    public class PerformanceMetrics
    {
        public float MaxSpeed { get; set; }
        public float AverageSpeed { get; set; }
        public float MaxGForce { get; set; }
        public float MaxBrakeForce { get; set; }
        public float MaxThrottle { get; set; }
        public int TotalGearChanges { get; set; }
        public float FuelUsed { get; set; }
        public float AverageThrottlePosition { get; set; }
        public float AverageBrakePosition { get; set; }
        public float TotalBrakingTime { get; set; }
        public float TotalAcceleratingTime { get; set; }
        public float CoastingTime { get; set; }
        public TirePerformance TirePerformance { get; set; } = new();
    }

    public class TirePerformance
    {
        public float AverageTemperature { get; set; }
        public float MaxTemperature { get; set; }
        public float AveragePressure { get; set; }
        public float WearPercentage { get; set; }
        public float GripLevel { get; set; }
        public List<TireTemperatureData> TemperatureHistory { get; set; } = new();
    }

    public class TireTemperatureData
    {
        public float Distance { get; set; }
        public float[] Temperatures { get; set; } = new float[4]; // FL, FR, RL, RR
        public float[] Pressures { get; set; } = new float[4];
    }

    public class LapConditions
    {
        public float TrackTemperature { get; set; }
        public float AmbientTemperature { get; set; }
        public float WindSpeed { get; set; }
        public float WindDirection { get; set; }
        public WeatherType Weather { get; set; }
        public TrackSurfaceType Surface { get; set; }
        public float Grip { get; set; }
        public TimeOfDay TimeOfDay { get; set; }
    }

    public class SectorAnalysis
    {
        public TimeSpan IdealTime { get; set; }
        public TimeSpan TimeLost { get; set; }
        public List<PerformanceIssue> Issues { get; set; } = new();
        public float EfficiencyScore { get; set; } // 0-100
        public SectorMetrics Metrics { get; set; } = new();
    }

    public class SectorMetrics
    {
        public float AverageSpeed { get; set; }
        public float MaxSpeed { get; set; }
        public float MinSpeed { get; set; }
        public float BrakingDistance { get; set; }
        public float AccelerationTime { get; set; }
        public float CoastingTime { get; set; }
        public int GearChanges { get; set; }
        public float FuelUsed { get; set; }
        public List<CornerAnalysis> Corners { get; set; } = new();
    }

    public class CornerAnalysis
    {
        public int CornerNumber { get; set; }
        public string CornerName { get; set; } = string.Empty;
        public Vector3 ApexPosition { get; set; }
        public Vector3 BrakingPoint { get; set; }
        public Vector3 TurnInPoint { get; set; }
        public Vector3 ExitPoint { get; set; }
        public float EntrySpeed { get; set; }
        public float ApexSpeed { get; set; }
        public float ExitSpeed { get; set; }
        public float BrakingDistance { get; set; }
        public TimeSpan CornerTime { get; set; }
        public CornerType Type { get; set; }
        public float Radius { get; set; }
        public float OptimalLine { get; set; } // Distância da linha ideal
        public List<CornerIssue> Issues { get; set; } = new();
        public CornerRating Rating { get; set; }
    }

    public class CornerIssue
    {
        public CornerIssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public float Severity { get; set; } // 0-1
        public TimeSpan TimeLost { get; set; }
        public Vector3 Position { get; set; }
    }

    public class PerformanceIssue
    {
        public IssueType Type { get; set; }
        public string Description { get; set; } = string.Empty;
        public float Severity { get; set; } // 0-1 scale
        public TimeSpan TimeLost { get; set; }
        public Vector3 Position { get; set; }
        public string Suggestion { get; set; } = string.Empty;
    }

    // Enums
    public enum WeatherType
    {
        Clear,
        Cloudy,
        LightRain,
        HeavyRain,
        Thunderstorm,
        Fog
    }

    public enum TrackSurfaceType
    {
        Dry,
        Damp,
        Wet,
        Flooded
    }

    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Evening,
        Night
    }

    public enum CornerType
    {
        Hairpin,
        Slow,
        Medium,
        Fast,
        Chicane,
        Sweeper
    }

    public enum CornerIssueType
    {
        LateApex,
        EarlyApex,
        LateBraking,
        EarlyBraking,
        PoorExit,
        WideEntry,
        OffLine,
        LostTraction,
        Understeer,
        Oversteer
    }

    public enum IssueType
    {
        BrakingPoint,
        Acceleration,
        CorneringLine,
        GearChange,
        Throttle,
        Brake,
        TrackLimits,
        Setup,
        Tires,
        Fuel
    }

    public enum CornerRating
    {
        Excellent,
        Good,
        Average,
        Poor,
        Terrible
    }
}