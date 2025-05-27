using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Application.UseCases
{
    public class ImportTelemetryUseCase
    {
        private readonly IEnumerable<IFileImporter> _importers;
        private readonly ITelemetryProcessor _processor;
        private readonly ITelemetryRepository _repository;
        private readonly ILogger<ImportTelemetryUseCase> _logger;

        public ImportTelemetryUseCase(
            IEnumerable<IFileImporter> importers,
            ITelemetryProcessor processor,
            ITelemetryRepository repository,
            ILogger<ImportTelemetryUseCase> logger)
        {
            _importers = importers;
            _processor = processor;
            _repository = repository;
            _logger = logger;
        }

        public async Task<ImportResult> ImportFileAsync(string filePath)
        {
            try
            {
                _logger.LogInformation($"Starting import of file: {filePath}");

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    return ImportResult.Failure($"File not found: {filePath}");
                }

                // Find appropriate importer
                var importer = _importers.FirstOrDefault(i => i.CanImport(filePath));
                if (importer == null)
                {
                    var extension = Path.GetExtension(filePath);
                    return ImportResult.Failure($"No importer available for file type: {extension}");
                }

                // Import telemetry data
                var rawData = await importer.ImportAsync(filePath);
                if (!rawData.Any())
                {
                    return ImportResult.Failure("No telemetry data found in file");
                }

                _logger.LogInformation($"Imported {rawData.Count} data points from {filePath}");

                // Process the data
                var processedData = await _processor.ProcessAsync(rawData);

                // Create session
                var session = new TelemetrySession
                {
                    Id = Guid.NewGuid(),
                    Name = Path.GetFileNameWithoutExtension(filePath),
                    ImportedAt = DateTime.Now,
                    Data = processedData,
                    Source = $"Imported from {Path.GetFileName(filePath)}",
                    FilePath = filePath,
                    Duration = CalculateSessionDuration(rawData)
                };

                // Save to repository
                await _repository.SaveSessionAsync(session);

                _logger.LogInformation($"Successfully imported and saved session: {session.Name}");
                return ImportResult.Success(session.Id, $"Imported {rawData.Count} data points");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing file: {filePath}");
                return ImportResult.Failure($"Import error: {ex.Message}");
            }
        }

        public async Task<ImportResult> ImportMultipleFilesAsync(string[] filePaths, string sessionName = null)
        {
            try
            {
                var allTelemetryData = new List<TelemetryData>();
                var importedFiles = new List<string>();

                foreach (var filePath in filePaths)
                {
                    var result = await ImportSingleFileDataAsync(filePath);
                    if (result.IsSuccess)
                    {
                        allTelemetryData.AddRange(result.Data);
                        importedFiles.Add(Path.GetFileName(filePath));
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to import {filePath}: {result.ErrorMessage}");
                    }
                }

                if (!allTelemetryData.Any())
                {
                    return ImportResult.Failure("No valid telemetry data found in any of the files");
                }

                // Sort by timestamp
                allTelemetryData = allTelemetryData.OrderBy(d => d.Timestamp).ToList();

                // Process combined data
                var processedData = await _processor.ProcessAsync(allTelemetryData);

                // Create combined session
                var session = new TelemetrySession
                {
                    Id = Guid.NewGuid(),
                    Name = sessionName ?? $"Combined Session - {DateTime.Now:yyyy-MM-dd HH:mm}",
                    ImportedAt = DateTime.Now,
                    Data = processedData,
                    Source = $"Imported from {importedFiles.Count} files: {string.Join(", ", importedFiles)}",
                    Duration = CalculateSessionDuration(allTelemetryData)
                };

                await _repository.SaveSessionAsync(session);

                _logger.LogInformation($"Successfully imported combined session from {importedFiles.Count} files");
                return ImportResult.Success(session.Id, $"Imported {allTelemetryData.Count} data points from {importedFiles.Count} files");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing multiple files");
                return ImportResult.Failure($"Import error: {ex.Message}");
            }
        }

        public async Task<ImportResult> ImportFromUrlAsync(string url)
        {
            try
            {
                _logger.LogInformation($"Starting import from URL: {url}");

                // Download file to temp location
                var tempFilePath = await DownloadFileAsync(url);

                try
                {
                    // Import the downloaded file
                    var result = await ImportFileAsync(tempFilePath);
                    
                    if (result.IsSuccess)
                    {
                        // Update source information
                        var session = await _repository.GetSessionAsync(result.SessionId);
                        if (session != null)
                        {
                            session.Source = $"Downloaded from {url}";
                            await _repository.UpdateSessionAsync(session);
                        }
                    }

                    return result;
                }
                finally
                {
                    // Clean up temp file
                    if (File.Exists(tempFilePath))
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing from URL: {url}");
                return ImportResult.Failure($"URL import error: {ex.Message}");
            }
        }

        private async Task<SingleFileImportResult> ImportSingleFileDataAsync(string filePath)
        {
            try
            {
                var importer = _importers.FirstOrDefault(i => i.CanImport(filePath));
                if (importer == null)
                {
                    return new SingleFileImportResult 
                    { 
                        IsSuccess = false, 
                        ErrorMessage = $"No importer for {Path.GetExtension(filePath)}" 
                    };
                }

                var data = await importer.ImportAsync(filePath);
                return new SingleFileImportResult 
                { 
                    IsSuccess = true, 
                    Data = data 
                };
            }
            catch (Exception ex)
            {
                return new SingleFileImportResult 
                { 
                    IsSuccess = false, 
                    ErrorMessage = ex.Message 
                };
            }
        }

        private async Task<string> DownloadFileAsync(string url)
        {
            using var httpClient = new System.Net.Http.HttpClient();
            var response = await httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var tempFilePath = Path.GetTempFileName();
            var content = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(tempFilePath, content);

            return tempFilePath;
        }

        private TimeSpan CalculateSessionDuration(List<TelemetryData> data)
        {
            if (data.Count < 2) return TimeSpan.Zero;
            
            var sortedData = data.OrderBy(d => d.Timestamp).ToList();
            return sortedData.Last().Timestamp - sortedData.First().Timestamp;
        }

        public List<string> GetSupportedExtensions()
        {
            return _importers.SelectMany(i => i.SupportedExtensions).Distinct().ToList();
        }

        public async Task<ValidationResult> ValidateFileAsync(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return new ValidationResult { IsValid = false, Message = "File not found" };
                }

                var importer = _importers.FirstOrDefault(i => i.CanImport(filePath));
                if (importer == null)
                {
                    return new ValidationResult 
                    { 
                        IsValid = false, 
                        Message = $"Unsupported file type: {Path.GetExtension(filePath)}" 
                    };
                }

                // Try to read a small sample to validate format
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    return new ValidationResult { IsValid = false, Message = "File is empty" };
                }

                if (fileInfo.Length > 500 * 1024 * 1024) // 500MB limit
                {
                    return new ValidationResult 
                    { 
                        IsValid = false, 
                        Message = "File too large (maximum 500MB)" 
                    };
                }

                return new ValidationResult { IsValid = true, Message = "File is valid" };
            }
            catch (Exception ex)
            {
                return new ValidationResult 
                { 
                    IsValid = false, 
                    Message = $"Validation error: {ex.Message}" 
                };
            }
        }
    }

    public class ImportResult
    {
        public bool IsSuccess { get; set; }
        public Guid SessionId { get; set; }
        public string Message { get; set; }
        public string ErrorMessage { get; set; }

        public static ImportResult Success(Guid sessionId, string message = null)
        {
            return new ImportResult 
            { 
                IsSuccess = true, 
                SessionId = sessionId, 
                Message = message 
            };
        }

        public static ImportResult Failure(string errorMessage)
        {
            return new ImportResult 
            { 
                IsSuccess = false, 
                ErrorMessage = errorMessage 
            };
        }
    }

    public class SingleFileImportResult
    {
        public bool IsSuccess { get; set; }
        public List<TelemetryData> Data { get; set; } = new();
        public string ErrorMessage { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }
}

// Application/UseCases/AnalyzeLapUseCase.cs
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