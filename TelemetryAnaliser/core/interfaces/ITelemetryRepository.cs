using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Core.Interfaces
{
    public interface ITelemetryRepository
    {
        Task SaveSessionAsync(TelemetrySession session);
        Task<List<TelemetrySession>> GetSessionsAsync();
        Task<TelemetrySession> GetSessionAsync(Guid sessionId);
        Task DeleteSessionAsync(Guid sessionId);
        Task UpdateSessionAsync(TelemetrySession session);
        Task SaveTelemetryPointAsync(TelemetryData data);
        Task<List<TelemetryData>> GetSessionTelemetryAsync(Guid sessionId);
    }
}