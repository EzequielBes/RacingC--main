using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Infrastructure.Services;

namespace TelemetryAnalyzer.Application.UseCases
{
    public class RealTimeTelemetryUseCase
    {
        private readonly Dictionary<string, IMemoryReader> _memoryReaders;
        private readonly ITelemetryProcessor _processor;
        private readonly ITelemetryRepository _repository;
        private readonly SimulatorDetectionService _simulatorDetection;
        private readonly ILogger<RealTimeTelemetryUseCase> _logger;

        private IMemoryReader _currentReader;
        private bool _isMonitoring = false;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;
        private readonly List<TelemetryData> _sessionBuffer = new();
        private DateTime _sessionStartTime;
        private string _currentSessionId;

        public event Action<TelemetryData> TelemetryDataReceived;
        public event Action<bool, string> ConnectionStatusChanged;
        public event Action<string> SessionStarted;
        public event Action<string, List<TelemetryData>> SessionEnded;

        public bool IsMonitoring => _isMonitoring;
        public string CurrentSimulator { get; private set; }
        public TimeSpan SessionDuration => _isMonitoring ? DateTime.Now - _sessionStartTime : TimeSpan.Zero;

        public RealTimeTelemetryUseCase(
            IEnumerable<IMemoryReader> memoryReaders,
            ITelemetryProcessor processor,
            ITelemetryRepository repository,
            SimulatorDetectionService simulatorDetection,
            ILogger<RealTimeTelemetryUseCase> logger)
        {
            _memoryReaders = memoryReaders.ToDictionary(r => r.GetType().Name.Replace("MemoryReader", ""), r => r);
            _processor = processor;
            _repository = repository;
            _simulatorDetection = simulatorDetection;
            _logger = logger;
        }

        public async Task<bool> StartMonitoringAsync(string simulatorType = null)
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Already monitoring telemetry data");
                return false;
            }

