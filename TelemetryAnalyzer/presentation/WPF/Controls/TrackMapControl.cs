using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Numerics;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

public partial class TrackMapControl : UserControl
{
    public TrackMap TrackMap { get; set; }
    public Vector3 CurrentPosition { get; set; }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        
        if (TrackMap?.TrackPoints == null) return;

        // Desenhar pista
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            var points = TrackMap.TrackPoints.Select(p => 
                new Point(p.X * ScaleFactor + OffsetX, p.Z * ScaleFactor + OffsetY)).ToArray();
            
            context.BeginFigure(points[0], false, false);
            context.PolyLineTo(points, true, false);
        }
        
        drawingContext.DrawGeometry(null, new Pen(Brushes.White, 3), geometry);
        
        // Desenhar posição atual do carro
        if (CurrentPosition != default)
        {
            var carPoint = new Point(
                CurrentPosition.X * ScaleFactor + OffsetX,
                CurrentPosition.Z * ScaleFactor + OffsetY);
            
            drawingContext.DrawEllipse(Brushes.Red, null, carPoint, 5, 5);
        }
        
        // Desenhar curvas importantes
        foreach (var corner in TrackMap.Corners)
        {
            var cornerPoint = new Point(
                corner.Position.X * ScaleFactor + OffsetX,
                corner.Position.Z * ScaleFactor + OffsetY);
            
            var brush = corner.Type == CornerType.Left ? Brushes.Blue : Brushes.Green;
            drawingContext.DrawEllipse(brush, null, cornerPoint, 3, 3);
        }
    }
}