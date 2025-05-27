public interface ITelemetryProcessor
{
    Task<ProcessedTelemetryData> ProcessAsync(List<TelemetryData> rawData);
    Task<LapAnalysis> AnalyzeLapAsync(List<TelemetryData> lapData);
    Task<TrackMap> GenerateTrackMapAsync(List<TelemetryData> sessionData);
}