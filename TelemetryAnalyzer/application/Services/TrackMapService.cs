using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Application.Services
{
    public class TrackMapService
    {
        private readonly Dictionary<string, TrackLayout> _knownTracks = new();
        private readonly TrackGeometryAnalyzer _geometryAnalyzer;

        public TrackMapService()
        {
            _geometryAnalyzer = new TrackGeometryAnalyzer();
            InitializeKnownTracks();
        }

        public async Task<TrackMap> GenerateTrackMapAsync(List<TelemetryData> sessionData)
        {
            if (!sessionData.Any()) return new TrackMap();

            var trackName = sessionData.First().Track?.Name ?? "Unknown";
            
            // Extrair posições do carro
            var positions = sessionData.Select(d => d.Car.Position).ToList();
            
            // Filtrar e suavizar trajetória
            var smoothedPositions = await SmoothTrajectoryAsync(positions);
            
            // Detectar início/fim da volta
            var startFinishLine = DetectStartFinishLine(smoothedPositions);
            
            // Detectar curvas
            var corners = await DetectCornersAsync(smoothedPositions, sessionData);
            
            // Calcular setores
            var sectors = CalculateTrackSectors(smoothedPositions, corners);
            
            // Detectar zonas de frenagem e aceleração
            var brakingZones = DetectBrakingZones(sessionData);
            var accelerationZones = DetectAccelerationZones(sessionData);
            
            var trackMap = new TrackMap
            {
                Name = trackName,
                TrackPoints = smoothedPositions,
                StartFinishLine = startFinishLine,
                TrackLength = CalculateTrackLength(smoothedPositions),
                Corners = corners,
                Sectors = sectors,
                BrakingZones = brakingZones,
                AccelerationZones = accelerationZones,
                IdealLine = await CalculateIdealLineAsync(smoothedPositions, corners),
                Elevation = ExtractElevationProfile(sessionData),
                TrackWidth = EstimateTrackWidth(sessionData)
            };

            // Salvar na cache de pistas conhecidas
            if (!_knownTracks.ContainsKey(trackName))
            {
                _knownTracks[trackName] = ConvertToTrackLayout(trackMap);
            }

            return trackMap;
        }

        public async Task<List<TrackingLine>> CompareTrackingLinesAsync(List<TelemetryData> lap1, List<TelemetryData> lap2)
        {
            var lines = new List<TrackingLine>();
            
            var positions1 = lap1.Select(d => d.Car.Position).ToList();
            var positions2 = lap2.Select(d => d.Car.Position).ToList();
            
            // Normalizar por distância para comparação
            var normalizedPos1 = NormalizeByDistance(positions1);
            var normalizedPos2 = NormalizeByDistance(positions2);
            
            // Criar linhas de traçado
            lines.Add(new TrackingLine
            {
                Name = "Piloto 1",
                Points = normalizedPos1,
                Color = "#FF0000",
                Style = LineStyle.Solid,
                Thickness = 3
            });
            
            lines.Add(new TrackingLine
            {
                Name = "Piloto 2", 
                Points = normalizedPos2,
                Color = "#0000FF",
                Style = LineStyle.Dashed,
                Thickness = 3
            });
            
            // Calcular linha ideal baseada nas duas voltas
            var idealLine = await CalculateIdealLineFromComparison(normalizedPos1, normalizedPos2, lap1, lap2);
            
            lines.Add(new TrackingLine
            {
                Name = "Linha Ideal",
                Points = idealLine,
                Color = "#00FF00",
                Style = LineStyle.Dotted,
                Thickness = 2
            });
            
            return lines;
        }

        public async Task<List<Vector3>> CalculateIdealLineAsync(List<Vector3> trackPoints, List<Corner> corners)
        {
            var idealLine = new List<Vector3>();
            
            if (!trackPoints.Any()) return idealLine;
            
            // Para cada segmento da pista
            for (int i = 0; i < trackPoints.Count - 1; i++)
            {
                var currentPoint = trackPoints[i];
                var nextPoint = trackPoints[i + 1];
                
                // Verificar se há uma curva próxima
                var nearbyCorner = corners.FirstOrDefault(c => 
                    Vector3.Distance(c.ApexPosition, currentPoint) < 50f);
                
                if (nearbyCorner != null)
                {
                    // Calcular linha ideal para a curva
                    var cornerIdealPoints = CalculateCornerIdealLine(nearbyCorner, trackPoints, i);
                    idealLine.AddRange(cornerIdealPoints);
                }
                else
                {
                    // Linha reta - usar centro da pista
                    idealLine.Add(currentPoint);
                }
            }
            
            return idealLine;
        }

        private async Task<List<Vector3>> SmoothTrajectoryAsync(List<Vector3> positions)
        {
            if (positions.Count < 5) return positions;
            
            var smoothed = new List<Vector3>();
            const int windowSize = 5;
            
            // Aplicar filtro de média móvel
            for (int i = 0; i < positions.Count; i++)
            {
                var start = Math.Max(0, i - windowSize / 2);
                var end = Math.Min(positions.Count - 1, i + windowSize / 2);
                
                var avgX = 0f;
                var avgY = 0f;
                var avgZ = 0f;
                var count = 0;
                
                for (int j = start; j <= end; j++)
                {
                    avgX += positions[j].X;
                    avgY += positions[j].Y;
                    avgZ += positions[j].Z;
                    count++;
                }
                
                smoothed.Add(new Vector3(avgX / count, avgY / count, avgZ / count));
            }
            
            // Remover outliers
            return RemoveOutliers(smoothed);
        }

        private List<Vector3> RemoveOutliers(List<Vector3> positions)
        {
            var filtered = new List<Vector3>();
            const float maxDistance = 20f; // Máxima distância entre pontos consecutivos
            
            if (positions.Any())
            {
                filtered.Add(positions.First());
                
                for (int i = 1; i < positions.Count; i++)
                {
                    var distance = Vector3.Distance(positions[i], positions[i - 1]);
                    
                    if (distance <= maxDistance)
                    {
                        filtered.Add(positions[i]);
                    }
                    // Se muito distante, interpolar
                    else if (distance <= maxDistance * 3)
                    {
                        var interpolated = Vector3.Lerp(positions[i - 1], positions[i], 0.5f);
                        filtered.Add(interpolated);
                    }
                }
            }
            
            return filtered;
        }

        private Vector3 DetectStartFinishLine(List<Vector3> positions)
        {
            // Usar primeiro ponto como linha de largada/chegada
            return positions.FirstOrDefault();
        }

        private async Task<List<Corner>> DetectCornersAsync(List<Vector3> trackPoints, List<TelemetryData> sessionData)
        {
            var corners = new List<Corner>();
            
            if (trackPoints.Count < 20) return corners;
            
            const float curvatureThreshold = 0.05f;
            const int lookAheadDistance = 10;
            var cornerCounter = 1;
            
            for (int i = lookAheadDistance; i < trackPoints.Count - lookAheadDistance; i++)
            {
                var curvature = CalculateCurvature(
                    trackPoints[i - lookAheadDistance],
                    trackPoints[i],
                    trackPoints[i + lookAheadDistance]
                );
                
                if (Math.Abs(curvature) > curvatureThreshold)
                {
                    // Verificar se não é muito próximo de uma curva já detectada
                    var tooClose = corners.Any(c => Vector3.Distance(c.ApexPosition, trackPoints[i]) < 30f);
                    
                    if (!tooClose)
                    {
                        var corner = new Corner
                        {
                            Id = cornerCounter++,
                            Name = $"Curva {cornerCounter - 1}",
                            ApexPosition = trackPoints[i],
                            Type = ClassifyCornerType(curvature, trackPoints, i),
                            Radius = CalculateCornerRadius(trackPoints, i, lookAheadDistance),
                            Direction = curvature > 0 ? CornerDirection.Right : CornerDirection.Left,
                            EntryPoint = trackPoints[Math.Max(0, i - lookAheadDistance)],
                            ExitPoint = trackPoints[Math.Min(trackPoints.Count - 1, i + lookAheadDistance)]
                        };
                        
                        // Adicionar dados de telemetria da curva
                        corner.TelemetryData = ExtractCornerTelemetry(sessionData, corner.ApexPosition);
                        
                        corners.Add(corner);
                    }
                }
            }
            
            return MergeNearbyCorners(corners);
        }

        private float CalculateCurvature(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Calcular curvatura usando três pontos
            var v1 = Vector3.Normalize(p2 - p1);
            var v2 = Vector3.Normalize(p3 - p2);
            
            var crossProduct = Vector3.Cross(v1, v2);
            var dotProduct = Vector3.Dot(v1, v2);
            
            var angle = (float)Math.Atan2(crossProduct.Length(), dotProduct);
            return angle * Math.Sign(crossProduct.Y); // Y determina direção
        }

        private CornerType ClassifyCornerType(float curvature, List<Vector3> trackPoints, int index)
        {
            var curvatureMagnitude = Math.Abs(curvature);
            
            if (curvatureMagnitude > 0.3f) return CornerType.Hairpin;
            if (curvatureMagnitude > 0.2f) return CornerType.Slow;
            if (curvatureMagnitude > 0.1f) return CornerType.Medium;
            if (curvatureMagnitude > 0.05f) return CornerType.Fast;
            
            // Verificar se é chicane (mudança rápida de direção)
            if (IsChicane(trackPoints, index))
                return CornerType.Chicane;
                
            return CornerType.Sweeper;
        }

        private bool IsChicane(List<Vector3> trackPoints, int index)
        {
            const int checkDistance = 15;
            
            if (index < checkDistance || index >= trackPoints.Count - checkDistance)
                return false;
            
            var curvature1 = CalculateCurvature(
                trackPoints[index - checkDistance],
                trackPoints[index],
                trackPoints[index + checkDistance / 2]
            );
            
            var curvature2 = CalculateCurvature(
                trackPoints[index + checkDistance / 2],
                trackPoints[index + checkDistance],
                trackPoints[index + checkDistance * 2]
            );
            
            // Chicane = curvaturas opostas próximas
            return Math.Sign(curvature1) != Math.Sign(curvature2) && 
                   Math.Abs(curvature1) > 0.1f && Math.Abs(curvature2) > 0.1f;
        }

        private float CalculateCornerRadius(List<Vector3> trackPoints, int centerIndex, int lookAhead)
        {
            var start = Math.Max(0, centerIndex - lookAhead);
            var end = Math.Min(trackPoints.Count - 1, centerIndex + lookAhead);
            
            var distances = new List<float>();
            var center = trackPoints[centerIndex];
            
            for (int i = start; i <= end; i++)
            {
                distances.Add(Vector3.Distance(center, trackPoints[i]));
            }
            
            return distances.Average();
        }

        private List<Corner> MergeNearbyCorners(List<Corner> corners)
        {
            var merged = new List<Corner>();
            const float mergeDistance = 25f;
            
            foreach (var corner in corners)
            {
                var nearbyCorner = merged.FirstOrDefault(c => 
                    Vector3.Distance(c.ApexPosition, corner.ApexPosition) < mergeDistance);
                
                if (nearbyCorner == null)
                {
                    merged.Add(corner);
                }
                else
                {
                    // Manter a curva com maior curvatura
                    if (Math.Abs(corner.Curvature) > Math.Abs(nearbyCorner.Curvature))
                    {
                        merged.Remove(nearbyCorner);
                        merged.Add(corner);
                    }
                }
            }
            
            return merged.OrderBy(c => c.Id).ToList();
        }

        private List<TrackSector> CalculateTrackSectors(List<Vector3> trackPoints, List<Corner> corners)
        {
            var sectors = new List<TrackSector>();
            var trackLength = CalculateTrackLength(trackPoints);
            var sectorLength = trackLength / 3;
            
            for (int i = 0; i < 3; i++)
            {
                var startDistance = i * sectorLength;
                var endDistance = (i + 1) * sectorLength;
                
                var sector = new TrackSector
                {
                    Number = i + 1,
                    StartDistance = startDistance,
                    EndDistance = endDistance,
                    StartPosition = GetPositionAtDistance(trackPoints, startDistance),
                    EndPosition = GetPositionAtDistance(trackPoints, endDistance),
                    Corners = corners.Where(c => IsCornerInSector(c, startDistance, endDistance, trackPoints)).ToList(),
                    Length = sectorLength
                };
                
                sectors.Add(sector);
            }
            
            return sectors;
        }

        private List<BrakingZone> DetectBrakingZones(List<TelemetryData> sessionData)
        {
            var brakingZones = new List<BrakingZone>();
            var inBrakingZone = false;
            var zoneStart = 0;
            var zoneCounter = 1;
            
            for (int i = 0; i < sessionData.Count; i++)
            {
                var isBraking = sessionData[i].Car.Brake > 0.2f;
                
                if (isBraking && !inBrakingZone)
                {
                    // Início de zona de frenagem
                    inBrakingZone = true;
                    zoneStart = i;
                }
                else if (!isBraking && inBrakingZone)
                {
                    // Fim de zona de frenagem
                    if (i - zoneStart > 5) // Mínimo de 5 pontos para considerar zona válida
                    {
                        var zone = new BrakingZone
                        {
                            Id = zoneCounter++,
                            StartPosition = sessionData[zoneStart].Car.Position,
                            EndPosition = sessionData[i - 1].Car.Position,
                            MaxBrakeForce = sessionData.Skip(zoneStart).Take(i - zoneStart).Max(d => d.Car.Brake),
                            Duration = sessionData[i - 1].Timestamp - sessionData[zoneStart].Timestamp,
                            SpeedReduction = sessionData[zoneStart].Car.Speed - sessionData[i - 1].Car.Speed
                        };
                        
                        brakingZones.Add(zone);
                    }
                    inBrakingZone = false;
                }
            }
            
            return brakingZones;
        }

        private List<AccelerationZone> DetectAccelerationZones(List<TelemetryData> sessionData)
        {
            var accelerationZones = new List<AccelerationZone>();
            var inAccelZone = false;
            var zoneStart = 0;
            var zoneCounter = 1;
            
            for (int i = 1; i < sessionData.Count; i++)
            {
                var isAccelerating = sessionData[i].Car.Throttle > 0.7f && 
                                   sessionData[i].Car.Speed > sessionData[i - 1].Car.Speed;
                
                if (isAccelerating && !inAccelZone)
                {
                    inAccelZone = true;
                    zoneStart = i;
                }
                else if (!isAccelerating && inAccelZone)
                {
                    if (i - zoneStart > 5)
                    {
                        var zone = new AccelerationZone
                        {
                            Id = zoneCounter++,
                            StartPosition = sessionData[zoneStart].Car.Position,
                            EndPosition = sessionData[i - 1].Car.Position,
                            MaxThrottle = sessionData.Skip(zoneStart).Take(i - zoneStart).Max(d => d.Car.Throttle),
                            Duration = sessionData[i - 1].Timestamp - sessionData[zoneStart].Timestamp,
                            SpeedGain = sessionData[i - 1].Car.Speed - sessionData[zoneStart].Car.Speed
                        };
                        
                        accelerationZones.Add(zone);
                    }
                    inAccelZone = false;
                }
            }
            
            return accelerationZones;
        }

        private float CalculateTrackLength(List<Vector3> trackPoints)
        {
            if (trackPoints.Count < 2) return 0;
            
            float totalLength = 0;
            for (int i = 1; i < trackPoints.Count; i++)
            {
                totalLength += Vector3.Distance(trackPoints[i - 1], trackPoints[i]);
            }
            
            return totalLength;
        }

        private List<Vector3> NormalizeByDistance(List<Vector3> positions)
        {
            // Implementar normalização por distância uniforme
            var normalized = new List<Vector3>();
            const float targetSpacing = 5f; // 5 metros entre pontos
            
            if (positions.Count < 2) return positions;
            
            normalized.Add(positions.First());
            var currentDistance = 0f;
            var targetDistance = targetSpacing;
            
            for (int i = 1; i < positions.Count; i++)
            {
                var segmentDistance = Vector3.Distance(positions[i - 1], positions[i]);
                currentDistance += segmentDistance;
                
                if (currentDistance >= targetDistance)
                {
                    // Interpolar posição exata
                    var excess = currentDistance - targetDistance;
                    var ratio = 1f - (excess / segmentDistance);
                    var interpolated = Vector3.Lerp(positions[i - 1], positions[i], ratio);
                    
                    normalized.Add(interpolated);
                    targetDistance += targetSpacing;
                }
            }
            
            return normalized;
        }

        private List<float> ExtractElevationProfile(List<TelemetryData> sessionData)
        {
            return sessionData.Select(d => d.Car.Position.Y).ToList();
        }

        private float EstimateTrackWidth(List<TelemetryData> sessionData)
        {
            // Estimativa baseada na variação lateral das posições
            var lateralPositions = sessionData.Select(d => d.Car.Position.X).ToList();
            return lateralPositions.Max() - lateralPositions.Min();
        }

        private async Task<List<Vector3>> CalculateIdealLineFromComparison(
            List<Vector3> line1, List<Vector3> line2, 
            List<TelemetryData> lap1, List<TelemetryData> lap2)
        {
            var idealLine = new List<Vector3>();
            
            // Escolher pontos da linha mais rápida em cada seção
            var minCount = Math.Min(line1.Count, line2.Count);
            
            for (int i = 0; i < minCount; i++)
            {
                // Comparar velocidades nos pontos correspondentes
                var speed1 = i < lap1.Count ? lap1[i].Car.Speed : 0;
                var speed2 = i < lap2.Count ? lap2[i].Car.Speed : 0;
                
                // Usar posição da volta mais rápida
                if (speed1 > speed2)
                {
                    idealLine.Add(line1[i]);
                }
                else
                {
                    idealLine.Add(line2[i]);
                }
            }
            
            return idealLine;
        }

        private List<Vector3> CalculateCornerIdealLine(Corner corner, List<Vector3> trackPoints, int cornerIndex)
        {
            var idealPoints = new List<Vector3>();
            
            // Calcular linha ideal para a curva baseada na geometria
            var entryIndex = Math.Max(0, cornerIndex - 10);
            var exitIndex = Math.Min(trackPoints.Count - 1, cornerIndex + 10);
            
            for (int i = entryIndex; i <= exitIndex; i++)
            {
                // Para curvas, a linha ideal geralmente está no lado interno
                var offset = corner.Direction == CornerDirection.Left ? -2f : 2f;
                var lateralOffset = new Vector3(offset, 0, 0);
                
                idealPoints.Add(trackPoints[i] + lateralOffset);
            }
            
            return idealPoints;
        }

        private Vector3 GetPositionAtDistance(List<Vector3> trackPoints, float targetDistance)
        {
            if (!trackPoints.Any()) return Vector3.Zero;
            
            var currentDistance = 0f;
            
            for (int i = 1; i < trackPoints.Count; i++)
            {
                var segmentDistance = Vector3.Distance(trackPoints[i - 1], trackPoints[i]);
                
                if (currentDistance + segmentDistance >= targetDistance)
                {
                    // Interpolar dentro do segmento
                    var ratio = (targetDistance - currentDistance) / segmentDistance;
                    return Vector3.Lerp(trackPoints[i - 1], trackPoints[i], ratio);
                }
                
                currentDistance += segmentDistance;
            }
            
            return trackPoints.Last();
        }

        private bool IsCornerInSector(Corner corner, float startDistance, float endDistance, List<Vector3> trackPoints)
        {
            // Calcular distância até o apex da curva
            var cornerDistance = GetDistanceToPosition(trackPoints, corner.ApexPosition);
            return cornerDistance >= startDistance && cornerDistance <= endDistance;
        }

        private float GetDistanceToPosition(List<Vector3> trackPoints, Vector3 targetPosition)
        {
            var minDistance = float.MaxValue;
            var currentDistance = 0f;
            var closestSegmentDistance = 0f;
            
            for (int i = 1; i < trackPoints.Count; i++)
            {
                var segmentDistance = Vector3.Distance(trackPoints[i - 1], trackPoints[i]);
                var distanceToTarget = Vector3.Distance(trackPoints[i], targetPosition);
                
                if (distanceToTarget < minDistance)
                {
                    minDistance = distanceToTarget;
                    closestSegmentDistance = currentDistance + segmentDistance;
                }
                
                currentDistance += segmentDistance;
            }
            
            return closestSegmentDistance;
        }

        private CornerTelemetryData ExtractCornerTelemetry(List<TelemetryData> sessionData, Vector3 apexPosition)
        {
            const float cornerRadius = 30f;
            
            var cornerData = sessionData
                .Where(d => Vector3.Distance(d.Car.Position, apexPosition) <= cornerRadius)
                .ToList();
            
            if (!cornerData.Any()) return new CornerTelemetryData();
            
            return new CornerTelemetryData
            {
                EntrySpeed = cornerData.First().Car.Speed,
                ApexSpeed = cornerData.OrderBy(d => d.Car.Speed).First().Car.Speed,
                ExitSpeed = cornerData.Last().Car.Speed,
                MaxBrakeForce = cornerData.Max(d => d.Car.Brake),
                MaxGForce = cornerData.Max(d => CalculateGForce(d.Car.Acceleration)),
                Duration = cornerData.Last().Timestamp - cornerData.First().Timestamp
            };
        }

        private float CalculateGForce(Vector3 acceleration)
        {
            return acceleration.Length() / 9.81f;
        }

        private void InitializeKnownTracks()
        {
            // Inicializar com dados de pistas conhecidas
            // Implementar conforme necessário
        }

        private TrackLayout ConvertToTrackLayout(TrackMap trackMap)
        {
            return new TrackLayout
            {
                Name = trackMap.Name,
                Length = trackMap.TrackLength,
                SectorCount = trackMap.Sectors.Count,
                CornerCount = trackMap.Corners.Count,
                ConfigurationHash = CalculateTrackHash(trackMap)
            };
        }

        private string CalculateTrackHash(TrackMap trackMap)
        {
            // Gerar hash único para a configuração da pista
            var hashInput = string.Join(",", trackMap.TrackPoints.Take(10).Select(p => $"{p.X:F1},{p.Z:F1}"));
            return hashInput.GetHashCode().ToString();
        }
    }

    // Classe auxiliar para análise geométrica
    public class TrackGeometryAnalyzer
    {
        public List<Vector3> OptimizeTrackLine(List<Vector3> originalLine, List<Corner> corners)
        {
            var optimized = new List<Vector3>(originalLine);
            
            foreach (var corner in corners)
            {
                OptimizeCornerLine(optimized, corner);
            }
            
            return optimized;
        }

        private void OptimizeCornerLine(List<Vector3> line, Corner corner)
        {
            // Implementar otimização da linha para cada curva
            // Aplicar princípios de racing line (late apex, etc.)
        }
    }


 public class TrackMap
    {
        public string Name { get; set; } = string.Empty;
        public List<Vector3> TrackPoints { get; set; } = new();
        public Vector3 StartFinishLine { get; set; }
        public float TrackLength { get; set; }
        public List<Corner> Corners { get; set; } = new();
        public List<TrackSector> Sectors { get; set; } = new();
        public List<BrakingZone> BrakingZones { get; set; } = new();
        public List<AccelerationZone> AccelerationZones { get; set; } = new();
        public List<Vector3> IdealLine { get; set; } = new();
        public List<float> Elevation { get; set; } = new();
        public float TrackWidth { get; set; }
    }

    public class TrackingLine
    {
        public string Name { get; set; } = string.Empty;
        public List<Vector3> Points { get; set; } = new();
        public string Color { get; set; } = "#FFFFFF";
        public LineStyle Style { get; set; } = LineStyle.Solid;
        public int Thickness { get; set; } = 2;
    }

    public class TrackSector
    {
        public int Number { get; set; }
        public float StartDistance { get; set; }
        public float EndDistance { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 EndPosition { get; set; }
        public List<Corner> Corners { get; set; } = new();
        public float Length { get; set; }
    }

    public class BrakingZone
    {
        public int Id { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 EndPosition { get; set; }
        public float MaxBrakeForce { get; set; }
        public TimeSpan Duration { get; set; }
        public float SpeedReduction { get; set; }
    }

    public class AccelerationZone
    {
        public int Id { get; set; }
        public Vector3 StartPosition { get; set; }
        public Vector3 EndPosition { get; set; }
        public float MaxThrottle { get; set; }
        public TimeSpan Duration { get; set; }
        public float SpeedGain { get; set; }
    }

    public class CornerTelemetryData
    {
        public float EntrySpeed { get; set; }
        public float ApexSpeed { get; set; }
        public float ExitSpeed { get; set; }
        public float MaxBrakeForce { get; set; }
        public float MaxGForce { get; set; }
        public TimeSpan Duration { get; set; }
    }

    public class TrackLayout
    {
        public string Name { get; set; } = string.Empty;
        public float Length { get; set; }
        public int SectorCount { get; set; }
        public int CornerCount { get; set; }
        public string ConfigurationHash { get; set; } = string.Empty;
    }

    // Enums
    public enum LineStyle
    {
        Solid,
        Dashed,
        Dotted,
        DashDot
    }

    public enum CornerDirection
    {
        Left,
        Right
    }

    // Extensões da classe Corner original
    public partial class Corner
    {
        public float Curvature { get; set; }
        public CornerDirection Direction { get; set; }
        public Vector3 EntryPoint { get; set; }
        public Vector3 ExitPoint { get; set; }
        public CornerTelemetryData TelemetryData { get; set; } = new();
    }
}