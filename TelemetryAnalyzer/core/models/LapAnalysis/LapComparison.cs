using System;
using System.Collections.Generic;
using System.Numerics;

namespace TelemetryAnalyzer.Core.Models.LapAnalysis
{
    public class LapComparison
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public LapData ReferenceLap { get; set; } = new();
        public LapData ComparisonLap { get; set; } = new();
        public ComparisonResults Results { get; set; } = new();
        public List<ComparisonPoint> TimeDeltas { get; set; } = new();
        public List<SectorComparison> SectorComparisons { get; set; } = new();
        public List<CornerComparison> CornerComparisons { get; set; } = new();
        public OverallAnalysis OverallAnalysis { get; set; } = new();
    }

    public class ComparisonResults
    {
        public TimeSpan TimeDifference { get; set; }
        public float SpeedDifference { get; set; }
        public ComparisonSummary Summary { get; set; } = new();
        public List<ImprovementArea> ImprovementAreas { get; set; } = new();
        public PerformanceBreakdown Breakdown { get; set; } = new();
    }

    public class ComparisonPoint
    {
        public float Distance { get; set; }
        public TimeSpan TimeDelta { get; set; }
        public float SpeedDifference { get; set; }
        public Vector3 Position { get; set; }
        public ComparisonMetrics Metrics { get; set; } = new();
    }

    public class ComparisonMetrics
    {
        public float ThrottleDifference { get; set; }
        public float BrakeDifference { get; set; }
        public float SteeringDifference { get; set; }
        public int GearDifference { get; set; }
        public float LineDifference { get; set; } // Distance from ideal line
    }

    public class SectorComparison
    {
        public int SectorNumber { get; set; }
        public TimeSpan TimeDifference { get; set; }
        public float SpeedDifference { get; set; }
        public SectorAnalysis ReferenceAnalysis { get; set; } = new();
        public SectorAnalysis ComparisonAnalysis { get; set; } = new();
        public List<ImprovementSuggestion> Suggestions { get; set; } = new();
    }

    public class CornerComparison
    {
        public int CornerNumber { get; set; }
        public string CornerName { get; set; } = string.Empty;
        public TimeSpan TimeDifference { get; set; }
        public CornerAnalysis Reference { get; set; } = new();
        public CornerAnalysis Comparison { get; set; } = new();
        public CornerDifferences Differences { get; set; } = new();
        public List<CornerImprovement> ImprovementSuggestions { get; set; } = new();
    }

    public class CornerDifferences
    {
        public float EntrySpeedDiff { get; set; }
        public float ApexSpeedDiff { get; set; }
        public float ExitSpeedDiff { get; set; }
        public float BrakingPointDiff { get; set; }
        public float LinePositionDiff { get; set; }
        public Vector3 ApexPositionDiff { get; set; }
    }

    public class CornerImprovement
    {
        public CornerPhase Phase { get; set; }
        public string Description { get; set; } = string.Empty;
        public TimeSpan PotentialGain { get; set; }
        public float Confidence { get; set; } // 0-1
        public ImprovementPriority Priority { get; set; }
    }

    public class ComparisonSummary
    {
        public string FasterDriver { get; set; } = string.Empty;
        public TimeSpan LapTimeDifference { get; set; }
        public float PercentageDifference { get; set; }
        public List<string> KeyDifferences { get; set; } = new();
        public ComparisonCategory Category { get; set; }
    }

    public class ImprovementArea
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TimeSpan PotentialGain { get; set; }
        public ImprovementPriority Priority { get; set; }
        public float Confidence { get; set; } // 0-1
        public List<Vector3> Positions { get; set; } = new();
        public List<ImprovementSuggestion> Suggestions { get; set; } = new();
    }

    public class ImprovementSuggestion
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public ImprovementPriority Priority { get; set; }
        public TimeSpan ExpectedGain { get; set; }
        public float Difficulty { get; set; } // 0-1, 1 being hardest
        public List<string> Steps { get; set; } = new();
    }

    public class PerformanceBreakdown
    {
        public TimeSpan BrakingTimeDiff { get; set; }
        public TimeSpan AccelerationTimeDiff { get; set; }
        public TimeSpan CorneringTimeDiff { get; set; }
        public TimeSpan StraightLineTimeDiff { get; set; }
        public Dictionary<string, TimeSpan> CategoryBreakdown { get; set; } = new();
    }

    public class OverallAnalysis
    {
        public StrengthsAndWeaknesses Analysis { get; set; } = new();
        public List<KeyInsight> KeyInsights { get; set; } = new();
        public TrainingRecommendations Recommendations { get; set; } = new();
        public float OverallScore { get; set; } // 0-100
    }

    public class StrengthsAndWeaknesses
    {
        public List<string> Strengths { get; set; } = new();
        public List<string> Weaknesses { get; set; } = new();
        public List<string> NeutralAreas { get; set; } = new();
    }

    public class KeyInsight
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public InsightType Type { get; set; }
        public float Impact { get; set; } // 0-1
        public List<Vector3> RelevantPositions { get; set; } = new();
    }

    public class TrainingRecommendations
    {
        public List<string> ImmediateFocus { get; set; } = new();
        public List<string> MediumTermGoals { get; set; } = new();
        public List<string> LongTermDevelopment { get; set; } = new();
        public List<SetupSuggestion> SetupRecommendations { get; set; } = new();
    }

    public class SetupSuggestion
    {
        public string Component { get; set; } = string.Empty;
        public string Adjustment { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public float Confidence { get; set; } // 0-1
    }

    // Enums
    public enum CornerPhase
    {
        Entry,
        Apex,
        Exit,
        BrakingZone,
        AccelerationZone
    }

    public enum ImprovementPriority
    {
        Critical,
        High,
        Medium,
        Low
    }

    public enum ComparisonCategory
    {
        VeryClose,      // < 0.5s
        Close,          // 0.5-1.0s
        Moderate,       // 1.0-2.0s
        Significant,    // 2.0-5.0s
        Large           // > 5.0s
    }

    public enum InsightType
    {
        Strength,
        Weakness,
        Opportunity,
        Pattern,
        Anomaly
    }
}