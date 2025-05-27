public class PatternAnalyzer
{
    public async Task<List<DrivingPattern>> AnalyzePatternsAsync(List<TelemetryData> sessionData)
    {
        var patterns = new List<DrivingPattern>();
        
        // Detectar padrões de frenagem
        patterns.AddRange(DetectBrakingPatterns(sessionData));
        
        // Detectar padrões de aceleração
        patterns.AddRange(DetectAccelerationPatterns(sessionData));
        
        // Detectar padrões de curva
        patterns.AddRange(DetectCorneringPatterns(sessionData));
        
        return patterns;
    }

    private List<DrivingPattern> DetectBrakingPatterns(List<TelemetryData> data)
    {
        var patterns = new List<DrivingPattern>();
        
        // Algoritmo para detectar pontos de frenagem inconsistentes
        var brakingPoints = data.Where(d => d.Car.Brake > 0.1f).ToList();
        
        foreach (var corner in DetectCorners(data))
        {
            var cornerBrakingData = brakingPoints
                .Where(bp => IsNearCorner(bp, corner))
                .ToList();
                
            if (cornerBrakingData.Count > 5) // Mínimo de amostras
            {
                var consistency = CalculateBrakingConsistency(cornerBrakingData);
                
                if (consistency < 0.7f) // Baixa consistência
                {
                    patterns.Add(new DrivingPattern
                    {
                        Type = PatternType.InconsistentBraking,
                        Location = corner.Position,
                        Severity = 1.0f - consistency,
                        Description = "Ponto de frenagem inconsistente nesta curva"
                    });
                }
            }
        }
        
        return patterns;
    }

    public List<ImprovementSuggestion> GenerateImprovementSuggestions(
        List<DrivingPattern> patterns, 
        LapAnalysis currentLap, 
        LapAnalysis referenceLap)
    {
        var suggestions = new List<ImprovementSuggestion>();
        
        // Sugestões baseadas em padrões detectados
        foreach (var pattern in patterns)
        {
            switch (pattern.Type)
            {
                case PatternType.InconsistentBraking:
                    suggestions.Add(new ImprovementSuggestion
                    {
                        Category = "Frenagem",
                        Description = $"Trabalhe na consistência do ponto de frenagem na curva {pattern.Location}",
                        Priority = pattern.Severity,
                        ExpectedGain = EstimateTimeGain(pattern)
                    });
                    break;
                    
                case PatternType.LateApex:
                    suggestions.Add(new ImprovementSuggestion
                    {
                        Category = "Traçado",
                        Description = "Tente fazer o apex mais cedo para melhor saída de curva",
                        Priority = pattern.Severity
                    });
                    break;
            }
        }
        
        // Sugestões baseadas em comparação com volta de referência
        if (referenceLap != null)
        {
            suggestions.AddRange(CompareWithReference(currentLap, referenceLap));
        }
        
        return suggestions.OrderByDescending(s => s.Priority).ToList();
    }
}