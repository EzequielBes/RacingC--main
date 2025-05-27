using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Application.UseCases
{
    public class AnalyzeLapUseCase
    {
        private readonly ITelemetryRepository _repository;
        private readonly LapAnalysisService _lapAnalysisService;
        private readonly LapComparisonService _comparisonService;
        private readonly TrackMapService _trackMapService;
        private readonly ILogger<AnalyzeLapUseCase> _logger;

        public AnalyzeLapUseCase(
            ITelemetryRepository repository,
            LapAnalysisService lapAnalysisService,
            LapComparisonService comparisonService,
            TrackMapService trackMapService,
            ILogger<AnalyzeLapUseCase> logger)
        {
            _repository = repository;
            _lapAnalysisService = lapAnalysisService;
            _comparisonService = comparisonService;
            _trackMapService = trackMapService;
            _logger = logger;
        }

        public async Task<LapAnalysisResult> AnalyzeLapAsync(Guid sessionId, int lapNumber)
        {
            try
            {
                _logger.LogInformation($"Starting lap analysis for session {sessionId}, lap {lapNumber}");

                // Get session data
                var session = await _repository.GetSessionAsync(sessionId);
                if (session == null)
                {
                    return LapAnalysisResult.Failure("Session not found");
                }

                // Find the specific lap
                var lapData = session.Data?.Laps?.FirstOrDefault(l => l.LapNumber == lapNumber);
                if (lapData == null)
                {
                    return LapAnalysisResult.Failure($"Lap {lapNumber} not found in session");
                }

                // Perform detailed analysis
                var detailedLap = await _lapAnalysisService.CreateLapDataAsync(lapData.Data);
                
                // Analyze corners
                var cornerAnalyses = await _lapAnalysisService.AnalyzeCornersAsync(lapData.Data);
                
                // Generate track map for this lap
                var trackMap = await _trackMapService.GenerateTrackMapAsync(lapData.Data);

                var result = new LapAnalysisResult
                {
                    IsSuccess = true,
                    LapData = detailedLap,
                    CornerAnalyses = cornerAnalyses,
                    TrackMap = trackMap,
                    AnalysisTimestamp = DateTime.Now
                };

                _logger.LogInformation($"Completed lap analysis for session {sessionId}, lap {lapNumber}");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing lap {lapNumber} in session {sessionId}");
                return LapAnalysisResult.Failure($"Analysis error: {ex.Message}");
            }
        }

        public async Task<LapComparisonResult> CompareLapsAsync(
            Guid session1Id, int lap1Number,
            Guid session2Id, int lap2Number)
        {
            try
            {
                _logger.LogInformation($"Starting lap comparison: Session {session1Id} Lap {lap1Number} vs Session {session2Id} Lap {lap2Number}");

                // Analyze both laps
                var analysis1 = await AnalyzeLapAsync(session1Id, lap1Number);
                var analysis2 = await AnalyzeLapAsync(session2Id, lap2Number);

                if (!analysis1.IsSuccess)
                    return LapComparisonResult.Failure($"Failed to analyze first lap: {analysis1.ErrorMessage}");
                
                if (!analysis2.IsSuccess)
                    return LapComparisonResult.Failure($"Failed to analyze second lap: {analysis2.ErrorMessage}");

                // Perform comparison
                var comparison = await _comparisonService.CompareLapsAsync(analysis1.LapData, analysis2.LapData);

                // Generate tracking lines for visualization
                var trackingLines = await _trackMapService.CompareTrackingLinesAsync(
                    analysis1.LapData.TelemetryPoints,
                    analysis2.LapData.TelemetryPoints);

                var result = new LapComparisonResult
                {
                    IsSuccess = true,
                    Comparison = comparison,
                    TrackingLines = trackingLines,
                    ComparisonTimestamp = DateTime.Now
                };

                _logger.LogInformation($"Completed lap comparison");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing laps");
                return LapComparisonResult.Failure($"Comparison error: {ex.Message}");
            }
        }

        public async Task<List<LapSummary>> GetSessionLapSummariesAsync(Guid sessionId)
        {
            try
            {
                var session = await _repository.GetSessionAsync(sessionId);
                if (session?.Data?.Laps == null)
                    return new List<LapSummary>();

                var summaries = new List<LapSummary>();

                foreach (var lap in session.Data.Laps.OrderBy(l => l.LapNumber))
                {
                    var summary = new LapSummary
                    {
                        SessionId = sessionId,
                        LapNumber = lap.LapNumber,
                        LapTime = lap.LapTime,
                        IsValid = lap.IsValid,
                        IsPersonalBest = lap.IsPersonalBest,
                        MaxSpeed = lap.Data?.Max(d => d.Car?.Speed ?? 0) ?? 0,
                        AverageSpeed = lap.Data?.Average(d => d.Car?.Speed ?? 0) ?? 0,
                        DataPointCount = lap.Data?.Count ?? 0
                    };

                    summaries.Add(summary);
                }

                // Mark actual personal best
                var validLaps = summaries.Where(s => s.IsValid && s.LapTime > TimeSpan.Zero).ToList();
                if (validLaps.Any())
                {
                    var fastestLap = validLaps.OrderBy(l => l.LapTime).First();
                    fastestLap.IsPersonalBest = true;
                }

                return summaries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting lap summaries for session {sessionId}");
                return new List<LapSummary>();
            }
        }

        public async Task<PerformanceTrendsResult> AnalyzePerformanceTrendsAsync(Guid sessionId)
        {
            try
            {
                var lapSummaries = await GetSessionLapSummariesAsync(sessionId);
                if (!lapSummaries.Any())
                {
                    return new PerformanceTrendsResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "No lap data available for trend analysis"
                    };
                }

                var validLaps = lapSummaries.Where(l => l.IsValid && l.LapTime > TimeSpan.Zero).ToList();
                
                var trends = new PerformanceTrends
                {
                    LapCount = validLaps.Count,
                    FastestLap = validLaps.OrderBy(l => l.LapTime).FirstOrDefault(),
                    SlowestLap = validLaps.OrderByDescending(l => l.LapTime).FirstOrDefault(),
                    AverageLapTime = TimeSpan.FromTicks((long)validLaps.Average(l => l.LapTime.Ticks)),
                    LapTimeStandardDeviation = CalculateStandardDeviation(validLaps.Select(l => l.LapTime.TotalSeconds)),
                    ConsistencyScore = CalculateConsistencyScore(validLaps),
                    ImprovementTrend = CalculateImprovementTrend(validLaps)
                };

                return new PerformanceTrendsResult
                {
                    IsSuccess = true,
                    Trends = trends
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error analyzing performance trends for session {sessionId}");
                return new PerformanceTrendsResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Trend analysis error: {ex.Message}"
                };
            }
        }

        private double CalculateStandardDeviation(IEnumerable<double> values)
        {
            var enumerable = values.ToList();
            var average = enumerable.Average();
            var sumOfSquaresOfDifferences = enumerable.Select(val => (val - average) * (val - average)).Sum();
            return Math.Sqrt(sumOfSquaresOfDifferences / enumerable.Count);
        }

        private float CalculateConsistencyScore(List<LapSummary> laps)
        {
            if (laps.Count < 2) return 100f;

            var lapTimes = laps.Select(l => l.LapTime.TotalSeconds).ToList();
            var average = lapTimes.Average();
            var maxDeviation = lapTimes.Max(t => Math.Abs(t - average));
            
            // Score based on how close laps are to each other (lower deviation = higher score)
            var consistencyPercentage = Math.Max(0, 100 - (maxDeviation / average * 100));
            return (float)consistencyPercentage;
        }

        private TrendDirection CalculateImprovementTrend(List<LapSummary> laps)
        {
            if (laps.Count < 3) return TrendDirection.Stable;

            var recentLaps = laps.TakeLast(5).ToList();
            var earlyLaps = laps.Take(5).ToList();

            var recentAverage = recentLaps.Average(l => l.LapTime.TotalSeconds);
            var earlyAverage = earlyLaps.Average(l => l.LapTime.TotalSeconds);

            var improvementThreshold = 0.5; // 0.5 seconds

            if (recentAverage < earlyAverage - improvementThreshold)
                return TrendDirection.Improving;
            else if (recentAverage > earlyAverage + improvementThreshold)
                return TrendDirection.Declining;
            else
                return TrendDirection.Stable;
        }
    }

    // Result classes
    public class LapAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public LapData LapData { get; set; }
        public List<CornerAnalysis> CornerAnalyses { get; set; } = new();
        public TrackMap TrackMap { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
        public string ErrorMessage { get; set; }

        public static LapAnalysisResult Failure(string errorMessage)
        {
            return new LapAnalysisResult { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }

    public class LapComparisonResult
    {
        public bool IsSuccess { get; set; }
        public LapComparison Comparison { get; set; }
        public List<TrackingLine> TrackingLines { get; set; } = new();
        public DateTime ComparisonTimestamp { get; set; }
        public string ErrorMessage { get; set; }

        public static LapComparisonResult Failure(string errorMessage)
        {
            return new LapComparisonResult { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }

    public class LapSummary
    {
        public Guid SessionId { get; set; }
        public int LapNumber { get; set; }
        public TimeSpan LapTime { get; set; }
        public bool IsValid { get; set; }
        public bool IsPersonalBest { get; set; }
        public float MaxSpeed { get; set; }
        public float AverageSpeed { get; set; }
        public int DataPointCount { get; set; }
    }

    public class PerformanceTrendsResult
    {
        public bool IsSuccess { get; set; }
        public PerformanceTrends Trends { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class PerformanceTrends
    {
        public int LapCount { get; set; }
        public LapSummary FastestLap { get; set; }
        public LapSummary SlowestLap { get; set; }
        public TimeSpan AverageLapTime { get; set; }
        public double LapTimeStandardDeviation { get; set; }
        public float ConsistencyScore { get; set; }
        public TrendDirection ImprovementTrend { get; set; }
    }

    public enum TrendDirection
    {
        Improving,
        Stable,
        Declining
    }
}