using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Application.Services
{
    public class LapComparisonService
    {
        private readonly LapAnalysisService _lapAnalysisService;

        public LapComparisonService(LapAnalysisService lapAnalysisService)
        {
            _lapAnalysisService = lapAnalysisService;
        }

        public async Task<LapComparison> CompareLapsAsync(LapData referenceLap, LapData comparisonLap)
        {
            var comparison = new LapComparison
            {
                Name = $"{referenceLap.DriverName} vs {comparisonLap.DriverName}",
                ReferenceLap = referenceLap,
                ComparisonLap = comparisonLap
            };

            // Calcular resultados da comparação
            comparison.Results = CalculateComparisonResults(referenceLap, comparisonLap);

            // Calcular deltas de tempo
            comparison.TimeDeltas = CalculateTimeDeltas(referenceLap, comparisonLap);

            // Comparar setores
            comparison.SectorComparisons = CompareSectors(referenceLap.Sectors, comparisonLap.Sectors);

            // Comparar curvas
            if (referenceLap.Performance != null && comparisonLap.Performance != null)
            {
                comparison.CornerComparisons = await CompareCorners(referenceLap, comparisonLap);
            }

            // Análise geral
            comparison.OverallAnalysis = GenerateOverallAnalysis(comparison);

            return comparison;
        }

        private ComparisonResults CalculateComparisonResults(LapData reference, LapData comparison)
        {
            var results = new ComparisonResults
            {
                TimeDifference = comparison.LapTime - reference.LapTime,
                SpeedDifference = comparison.Performance.AverageSpeed - reference.Performance.AverageSpeed
            };

            // Resumo da comparação
            results.Summary = new ComparisonSummary
            {
                FasterDriver = results.TimeDifference.TotalSeconds < 0 ? comparison.DriverName : reference.DriverName,
                LapTimeDifference = TimeSpan.FromSeconds(Math.Abs(results.TimeDifference.TotalSeconds)),
                PercentageDifference = (float)(Math.Abs(results.TimeDifference.TotalSeconds) / reference.LapTime.TotalSeconds * 100),
                Category = CategorizeTimeDifference(results.TimeDifference)
            };

            // Detectar áreas de melhoria
            results.ImprovementAreas = DetectImprovementAreas(reference, comparison);

            // Breakdown de performance
            results.Breakdown = CalculatePerformanceBreakdown(reference, comparison);

            return results;
        }

        private List<ComparisonPoint> CalculateTimeDeltas(LapData reference, LapData comparison)
        {
            var deltas = new List<ComparisonPoint>();
            
            // Normalizar as voltas por distância
            var refPoints = NormalizeLapByDistance(reference);
            var compPoints = NormalizeLapByDistance(comparison);
            
            // Interpolar pontos para comparação
            var commonPoints = InterpolateCommonPoints(refPoints, compPoints);
            
            var cumulativeTimeDelta = TimeSpan.Zero;
            
            foreach (var point in commonPoints)
            {
                cumulativeTimeDelta += point.TimeDelta;
                
                deltas.Add(new ComparisonPoint
                {
                    Distance = point.Distance,
                    TimeDelta = cumulativeTimeDelta,
                    SpeedDifference = point.SpeedDifference,
                    Position = point.Position,
                    Metrics = point.Metrics
                });
            }
            
            return deltas;
        }

        private List<SectorComparison> CompareSectors(List<SectorData> referenceSectors, List<SectorData> comparisonSectors)
        {
            var sectorComparisons = new List<SectorComparison>();
            
            for (int i = 0; i < Math.Min(referenceSectors.Count, comparisonSectors.Count); i++)
            {
                var refSector = referenceSectors[i];
                var compSector = comparisonSectors[i];
                
                var comparison = new SectorComparison
                {
                    SectorNumber = refSector.SectorNumber,
                    TimeDifference = compSector.SectorTime - refSector.SectorTime,
                    SpeedDifference = compSector.Analysis.Metrics.AverageSpeed - refSector.Analysis.Metrics.AverageSpeed,
                    ReferenceAnalysis = refSector.Analysis,
                    ComparisonAnalysis = compSector.Analysis
                };
                
                // Gerar sugestões de melhoria para o setor
                comparison.Suggestions = GenerateSectorSuggestions(refSector, compSector);
                
                sectorComparisons.Add(comparison);
            }
            
            return sectorComparisons;
        }

        private async Task<List<CornerComparison>> CompareCorners(LapData reference, LapData comparison)
        {
            var cornerComparisons = new List<CornerComparison>();
            
            // Analisar curvas de ambas as voltas
            var refCorners = await _lapAnalysisService.AnalyzeCornersAsync(reference.TelemetryPoints);
            var compCorners = await _lapAnalysisService.AnalyzeCornersAsync(comparison.TelemetryPoints);
            
            // Combinar curvas por posição aproximada
            for (int i = 0; i < Math.Min(refCorners.Count, compCorners.Count); i++)
            {
                var refCorner = refCorners[i];
                var compCorner = compCorners[i];
                
                var cornerComparison = new CornerComparison
                {
                    CornerNumber = refCorner.CornerNumber,
                    CornerName = refCorner.CornerName,
                    TimeDifference = compCorner.CornerTime - refCorner.CornerTime,
                    Reference = refCorner,
                    Comparison = compCorner,
                    Differences = CalculateCornerDifferences(refCorner, compCorner),
                    ImprovementSuggestions = GenerateCornerImprovements(refCorner, compCorner)
                };
                
                cornerComparisons.Add(cornerComparison);
            }
            
            return cornerComparisons;
        }

        private OverallAnalysis GenerateOverallAnalysis(LapComparison comparison)
        {
            var analysis = new OverallAnalysis();
            
            // Identificar forças e fraquezas
            analysis.Analysis = IdentifyStrengthsAndWeaknesses(comparison);
            
            // Gerar insights-chave
            analysis.KeyInsights = GenerateKeyInsights(comparison);
            
            // Recomendações de treinamento
            analysis.Recommendations = GenerateTrainingRecommendations(comparison);
            
            // Score geral (0-100)
            analysis.OverallScore = CalculateOverallScore(comparison);
            
            return analysis;
        }

        private StrengthsAndWeaknesses IdentifyStrengthsAndWeaknesses(LapComparison comparison)
        {
            var analysis = new StrengthsAndWeaknesses();
            
            // Analisar setores para identificar pontos fortes e fracos
            foreach (var sector in comparison.SectorComparisons)
            {
                if (sector.TimeDifference.TotalSeconds < -0.1) // Mais rápido que referência
                {
                    analysis.Strengths.Add($"Setor {sector.SectorNumber}: {Math.Abs(sector.TimeDifference.TotalSeconds):F3}s mais rápido");
                }
                else if (sector.TimeDifference.TotalSeconds > 0.1) // Mais lento que referência
                {
                    analysis.Weaknesses.Add($"Setor {sector.SectorNumber}: {sector.TimeDifference.TotalSeconds:F3}s mais lento");
                }
                else
                {
                    analysis.NeutralAreas.Add($"Setor {sector.SectorNumber}: Performance similar");
                }
            }
            
            return analysis;
        }

        private List<KeyInsight> GenerateKeyInsights(LapComparison comparison)
        {
            var insights = new List<KeyInsight>();
            
            // Insight sobre tempo geral
            var totalTimeDiff = comparison.Results.TimeDifference.TotalSeconds;
            if (Math.Abs(totalTimeDiff) > 0.5)
            {
                insights.Add(new KeyInsight
                {
                    Title = "Diferença Significativa de Tempo",
                    Description = $"Diferença de {Math.Abs(totalTimeDiff):F3}s indica oportunidades claras de melhoria",
                    Type = totalTimeDiff > 0 ? InsightType.Weakness : InsightType.Strength,
                    Impact = Math.Min(1.0f, (float)(Math.Abs(totalTimeDiff) / 5.0)) // Normalizar para 0-1
                });
            }
            
            // Insights sobre setores
            var worstSector = comparison.SectorComparisons
                .OrderByDescending(s => s.TimeDifference.TotalSeconds)
                .FirstOrDefault();
                
            if (worstSector != null && worstSector.TimeDifference.TotalSeconds > 0.2)
            {
                insights.Add(new KeyInsight
                {
                    Title = $"Setor {worstSector.SectorNumber} Precisa de Atenção",
                    Description = $"Perdendo {worstSector.TimeDifference.TotalSeconds:F3}s neste setor",
                    Type = InsightType.Opportunity,
                    Impact = 0.8f
                });
            }
            
            return insights;
        }

        private TrainingRecommendations GenerateTrainingRecommendations(LapComparison comparison)
        {
            var recommendations = new TrainingRecommendations();
            
            // Focos imediatos
            var worstSectors = comparison.SectorComparisons
                .Where(s => s.TimeDifference.TotalSeconds > 0.1)
                .OrderByDescending(s => s.TimeDifference.TotalSeconds)
                .Take(2);
                
            foreach (var sector in worstSectors)
            {
                recommendations.ImmediateFocus.Add($"Trabalhar no Setor {sector.SectorNumber} - perda de {sector.TimeDifference.TotalSeconds:F3}s");
            }
            
            // Metas de médio prazo
            if (comparison.Results.TimeDifference.TotalSeconds > 1.0)
            {
                recommendations.MediumTermGoals.Add("Reduzir diferença geral para menos de 1s");
                recommendations.MediumTermGoals.Add("Melhorar consistência entre setores");
            }
            
            // Desenvolvimento de longo prazo
            recommendations.LongTermDevelopment.Add("Desenvolver feeling do carro");
            recommendations.LongTermDevelopment.Add("Otimizar setup para seu estilo de pilotagem");
            
            return recommendations;
        }

        private float CalculateOverallScore(LapComparison comparison)
        {
            var timeDiff = Math.Abs(comparison.Results.TimeDifference.TotalSeconds);
            var referenceTime = comparison.ReferenceLap.LapTime.TotalSeconds;
            
            // Score baseado na diferença percentual (invertido)
            var percentageDiff = timeDiff / referenceTime * 100;
            var score = Math.Max(0, 100 - percentageDiff * 10); // Cada 1% de diferença remove 10 pontos
            
            return (float)Math.Min(100, score);
        }

        // Métodos auxiliares
        private ComparisonCategory CategorizeTimeDifference(TimeSpan timeDiff)
        {
            var seconds = Math.Abs(timeDiff.TotalSeconds);
            
            if (seconds < 0.5) return ComparisonCategory.VeryClose;
            if (seconds < 1.0) return ComparisonCategory.Close;
            if (seconds < 2.0) return ComparisonCategory.Moderate;
            if (seconds < 5.0) return ComparisonCategory.Significant;
            return ComparisonCategory.Large;
        }

        private List<ImprovementArea> DetectImprovementAreas(LapData reference, LapData comparison)
        {
            var areas = new List<ImprovementArea>();
            
            // Comparar métricas de performance
            if (comparison.Performance.AverageSpeed < reference.Performance.AverageSpeed)
            {
                areas.Add(new ImprovementArea
                {
                    Category = "Velocidade",
                    Description = "Velocidade média inferior à referência",
                    PotentialGain = TimeSpan.FromSeconds(0.5),
                    Priority = ImprovementPriority.High
                });
            }
            
            if (comparison.Performance.TotalBrakingTime > reference.Performance.TotalBrakingTime * 1.1)
            {
                areas.Add(new ImprovementArea
                {
                    Category = "Frenagem",
                    Description = "Tempo de frenagem excessivo",
                    PotentialGain = TimeSpan.FromSeconds(0.3),
                    Priority = ImprovementPriority.Medium
                });
            }
            
            return areas;
        }

        private PerformanceBreakdown CalculatePerformanceBreakdown(LapData reference, LapData comparison)
        {
            return new PerformanceBreakdown
            {
                BrakingTimeDiff = TimeSpan.FromSeconds(comparison.Performance.TotalBrakingTime - reference.Performance.TotalBrakingTime),
                AccelerationTimeDiff = TimeSpan.FromSeconds(comparison.Performance.TotalAcceleratingTime - reference.Performance.TotalAcceleratingTime),
                CorneringTimeDiff = TimeSpan.FromSeconds(0), // Calcular baseado nas curvas
                StraightLineTimeDiff = TimeSpan.FromSeconds(0) // Calcular baseado nas retas
            };
        }

        private List<NormalizedTelemetryPoint> NormalizeLapByDistance(LapData lap)
        {
            // Implementar normalização por distância
            return new List<NormalizedTelemetryPoint>();
        }

        private List<ComparisonPoint> InterpolateCommonPoints(List<NormalizedTelemetryPoint> refPoints, List<NormalizedTelemetryPoint> compPoints)
        {
            // Implementar interpolação para pontos comuns
            return new List<ComparisonPoint>();
        }

        private List<ImprovementSuggestion> GenerateSectorSuggestions(SectorData reference, SectorData comparison)
        {
            var suggestions = new List<ImprovementSuggestion>();
            
            if (comparison.SectorTime > reference.SectorTime)
            {
                suggestions.Add(new ImprovementSuggestion
                {
                    Title = "Melhorar tempo do setor",
                    Description = $"Setor está {(comparison.SectorTime - reference.SectorTime).TotalSeconds:F3}s mais lento",
                    Category = "Tempo",
                    Priority = ImprovementPriority.High,
                    ExpectedGain = reference.SectorTime - comparison.SectorTime
                });
            }
            
            return suggestions;
        }

        private CornerDifferences CalculateCornerDifferences(CornerAnalysis reference, CornerAnalysis comparison)
        {
            return new CornerDifferences
            {
                EntrySpeedDiff = comparison.EntrySpeed - reference.EntrySpeed,
                ApexSpeedDiff = comparison.ApexSpeed - reference.ApexSpeed,
                ExitSpeedDiff = comparison.ExitSpeed - reference.ExitSpeed,
                ApexPositionDiff = comparison.ApexPosition - reference.ApexPosition
            };
        }

        private List<CornerImprovement> GenerateCornerImprovements(CornerAnalysis reference, CornerAnalysis comparison)
        {
            var improvements = new List<CornerImprovement>();
            
            if (comparison.EntrySpeed < reference.EntrySpeed * 0.95f)
            {
                improvements.Add(new CornerImprovement
                {
                    Phase = CornerPhase.Entry,
                    Description = "Aumentar velocidade de entrada",
                    PotentialGain = TimeSpan.FromMilliseconds(50),
                    Priority = ImprovementPriority.Medium
                });
            }
            
            return improvements;
        }
    }

    // Classes auxiliares para comparação
    public class NormalizedTelemetryPoint
    {
        public float Distance { get; set; }
        public TimeSpan Time { get; set; }
        public float Speed { get; set; }
        public Vector3 Position { get; set; }
        public float Throttle { get; set; }
        public float Brake { get; set; }
        public int Gear { get; set; }
    }
}