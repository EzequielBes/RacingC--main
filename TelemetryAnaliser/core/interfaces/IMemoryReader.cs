public interface IMemoryReader : IDisposable
{
    bool Initialize();
    TelemetryData ReadTelemetryData();
    bool IsConnected { get; }
    event EventHandler<TelemetryData> DataReceived;
    void StartReading();
    void StopReading();
}
