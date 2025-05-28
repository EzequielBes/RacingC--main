using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;
// Movido de dentro do namespace TelemetryAnalyzer.Application.UseCases para o topo do arquivo
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

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

// O conteúdo de AnalyzeLapUseCase.cs foi removido deste arquivo pois estava concatenado incorretamente.
// Certifique-se de que AnalyzeLapUseCase.cs exista como um arquivo separado e correto.

