using System;
using System.Threading;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Interfaces;

public class iRacingMemoryReader : IMemoryReader
{
    private iRacingSDK _sdk;
    private bool _isReading;
    private CancellationTokenSource _cancellationTokenSource;

    public bool IsConnected { get; private set; }
    public event EventHandler<TelemetryData> DataReceived;
    public event EventHandler<TelemetryAnalyzer.Core.Models.TelemetryData> TelemetryReceived;

    public bool Initialize()
    {
        try
        {
            _sdk = new iRacingSDK();
            IsConnected = _sdk.Startup();
            return IsConnected;
        }
        catch
        {
            IsConnected = false;
            return false;
        }
    }

    public void StartReading()
    {
        if (_isReading) return;
        
        _isReading = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var data = ReadTelemetryData();
                if (data != null)
                {
                    DataReceived?.Invoke(this, data);
                }
                await Task.Delay(16, _cancellationTokenSource.Token);
            }
        }, _cancellationTokenSource.Token);
    }

    public TelemetryData ReadTelemetryData()
    {
        if (!IsConnected || !_sdk.IsConnected()) return null;

        var sessionData = _sdk.GetSessionData();
        var telemetryData = _sdk.GetTelemetryData();

        return new TelemetryData
        {
            Timestamp = DateTime.Now,
            SimulatorName = "iRacing",
            Car = MapiRacingCarData(telemetryData),
            Track = MapiRacingTrackData(sessionData),
            Session = MapiRacingSessionData(sessionData, telemetryData)
        };
    }

    private CarData MapiRacingCarData(object telemetryData)
    {
        // Implementar mapeamento específico do iRacing
        return new CarData
        {
            Speed = _sdk.GetFloat("Speed"),
            RPM = _sdk.GetFloat("RPM"),
            Gear = _sdk.GetInt("Gear"),
            Throttle = _sdk.GetFloat("Throttle"),
            Brake = _sdk.GetFloat("Brake"),
            // ... outros campos
        };
    }

    public void StopReading()
    {
        _isReading = false;
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        StopReading();
        _sdk?.Shutdown();
    }

    public void Start() => StartReading();
    public void Stop() => StopReading();
}