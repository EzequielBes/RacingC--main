public interface IFileImporter
{
    bool CanImport(string filePath);
    Task<List<TelemetryData>> ImportAsync(string filePath);
    string[] SupportedExtensions { get; }
}