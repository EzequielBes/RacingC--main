using System;

namespace TelemetryAnalyzer.Core.Interfaces
{
    public interface IMemoryReader : IDisposable
    {
        event EventHandler<Core.Models.TelemetryData> TelemetryReceived;
        void Start();
        void Stop();
    }
}