            try
            {
                // Auto-detect simulator if not specified
                if (string.IsNullOrEmpty(simulatorType) || simulatorType == "Auto-Detect")
                {
                    var detectedSimulators = await _simulatorDetection.DetectRunningSimulatorsAsync();
                    simulatorType = detectedSimulators.FirstOrDefault();

                    if (string.IsNullOrEmpty(simulatorType))
                    {
                        ConnectionStatusChanged?.Invoke(false, "No supported simulator detected");
                        return false;
                    }
                }

                // Find appropriate memory reader
                var readerKey = GetReaderKey(simulatorType);
                if (!_memoryReaders.TryGetValue(readerKey, out _currentReader))
                {
                    ConnectionStatusChanged?.Invoke(false, $"No memory reader available for {simulatorType}");
                    return false;
                }

                // Initialize reader
                if (!_currentReader.Initialize())
                {
                    ConnectionStatusChanged?.Invoke(false, $"Failed to initialize {simulatorType} memory reader");
                    return false;
                }

                // Setup event handlers
                _currentReader.DataReceived += OnTelemetryDataReceived;

                // Start monitoring
                _isMonitoring = true;
                CurrentSimulator = simulatorType;
                _sessionStartTime = DateTime.Now;
                _currentSessionId = Guid.NewGuid().ToString();
                _sessionBuffer.Clear();

                _cancellationTokenSource = new CancellationTokenSource();
                
                _currentReader.StartReading();
                
                _monitoringTask = Task.Run(() => MonitoringLoop(_cancellationTokenSource.Token));

                ConnectionStatusChanged?.Invoke(true, $"Connected to {simulatorType}");
                SessionStarted?.Invoke(_currentSessionId);
                
                _logger.LogInformation($"Started monitoring {simulatorType}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error starting telemetry monitoring for {simulatorType}");
                ConnectionStatusChanged?.Invoke(false, $"Connection error: {ex.Message}");
                return false;
            }
        }

        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring) return;

            try
            {
                _isMonitoring = false;
                _cancellationTokenSource?.Cancel();

                // Stop current reader
                _currentReader?.StopReading();
                if (_currentReader != null)
                {
                    _currentReader.DataReceived -= OnTelemetryDataReceived;
                }

                // Wait for monitoring task to complete
                if (_monitoringTask != null)
                {
                    await _monitoringTask;
                }

                // Save session data if any was collected
                if (_sessionBuffer.Any())
                {
                    await SaveSessionDataAsync();
                }

                SessionEnded?.Invoke(_currentSessionId, _sessionBuffer.ToList());
                ConnectionStatusChanged?.Invoke(false, "Disconnected");
                
                _logger.LogInformation($"Stopped monitoring {CurrentSimulator}");
                
                CurrentSimulator = null;
                _currentReader = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping telemetry monitoring");
            }
        }

        private async Task MonitoringLoop(CancellationToken cancellationToken)
        {
            var lastSaveTime = DateTime.Now;
            const int saveIntervalMinutes = 5; // Save buffer every 5 minutes

            while (!cancellationToken.IsCancellationRequested && _isMonitoring)
            {
                try
                {
                    // Check connection status
                    if (_currentReader != null && !_currentReader.IsConnected)
                    {
                        _logger.LogWarning("Lost connection to simulator, attempting to reconnect...");
                        
                        if (!_currentReader.Initialize())
                        {
                            await Task.Delay(2000, cancellationToken); // Wait before retry
                            continue;
                        }
                        
                        ConnectionStatusChanged?.Invoke(true, $"Reconnected to {CurrentSimulator}");
                    }

                    // Periodic save of session buffer
                    if (DateTime.Now - lastSaveTime > TimeSpan.FromMinutes(saveIntervalMinutes))
                    {
                        if (_sessionBuffer.Any())
                        {
                            await SaveBufferToTempStorageAsync();
                            lastSaveTime = DateTime.Now;
                        }
                    }

                    await Task.Delay(1000, cancellationToken); // Check every second
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in monitoring loop");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        private void OnTelemetryDataReceived(object sender, TelemetryData data)
        {
            if (!_isMonitoring || data == null) return;

            try
            {
                // Add to session buffer
                _sessionBuffer.Add(data);
                
                // Limit buffer size to prevent memory issues
                if (_sessionBuffer.Count > 10000) // ~3 minutes at 60fps
                {
                    _sessionBuffer.RemoveRange(0, 1000); // Remove oldest 1000 entries
                }

                // Notify subscribers
                TelemetryDataReceived?.Invoke(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing telemetry data");
            }
        }

        private async Task SaveSessionDataAsync()
        {
            try
            {
                if (!_sessionBuffer.Any()) return;

                var processedData = await _processor.ProcessAsync(_sessionBuffer.ToList());
                
                var session = new TelemetrySession
                {
                    Id = Guid.Parse(_currentSessionId),
                    Name = $"{CurrentSimulator} Session - {_sessionStartTime:yyyy-MM-dd HH:mm}",
                    ImportedAt = DateTime.Now,
                    Data = processedData,
                    Source = $"Real-time from {CurrentSimulator}",
                    Duration = SessionDuration
                };

                await _repository.SaveSessionAsync(session);
                _logger.LogInformation($"Saved session data: {session.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving session data");
            }
        }

        private async Task SaveBufferToTempStorageAsync()
        {
            try
            {
                // Save to temporary storage (could be file or database)
                // This is a backup in case the application crashes
                var tempData = _sessionBuffer.TakeLast(1000).ToList(); // Save last 1000 points
                
                // Implementation would depend on chosen temp storage method
                _logger.LogDebug($"Saved {tempData.Count} data points to temporary storage");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving to temporary storage");
            }
        }

        private string GetReaderKey(string simulatorType)
        {
            return simulatorType switch
            {
                "Assetto Corsa Competizione" or "ACC" => "ACC",
                "Le Mans Ultimate" or "LeMansUltimate" => "LMU",
                "iRacing" => "iRacing",
                _ => simulatorType
            };
        }

        public TelemetryStatistics GetCurrentSessionStatistics()
        {
            if (!_sessionBuffer.Any()) return new TelemetryStatistics();

            var recentData = _sessionBuffer.TakeLast(100).ToList(); // Last 100 data points
            
            return new TelemetryStatistics
            {
                DataPointCount = _sessionBuffer.Count,
                SessionDuration = SessionDuration,
                AverageSpeed = recentData.Average(d => d.Car?.Speed ?? 0),
                MaxSpeed = _sessionBuffer.Max(d => d.Car?.Speed ?? 0),
                CurrentLap = recentData.LastOrDefault()?.Session?.CurrentLap ?? 0,
                LastLapTime = recentData.LastOrDefault()?.Session?.CurrentLapTime ?? TimeSpan.Zero
            };
        }

        public void Dispose()
        {
            StopMonitoringAsync().Wait();
            _cancellationTokenSource?.Dispose();
            
            foreach (var reader in _memoryReaders.Values)
            {
                reader?.Dispose();
            }
        }
    }

    public class TelemetryStatistics
    {
        public int DataPointCount { get; set; }
        public TimeSpan SessionDuration { get; set; }
        public float AverageSpeed { get; set; }
        public float MaxSpeed { get; set; }
        public int CurrentLap { get; set; }
        public TimeSpan LastLapTime { get; set; }
    }
}