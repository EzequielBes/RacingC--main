using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Application.Services
{
    public class LapAnalysisService
    {
        private readonly TrackMapService _trackMapService;
        private readonly PerformanceAnalysisService _performanceService;

        public LapAnalysisService(TrackMapService trackMapService, PerformanceAnalysisService performanceService)
        {
            _trackMapService = trackMapService;
            _performanceService = performanceService;
        }

        public async Task<LapData> CreateLapDataAsync(List<TelemetryData> telemetryPoints)
        {
            if (!telemetryPoints.Any()) return new LapData();

            var lapData = new LapData
            {
                TelemetryPoints = telemetryPoints,
                LapTime = CalculateLapTime(telemetryPoints),
                Timestamp = telemetryPoints.First().Timestamp,
                DriverName = ExtractDriverName(telemetryPoints),
                TrackName = telemetryPoints.First().Track?.Name ?? "Unknown",
                CarModel = ExtractCarModel(telemetryPoints)
            };

            // Calcular setores
            lapData.Sectors = await CalculateSectorsAsync(telemetryPoints);
            
            // Validar volta
            lapData.Validation = ValidateLap(telemetryPoints);
            
            // Calcular métricas de performance
            lapData.Performance = CalculatePerformanceMetrics(telemetryPoints);
            
            // Condições da volta
            lapData.Conditions = ExtractLapConditions(telemetryPoints);

            return lapData;
        }

        public async Task<List<CornerAnalysis>> AnalyzeCornersAsync(List<TelemetryData> telemetryPoints)
        {
            var corners = new List<CornerAnalysis>();
            var trackMap = await _trackMapService.GenerateTrackMapAsync(telemetryPoints);
            
            foreach (var corner in trackMap.Corners)
            {
                var cornerTelemetry = GetCornerTelemetry(telemetryPoints, corner);
                var analysis = AnalyzeCorner(cornerTelemetry, corner);
                corners.Add(analysis);
            }

            return corners;
        }

        private async Task<List<SectorData>> CalculateSectorsAsync(List<TelemetryData> telemetryPoints)
        {
            var sectors = new List<SectorData>();
            var trackLength = CalculateTrackLength(telemetryPoints);
            var sectorLength = trackLength / 3; // 3 setores padrão

            for (int i = 0; i < 3; i++)
            {
                var startDistance = i * sectorLength;
                var endDistance = (i + 1) * sectorLength;
                
                var sectorTelemetry = GetSectorTelemetry(telemetryPoints, startDistance, endDistance);
                
                var sector = new SectorData
                {
                    SectorNumber = i + 1,
                    StartDistance = startDistance,
                    EndDistance = endDistance,
                    TelemetryPoints = sectorTelemetry,
                    SectorTime = CalculateSectorTime(sectorTelemetry),
                    Analysis = await AnalyzeSectorAsync(sectorTelemetry)
                };
                
                sectors.Add(sector);
            }

            return sectors;
        }

        private LapValidation ValidateLap(List<TelemetryData> telemetryPoints)
        {
            var validation = new LapValidation();
            var violations = new List<string>();

            // Verificar limites da pista
            var trackLimitsViolations = 0;
            foreach (var point in telemetryPoints)
            {
                if (IsOffTrack(point))
                {
                    trackLimitsViolations++;
                }
            }

            var trackLimitsPercentage = (float)trackLimitsViolations / telemetryPoints.Count * 100;
            validation.TrackLimitsPercentage = trackLimitsPercentage;
            
            if (trackLimitsPercentage > 5) // Mais de 5% fora da pista
            {
                validation.HasTrackLimitsViolation = true;
                violations.Add($"Violação de limites da pista: {trackLimitsPercentage:F1}%");
            }

            // Verificar colisões (aceleração G muito alta)
            var maxGForce = telemetryPoints.Max(p => CalculateGForce(p.Car.Acceleration));
            if (maxGForce > 6.0f) // G-force anormal indica colisão
            {
                validation.HasCollision = true;
                violations.Add($"Possível colisão detectada (G-force: {maxGForce:F1}G)");
            }

            validation.IsValid = violations.Count == 0;
            validation.Violations = violations;

            return validation;
        }

        private PerformanceMetrics CalculatePerformanceMetrics(List<TelemetryData> telemetryPoints)
        {
            var metrics = new PerformanceMetrics();

            if (!telemetryPoints.Any()) return metrics;

            // Velocidade
            metrics.MaxSpeed = telemetryPoints.Max(p => p.Car.Speed);
            metrics.AverageSpeed = telemetryPoints.Average(p => p.Car.Speed);

            // G-Force
            metrics.MaxGForce = telemetryPoints.Max(p => CalculateGForce(p.Car.Acceleration));

            // Pedais
            metrics.MaxBrakeForce = telemetryPoints.Max(p => p.Car.Brake);
            metrics.MaxThrottle = telemetryPoints.Max(p => p.Car.Throttle);
            metrics.AverageThrottlePosition = telemetryPoints.Average(p => p.Car.Throttle);
            metrics.AverageBrakePosition = telemetryPoints.Average(p => p.Car.Brake);

            // Tempos de uso dos pedais
            var totalTime = (float)(telemetryPoints.Last().Timestamp - telemetryPoints.First().Timestamp).TotalSeconds;
            metrics.TotalBrakingTime = telemetryPoints.Count(p => p.Car.Brake > 0.1f) / (float)telemetryPoints.Count * totalTime;
            metrics.TotalAcceleratingTime = telemetryPoints.Count(p => p.Car.Throttle > 0.1f) / (float)telemetryPoints.Count * totalTime;
            metrics.CoastingTime = totalTime - metrics.TotalBrakingTime - metrics.TotalAcceleratingTime;

            // Trocas de marcha
            metrics.TotalGearChanges = CountGearChanges(telemetryPoints);

            // Combustível
            if (telemetryPoints.First().Car.FuelLevel > 0 && telemetryPoints.Last().Car.FuelLevel > 0)
            {
                metrics.FuelUsed = telemetryPoints.First().Car.FuelLevel - telemetryPoints.Last().Car.FuelLevel;
            }

            // Performance dos pneus
            metrics.TirePerformance = CalculateTirePerformance(telemetryPoints);

            return metrics;
        }

        private TirePerformance CalculateTirePerformance(List<TelemetryData> telemetryPoints)
        {
            var tirePerf = new TirePerformance();
            var tempHistory = new List<TireTemperatureData>();

            if (!telemetryPoints.Any() || telemetryPoints.First().Car.Tires == null)
                return tirePerf;

            var avgTemps = new List<float>();
            var maxTemps = new List<float>();
            var avgPressures = new List<float>();

            var distance = 0f;
            
            foreach (var point in telemetryPoints)
            {
                if (point.Car.Tires != null && point.Car.Tires.Length >= 4)
                {
                    var temps = point.Car.Tires.Select(t => t.Temperature).ToArray();
                    var pressures = point.Car.Tires.Select(t => t.Pressure).ToArray();
                    
                    avgTemps.AddRange(temps);
                    maxTemps.AddRange(temps);
                    avgPressures.AddRange(pressures);

                    tempHistory.Add(new TireTemperatureData
                    {
                        Distance = distance,
                        Temperatures = temps,
                        Pressures = pressures
                    });
                }
                distance += 0.1f; // Aproximação de distância
            }

            if (avgTemps.Any())
            {
                tirePerf.AverageTemperature = avgTemps.Average();
                tirePerf.MaxTemperature = maxTemps.Max();
                tirePerf.AveragePressure = avgPressures.Average();
                tirePerf.TemperatureHistory = tempHistory;
            }

            return tirePerf;
        }

        private async Task<SectorAnalysis> AnalyzeSectorAsync(List<TelemetryData> sectorTelemetry)
        {
            var analysis = new SectorAnalysis();
            
            if (!sectorTelemetry.Any()) return analysis;

            // Calcular tempo ideal (simulação simples)
            analysis.IdealTime = TimeSpan.FromSeconds(sectorTelemetry.Count * 0.016 * 0.95); // 95% do tempo atual
            analysis.TimeLost = CalculateSectorTime(sectorTelemetry) - analysis.IdealTime;
            
            // Calcular score de eficiência
            var actualTime = CalculateSectorTime(sectorTelemetry).TotalSeconds;
            var idealTime = analysis.IdealTime.TotalSeconds;
            analysis.EfficiencyScore = Math.Max(0, Math.Min(100, (float)(idealTime / actualTime * 100)));

            // Detectar problemas
            analysis.Issues = DetectSectorIssues(sectorTelemetry);

            // Métricas do setor
            analysis.Metrics = CalculateSectorMetrics(sectorTelemetry);

            return analysis;
        }

        private List<PerformanceIssue> DetectSectorIssues(List<TelemetryData> telemetry)
        {
            var issues = new List<PerformanceIssue>();

            // Detectar frenagem tardia/antecipada
            var brakingPoints = DetectBrakingPoints(telemetry);
            foreach (var point in brakingPoints)
            {
                if (point.IsLate)
                {
                    issues.Add(new PerformanceIssue
                    {
                        Type = IssueType.BrakingPoint,
                        Description = "Frenagem tardia detectada",
                        Severity = point.Severity,
                        Position = point.Position,
                        TimeLost = TimeSpan.FromMilliseconds(point.TimeLostMs),
                        Suggestion = "Tente frenar mais cedo para manter velocidade na curva"
                    });
                }
            }

            // Detectar problemas de aceleração
            issues.AddRange(DetectAccelerationIssues(telemetry));

            // Detectar problemas de traçado
            issues.AddRange(DetectLineIssues(telemetry));

            return issues;
        }

        private SectorMetrics CalculateSectorMetrics(List<TelemetryData> telemetry)
        {
            var metrics = new SectorMetrics();

            if (!telemetry.Any()) return metrics;

            metrics.AverageSpeed = telemetry.Average(t => t.Car.Speed);
            metrics.MaxSpeed = telemetry.Max(t => t.Car.Speed);
            metrics.MinSpeed = telemetry.Min(t => t.Car.Speed);
            metrics.GearChanges = CountGearChanges(telemetry);

            // Calcular distância de frenagem
            var brakingPoints = telemetry.Where(t => t.Car.Brake > 0.1f).ToList();
            if (brakingPoints.Count > 1)
            {
                var firstBrake = brakingPoints.First().Car.Position;
                var lastBrake = brakingPoints.Last().Car.Position;
                metrics.BrakingDistance = Vector3.Distance(firstBrake, lastBrake);
            }

            return metrics;
        }

        private CornerAnalysis AnalyzeCorner(List<TelemetryData> cornerTelemetry, Corner corner)
        {
            var analysis = new CornerAnalysis
            {
                CornerNumber = corner.Id,
                CornerName = corner.Name,
                Type = corner.Type,
                Radius = corner.Radius,
                ApexPosition = corner.ApexPosition
            };

            if (!cornerTelemetry.Any()) return analysis;

            // Encontrar pontos-chave da curva
            var speeds = cornerTelemetry.Select(t => t.Car.Speed).ToArray();
            var positions = cornerTelemetry.Select(t => t.Car.Position).ToArray();

            // Velocidade de entrada (primeira medição)
            analysis.EntrySpeed = speeds.First();
            
            // Velocidade do apex (velocidade mínima na curva)
            var minSpeedIndex = Array.IndexOf(speeds, speeds.Min());
            analysis.ApexSpeed = speeds[minSpeedIndex];
            analysis.ApexPosition = positions[minSpeedIndex];
            
            // Velocidade de saída (última medição)
            analysis.ExitSpeed = speeds.Last();

            // Calcular tempo da curva
            if (cornerTelemetry.Count > 1)
            {
                analysis.CornerTime = cornerTelemetry.Last().Timestamp - cornerTelemetry.First().Timestamp;
            }

            // Detectar problemas na curva
            analysis.Issues = DetectCornerIssues(cornerTelemetry, corner);

            // Avaliar performance da curva
            analysis.Rating = RateCornerPerformance(analysis);

            return analysis;
        }

        private List<CornerIssue> DetectCornerIssues(List<TelemetryData> telemetry, Corner corner)
        {
            var issues = new List<CornerIssue>();

            // Implementar detecção de problemas específicos das curvas
            // Apex muito cedo/tarde, frenagem inadequada, etc.

            return issues;
        }

        private CornerRating RateCornerPerformance(CornerAnalysis analysis)
        {
            // Lógica simplificada de avaliação
            var speedRatio = analysis.ApexSpeed / analysis.EntrySpeed;
            
            if (speedRatio > 0.8f) return CornerRating.Excellent;
            if (speedRatio > 0.7f) return CornerRating.Good;
            if (speedRatio > 0.6f) return CornerRating.Average;
            if (speedRatio > 0.5f) return CornerRating.Poor;
            return CornerRating.Terrible;
        }

        // Métodos auxiliares
        private TimeSpan CalculateLapTime(List<TelemetryData> telemetry)
        {
            if (telemetry.Count < 2) return TimeSpan.Zero;
            return telemetry.Last().Timestamp - telemetry.First().Timestamp;
        }

        private TimeSpan CalculateSectorTime(List<TelemetryData> telemetry)
        {
            if (telemetry.Count < 2) return TimeSpan.Zero;
            return telemetry.Last().Timestamp - telemetry.First().Timestamp;
        }

        private string ExtractDriverName(List<TelemetryData> telemetry)
        {
            return "Piloto"; // Implementar extração do nome do piloto
        }

        private string ExtractCarModel(List<TelemetryData> telemetry)
        {
            return telemetry.FirstOrDefault()?.SimulatorName ?? "Unknown";
        }

        private LapConditions ExtractLapConditions(List<TelemetryData> telemetry)
        {
            var conditions = new LapConditions();
            
            if (telemetry.Any())
            {
                var track = telemetry.First().Track;
                if (track != null)
                {
                    conditions.TrackTemperature = track.TrackTemperature;
                    conditions.AmbientTemperature = track.AmbientTemperature;
                }
            }

            return conditions;
        }

        private bool IsOffTrack(TelemetryData point)
        {
            // Implementar lógica de detecção de saída da pista
            // Por enquanto, retorna false
            return false;
        }

        private float CalculateGForce(Vector3 acceleration)
        {
            return acceleration.Length() / 9.81f; // Converter para G-force
        }

        private int CountGearChanges(List<TelemetryData> telemetry)
        {
            if (telemetry.Count < 2) return 0;
            
            int changes = 0;
            int previousGear = telemetry.First().Car.Gear;
            
            foreach (var point in telemetry.Skip(1))
            {
                if (point.Car.Gear != previousGear)
                {
                    changes++;
                    previousGear = point.Car.Gear;
                }
            }
            
            return changes;
        }

        private float CalculateTrackLength(List<TelemetryData> telemetry)
        {
            if (telemetry.Count < 2) return 0;
            
            float totalDistance = 0;
            for (int i = 1; i < telemetry.Count; i++)
            {
                var distance = Vector3.Distance(
                    telemetry[i - 1].Car.Position,
                    telemetry[i].Car.Position
                );
                totalDistance += distance;
            }
            
            return totalDistance;
        }

        private List<TelemetryData> GetSectorTelemetry(List<TelemetryData> telemetry, float startDistance, float endDistance)
        {
            // Implementar lógica para extrair telemetria de um setor específico
            var sectorSize = telemetry.Count / 3;
            var sectorIndex = (int)(startDistance / (CalculateTrackLength(telemetry) / 3));
            
            var startIndex = sectorIndex * sectorSize;
            var endIndex = Math.Min(startIndex + sectorSize, telemetry.Count);
            
            return telemetry.Skip(startIndex).Take(endIndex - startIndex).ToList();
        }

        private List<TelemetryData> GetCornerTelemetry(List<TelemetryData> telemetry, Corner corner)
        {
            // Implementar lógica para extrair telemetria de uma curva específica
            return telemetry.Where(t => Vector3.Distance(t.Car.Position, corner.ApexPosition) < corner.Radius * 2).ToList();
        }

        private List<BrakingPoint> DetectBrakingPoints(List<TelemetryData> telemetry)
        {
            var brakingPoints = new List<BrakingPoint>();
            
            for (int i = 1; i < telemetry.Count; i++)
            {
                var current = telemetry[i];
                var previous = telemetry[i - 1];
                
                // Detectar início da frenagem
                if (previous.Car.Brake < 0.1f && current.Car.Brake > 0.1f)
                {
                    brakingPoints.Add(new BrakingPoint
                    {
                        Position = current.Car.Position,
                        Speed = current.Car.Speed,
                        BrakeForce = current.Car.Brake,
                        IsLate = DetermineBrakingTiming(telemetry, i),
                        Severity = CalculateBrakingSeverity(current),
                        TimeLostMs = EstimateTimeLoss(current)
                    });
                }
            }
            
            return brakingPoints;
        }

        private List<PerformanceIssue> DetectAccelerationIssues(List<TelemetryData> telemetry)
        {
            var issues = new List<PerformanceIssue>();
            
            // Detectar aceleração tardia após curvas
            for (int i = 1; i < telemetry.Count - 1; i++)
            {
                var current = telemetry[i];
                var next = telemetry[i + 1];
                
                // Se a velocidade está aumentando mas o throttle está baixo
                if (next.Car.Speed > current.Car.Speed && current.Car.Throttle < 0.5f && current.Car.Brake < 0.1f)
                {
                    issues.Add(new PerformanceIssue
                    {
                        Type = IssueType.Acceleration,
                        Description = "Aceleração tardia detectada",
                        Position = current.Car.Position,
                        Severity = 1.0f - current.Car.Throttle,
                        Suggestion = "Acelere mais cedo para ganhar velocidade"
                    });
                }
            }
            
            return issues;
        }

        private List<PerformanceIssue> DetectLineIssues(List<TelemetryData> telemetry)
        {
            var issues = new List<PerformanceIssue>();
            
            // Implementar detecção de problemas de traçado
            // Por enquanto retorna lista vazia
            
            return issues;
        }

        private bool DetermineBrakingTiming(List<TelemetryData> telemetry, int index)
        {
            // Lógica simplificada - considera tardio se freou muito próximo da curva
            return false;
        }

        private float CalculateBrakingSeverity(TelemetryData point)
        {
            return Math.Min(1.0f, point.Car.Brake);
        }

        private int EstimateTimeLoss(TelemetryData point)
        {
            return (int)(point.Car.Brake * 100); // Estimativa simples
        }
    }

    // Classes auxiliares
    public class BrakingPoint
    {
        public Vector3 Position { get; set; }
        public float Speed { get; set; }
        public float BrakeForce { get; set; }
        public bool IsLate { get; set; }
        public float Severity { get; set; }
        public int TimeLostMs { get; set; }
    }

    public class Corner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Vector3 ApexPosition { get; set; }
        public CornerType Type { get; set; }
        public float Radius { get; set; }
    }
}