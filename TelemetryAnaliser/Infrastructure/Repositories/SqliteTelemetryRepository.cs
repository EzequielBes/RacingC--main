using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Infrastructure.Data;

namespace TelemetryAnalyzer.Infrastructure.Repositories
{
    public class SqliteTelemetryRepository : ITelemetryRepository
    {
        private readonly TelemetryDbContext _context;

        public SqliteTelemetryRepository(TelemetryDbContext context)
        {
            _context = context;
        }

        public async Task SaveSessionAsync(TelemetrySession session)
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TelemetrySession>> GetSessionsAsync()
        {
            return await _context.Sessions
                .Include(s => s.DataPoints)
                .OrderByDescending(s => s.ImportedAt)
                .ToListAsync();
        }

        public async Task<TelemetrySession> GetSessionAsync(Guid sessionId)
        {
            var session = await _context.Sessions
                .Include(s => s.DataPoints)
                .FirstOrDefaultAsync(s => s.Id == sessionId);

            if (session?.DataPoints?.Any() == true)
            {
                // Deserializar dados de telemetria
                session.Data = new ProcessedTelemetryData
                {
                    RawData = session.DataPoints
                        .Select(dp => JsonConvert.DeserializeObject<TelemetryData>(dp.JsonData))
                        .Where(td => td != null)
                        .ToList()
                };

                // Processar voltas a partir dos dados brutos
                session.Data.Laps = ExtractLapsFromRawData(session.Data.RawData);
            }

            return session;
        }

        public async Task DeleteSessionAsync(Guid sessionId)
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session != null)
            {
                _context.Sessions.Remove(session);
                await _context.SaveChangesAsync();
            }
        }

        public async Task UpdateSessionAsync(TelemetrySession session)
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
        }

        public async Task SaveTelemetryPointAsync(TelemetryData data)
        {
            var dataPoint = new TelemetryDataPoint
            {
                Timestamp = data.Timestamp,
                JsonData = JsonConvert.SerializeObject(data)
            };

            _context.DataPoints.Add(dataPoint);
            await _context.SaveChangesAsync();
        }

        public async Task<List<TelemetryData>> GetSessionTelemetryAsync(Guid sessionId)
        {
            var dataPoints = await _context.DataPoints
                .Where(dp => dp.Session.Id == sessionId)
                .OrderBy(dp => dp.Timestamp)
                .ToListAsync();

            return dataPoints
                .Select(dp => JsonConvert.DeserializeObject<TelemetryData>(dp.JsonData))
                .Where(td => td != null)
                .ToList();
        }

        private List<LapData> ExtractLapsFromRawData(List<TelemetryData> rawData)
        {
            var laps = new List<LapData>();
            var currentLapData = new List<TelemetryData>();
            int currentLapNumber = 1;

            foreach (var data in rawData.OrderBy(d => d.Timestamp))
            {
                if (data.Session?.CurrentLap != currentLapNumber && currentLapData.Any())
                {
                    // Nova volta detectada
                    laps.Add(new LapData
                    {
                        LapNumber = currentLapNumber,
                        Data = currentLapData.ToList(),
                        LapTime = CalculateLapTime(currentLapData),
                        IsValid = ValidateLap(currentLapData)
                    });

                    currentLapData.Clear();
                    currentLapNumber = data.Session?.CurrentLap ?? currentLapNumber + 1;
                }

                currentLapData.Add(data);
            }

            // Adicionar última volta se existir
            if (currentLapData.Any())
            {
                laps.Add(new LapData
                {
                    LapNumber = currentLapNumber,
                    Data = currentLapData,
                    LapTime = CalculateLapTime(currentLapData),
                    IsValid = ValidateLap(currentLapData)
                });
            }

            // Marcar volta mais rápida como personal best
            var validLaps = laps.Where(l => l.IsValid && l.LapTime > TimeSpan.Zero).ToList();
            if (validLaps.Any())
            {
                var fastestLap = validLaps.OrderBy(l => l.LapTime).First();
                fastestLap.IsPersonalBest = true;
            }

            return laps;
        }

        private TimeSpan CalculateLapTime(List<TelemetryData> lapData)
        {
            if (lapData.Count < 2) return TimeSpan.Zero;
            return lapData.Last().Timestamp - lapData.First().Timestamp;
        }

        private bool ValidateLap(List<TelemetryData> lapData)
        {
            if (lapData.Count < 10) return false; // Muito poucos dados
            
            var lapTime = CalculateLapTime(lapData);
            if (lapTime.TotalSeconds < 30) return false; // Volta muito rápida (suspeita)
            if (lapTime.TotalSeconds > 600) return false; // Volta muito lenta (10 minutos)

            return true;
        }
    }
}