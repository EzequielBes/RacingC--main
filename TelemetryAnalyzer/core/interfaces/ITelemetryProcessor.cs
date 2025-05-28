using System.Collections.Generic;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models; // Assuming custom types are here

namespace TelemetryAnalyzer.Core.Interfaces
{
    public interface ITelemetryProcessor
    {
        Task<ProcessedTelemetryData> ProcessRawDataAsync(List<TelemetryData> rawData);
        Task<LapAnalysis> AnalyzeLapAsync(List<TelemetryData> lapData);
        Task<TrackMap> GenerateTrackMapAsync(List<TelemetryData> sessionData);
        // Add other processing methods as needed
    }
}

