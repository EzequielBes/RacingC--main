using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;

namespace TelemetryAnalyzer.Infrastructure.Services
{
    public class SimulatorDetectionService
    {
        private readonly Dictionary<string, string> _simulatorProcesses = new()
        {
            ["AC2-Win64-Shipping"] = "ACC",
            ["LMU"] = "LeMansUltimate",
            ["LMU-Win64-Shipping"] = "LeMansUltimate",
            ["LeMansUltimate"] = "LeMansUltimate",
            ["iRacingSim64DX11"] = "iRacing", 
            ["iRacingSim"] = "iRacing"
        };

        private readonly Dictionary<string, string[]> _memoryMapNames = new()
        {
            ["ACC"] = new[] { "Local\\acpmf_physics", "Local\\acpmf_graphics", "Local\\acpmf_static" },
            ["LeMansUltimate"] = new[] { "Local\\lmu_physics", "Local\\lmu_graphics", "Local\\lmu_static", 
                                        "Local\\acpmf_physics", "Local\\acpmf_graphics", "Local\\acpmf_static" }
        };

        public async Task<List<string>> DetectRunningSimulatorsAsync()
        {
            var runningSimulators = new List<string>();
            
            try
            {
                var processes = Process.GetProcesses();
                
                foreach (var process in processes)
                {
                    try
                    {
                        if (_simulatorProcesses.TryGetValue(process.ProcessName, out var simName))
                        {
                            runningSimulators.Add(simName);
                        }
                    }
                    catch
                    {
                        // Processo pode ter terminado durante verificação
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao detectar processos: {ex.Message}");
            }
            
            return runningSimulators.Distinct().ToList();
        }

        public async Task<Dictionary<string, bool>> CheckMemoryAvailabilityAsync()
        {
            var availability = new Dictionary<string, bool>();
            
            foreach (var kvp in _memoryMapNames)
            {
                var simulatorName = kvp.Key;
                var memoryMaps = kvp.Value;
                
                bool anyMapAvailable = false;
                
                foreach (var mapName in memoryMaps)
                {
                    try
                    {
                        using var mmf = MemoryMappedFile.OpenExisting(mapName);
                        anyMapAvailable = true;
                        break; // Se encontrou pelo menos um, é suficiente
                    }
                    catch
                    {
                        // Continuar tentando outros nomes
                    }
                }
                
                availability[simulatorName] = anyMapAvailable;
            }
            
            return availability;
        }

        public async Task<SimulatorInfo> GetSimulatorInfoAsync(string simulatorName)
        {
            var isRunning = (await DetectRunningSimulatorsAsync()).Contains(simulatorName);
            var memoryAvailable = (await CheckMemoryAvailabilityAsync()).GetValueOrDefault(simulatorName, false);
            
            return new SimulatorInfo
            {
                Name = simulatorName,
                IsProcessRunning = isRunning,
                IsMemoryAvailable = memoryAvailable,
                IsFullyAvailable = isRunning && memoryAvailable
            };
        }
    }

    public class SimulatorInfo
    {
        public string Name { get; set; } = string.Empty;
        public bool IsProcessRunning { get; set; }
        public bool IsMemoryAvailable { get; set; }
        public bool IsFullyAvailable { get; set; }
        
        public string Status => IsFullyAvailable ? "✅ Conectado" : 
                               IsProcessRunning ? "⚠️ Rodando (memória indisponível)" : 
                               "❌ Não detectado";
    }
}