using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Infrastructure.Services
{
    public class PerformanceAnalysisService
    {
        public PerformanceMetrics AnalyzePerformance(List<TelemetryData> telemetryData)
        {
            if (!telemetryData.Any()) return new PerformanceMetrics();

            var metrics = new PerformanceMetrics();

            // Velocidade
            metrics.MaxSpeed = telemetryData.Max(d => d.Car?.Speed ?? 0);
            metrics.AverageSpeed = telemetryData.Average(d => d.Car?.Speed ?? 0);

            // G-Force
            metrics.MaxGForce = telemetryData.Max(d => CalculateGForce(d.Car?.Acceleration ?? Vector3.Zero));

            // Pedais
            metrics.MaxBrakeForce = telemetryData.Max(d => d.Car?.Brake ?? 0);
            metrics.MaxThrottle = telemetryData.Max(d => d.Car?.Throttle ?? 0);
            metrics.AverageThrottlePosition = telemetryData.Average(d => d.Car?.Throttle ?? 0);
            metrics.AverageBrakePosition = telemetryData.Average(d => d.Car?.Brake ?? 0);

            // Contagem de trocas de marcha
            metrics.TotalGearChanges = CountGearChanges(telemetryData);

            return metrics;
        }

        private float CalculateGForce(Vector3 acceleration)
        {
            return acceleration.Length() / 9.81f;
        }

        private int CountGearChanges(List<TelemetryData> data)
        {
            if (data.Count < 2) return 0;

            int changes = 0;
            int previousGear = data.First().Car?.Gear ?? 0;

            foreach (var point in data.Skip(1))
            {
                var currentGear = point.Car?.Gear ?? 0;
                if (currentGear != previousGear)
                {
                    changes++;
                    previousGear = currentGear;
                }
            }

            return changes;
        }

        public List<PerformanceIssue> DetectIssues(List<TelemetryData> data)
        {
            var issues = new List<PerformanceIssue>();

            // Detectar frenagem inconsistente
            issues.AddRange(DetectBrakingIssues(data));

            // Detectar problemas de aceleração
            issues.AddRange(DetectAccelerationIssues(data));

            // Detectar problemas de traçado
            issues.AddRange(DetectLineIssues(data));

            return issues.OrderByDescending(i => i.Severity).ToList();
        }

        private List<PerformanceIssue> DetectBrakingIssues(List<TelemetryData> data)
        {
            var issues = new List<PerformanceIssue>();

            for (int i = 1; i < data.Count - 1; i++)
            {
                var current = data[i];
                var previous = data[i - 1];
                var next = data[i + 1];

                // Detectar frenagem muito tardia
                if (previous.Car.Brake < 0.1f && current.Car.Brake > 0.8f)
                {
                    var speedDrop = previous.Car.Speed - next.Car.Speed;
                    if (speedDrop > 50) // Perda de velocidade > 50 km/h rapidamente
                    {
                        issues.Add(new PerformanceIssue
                        {
                            Type = IssueType.BrakingPoint,
                            Description = "Frenagem muito tardia detectada",
                            Position = current.Car.Position,
                            Severity = Math.Min(1.0f, speedDrop / 100f),
                            Suggestion = "Tente frenar mais cedo para entrada mais suave na curva"
                        });
                    }
                }
            }

            return issues;
        }

        private List<PerformanceIssue> DetectAccelerationIssues(List<TelemetryData> data)
        {
            var issues = new List<PerformanceIssue>();

            for (int i = 1; i < data.Count; i++)
            {
                var current = data[i];
                var previous = data[i - 1];

                // Detectar aceleração perdida
                if (current.Car.Speed > previous.Car.Speed && // Velocidade aumentando
                    current.Car.Throttle < 0.5f && // Mas throttle baixo
                    current.Car.Brake < 0.1f) // E não está freando
                {
                    issues.Add(new PerformanceIssue
                    {
                        Type = IssueType.Acceleration,
                        Description = "Oportunidade de aceleração perdida",
                        Position = current.Car.Position,
                        Severity = 1.0f - current.Car.Throttle,
                        Suggestion = "Acelere mais cedo para ganhar velocidade"
                    });
                }
            }

            return issues;
        }

        private List<PerformanceIssue> DetectLineIssues(List<TelemetryData> data)
        {
            var issues = new List<PerformanceIssue>();
            // Implementação mais complexa seria necessária para detectar problemas de traçado
            return issues;
        }
    }
}