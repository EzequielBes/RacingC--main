using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class TrackMapControl : UserControl
    {
        private TrackMap _currentTrackMap;
        private List<TrackingLine> _trackingLines = new();
        private Vector3 _currentCarPosition;
        private readonly Dictionary<string, Brush> _lineBrushes = new();
        private double _scaleFactor = 1.0;
        private Point _trackCenter;
        private readonly List<UIElement> _trackElements = new();

        public TrackMapControl()
        {
            InitializeComponent();
            InitializeBrushes();
        }

        private void InitializeBrushes()
        {
            _lineBrushes["track"] = new SolidColorBrush(Colors.White);
            _lineBrushes["ideal"] = new SolidColorBrush(Colors.LimeGreen);
            _lineBrushes["player1"] = new SolidColorBrush(Colors.Red);
            _lineBrushes["player2"] = new SolidColorBrush(Colors.Blue);
            _lineBrushes["sectors"] = new SolidColorBrush(Colors.Yellow);
            _lineBrushes["braking"] = new SolidColorBrush(Colors.Orange);
        }

        public void LoadTrackMap(TrackMap trackMap)
        {
            _currentTrackMap = trackMap;
            
            Dispatcher.Invoke(() =>
            {
                TrackNameText.Text = trackMap.Name;
                TrackLengthText.Text = $"{trackMap.TrackLength:F0}m";
                
                DrawTrackMap();
            });
        }

        public void UpdateTrackingLines(List<TrackingLine> lines)
        {
            _trackingLines = lines;
            
            Dispatcher.Invoke(() =>
            {
                DrawTrackingLines();
            });
        }

        public void UpdatePosition(Vector3 position)
        {
            _currentCarPosition = position;
            
            Dispatcher.Invoke(() =>
            {
                UpdateCarPosition();
            });
        }

        private void DrawTrackMap()
        {
            if (_currentTrackMap?.TrackPoints == null || !_currentTrackMap.TrackPoints.Any())
                return;

            ClearCanvas();
            CalculateScaleAndCenter();
            
            // Draw track outline
            DrawTrackOutline();
            
            // Draw sectors
            DrawSectors();
            
            // Draw corners
            DrawCorners();
            
            // Draw braking zones
            DrawBrakingZones();
            
            // Draw start/finish line
            DrawStartFinishLine();
        }

        private void ClearCanvas()
        {
            TrackCanvas.Children.Clear();
            _trackElements.Clear();
        }

        private void CalculateScaleAndCenter()
        {
            if (!_currentTrackMap.TrackPoints.Any()) return;

            var minX = _currentTrackMap.TrackPoints.Min(p => p.X);
            var maxX = _currentTrackMap.TrackPoints.Max(p => p.X);
            var minZ = _currentTrackMap.TrackPoints.Min(p => p.Z);
            var maxZ = _currentTrackMap.TrackPoints.Max(p => p.Z);

            var trackWidth = maxX - minX;
            var trackHeight = maxZ - minZ;

            var canvasWidth = TrackCanvas.Width - 40; // Margin
            var canvasHeight = TrackCanvas.Height - 40;

            var scaleX = canvasWidth / trackWidth;
            var scaleZ = canvasHeight / trackHeight;
            
            _scaleFactor = Math.Min(scaleX, scaleZ) * 0.8; // 80% to leave margin

            _trackCenter = new Point(
                (minX + maxX) / 2,
                (minZ + maxZ) / 2
            );
        }

        private void DrawTrackOutline()
        {
            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            
            var firstPoint = WorldToCanvas(_currentTrackMap.TrackPoints.First());
            pathFigure.StartPoint = firstPoint;
            
            foreach (var point in _currentTrackMap.TrackPoints.Skip(1))
            {
                var canvasPoint = WorldToCanvas(point);
                pathFigure.Segments.Add(new LineSegment(canvasPoint, true));
            }

            pathGeometry.Figures.Add(pathFigure);

            var trackPath = new Path
            {
                Data = pathGeometry,
                Stroke = _lineBrushes["track"],
                StrokeThickness = 4,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            TrackCanvas.Children.Add(trackPath);
            _trackElements.Add(trackPath);
        }

        private void DrawSectors()
        {
            if (_currentTrackMap.Sectors == null) return;

            foreach (var sector in _currentTrackMap.Sectors)
            {
                var startPoint = WorldToCanvas(sector.StartPosition);
                var endPoint = WorldToCanvas(sector.EndPosition);

                // Sector boundary line
                var sectorLine = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = startPoint.X,
                    Y2 = startPoint.Y - 20, // Vertical line upward
                    Stroke = _lineBrushes["sectors"],
                    StrokeThickness = 3
                };

                TrackCanvas.Children.Add(sectorLine);
                _trackElements.Add(sectorLine);

                // Sector number
                var sectorText = new TextBlock
                {
                    Text = $"S{sector.Number}",
                    Foreground = _lineBrushes["sectors"],
                    FontSize = 12,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(sectorText, startPoint.X - 10);
                Canvas.SetTop(sectorText, startPoint.Y - 35);

                TrackCanvas.Children.Add(sectorText);
                _trackElements.Add(sectorText);
            }
        }

        private void DrawCorners()
        {
            if (_currentTrackMap.Corners == null) return;

            foreach (var corner in _currentTrackMap.Corners)
            {
                var cornerPoint = WorldToCanvas(corner.ApexPosition);

                var cornerMarker = new Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = corner.Direction == CornerDirection.Left ? 
                           new SolidColorBrush(Colors.Cyan) : 
                           new SolidColorBrush(Colors.Magenta),
                    Stroke = new SolidColorBrush(Colors.White),
                    StrokeThickness = 1
                };

                Canvas.SetLeft(cornerMarker, cornerPoint.X - 4);
                Canvas.SetTop(cornerMarker, cornerPoint.Y - 4);

                TrackCanvas.Children.Add(cornerMarker);
                _trackElements.Add(cornerMarker);

                // Corner number
                var cornerText = new TextBlock
                {
                    Text = corner.Id.ToString(),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontSize = 8,
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(cornerText, cornerPoint.X + 8);
                Canvas.SetTop(cornerText, cornerPoint.Y - 8);

                TrackCanvas.Children.Add(cornerText);
                _trackElements.Add(cornerText);
            }
        }

        private void DrawBrakingZones()
        {
            if (_currentTrackMap.BrakingZones == null) return;

            foreach (var zone in _currentTrackMap.BrakingZones)
            {
                var startPoint = WorldToCanvas(zone.StartPosition);
                var endPoint = WorldToCanvas(zone.EndPosition);

                var brakingLine = new Line
                {
                    X1 = startPoint.X,
                    Y1 = startPoint.Y,
                    X2 = endPoint.X,
                    Y2 = endPoint.Y,
                    Stroke = _lineBrushes["braking"],
                    StrokeThickness = 6,
                    Opacity = 0.7
                };

                TrackCanvas.Children.Add(brakingLine);
                _trackElements.Add(brakingLine);
            }
        }

        private void DrawStartFinishLine()
        {
            if (_currentTrackMap.StartFinishLine == default) return;

            var startFinishPoint = WorldToCanvas(_currentTrackMap.StartFinishLine);

            var startFinishLine = new Rectangle
            {
                Width = 20,
                Height = 4,
                Fill = new SolidColorBrush(Colors.White),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 1
            };

            Canvas.SetLeft(startFinishLine, startFinishPoint.X - 10);
            Canvas.SetTop(startFinishLine, startFinishPoint.Y - 2);

            TrackCanvas.Children.Add(startFinishLine);
            _trackElements.Add(startFinishLine);

            // S/F text
            var sfText = new TextBlock
            {
                Text = "S/F",
                Foreground = new SolidColorBrush(Colors.White),
                FontSize = 10,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(sfText, startFinishPoint.X - 12);
            Canvas.SetTop(sfText, startFinishPoint.Y + 8);

            TrackCanvas.Children.Add(sfText);
            _trackElements.Add(sfText);
        }

        private void DrawTrackingLines()
        {
            // Remove existing tracking lines
            var linesToRemove = _trackElements.OfType<Path>()
                .Where(p => p.Tag?.ToString().StartsWith("tracking_") == true)
                .ToList();

            foreach (var line in linesToRemove)
            {
                TrackCanvas.Children.Remove(line);
                _trackElements.Remove(line);
            }

            // Draw new tracking lines
            foreach (var trackingLine in _trackingLines)
            {
                if (trackingLine.Points == null || !trackingLine.Points.Any())
                    continue;

                var pathGeometry = new PathGeometry();
                var pathFigure = new PathFigure();

                var firstPoint = WorldToCanvas(trackingLine.Points.First());
                pathFigure.StartPoint = firstPoint;

                foreach (var point in trackingLine.Points.Skip(1))
                {
                    var canvasPoint = WorldToCanvas(point);
                    pathFigure.Segments.Add(new LineSegment(canvasPoint, true));
                }

                pathGeometry.Figures.Add(pathFigure);

                var linePath = new Path
                {
                    Data = pathGeometry,
                    Stroke = (Brush)new BrushConverter().ConvertFrom(trackingLine.Color),
                    StrokeThickness = trackingLine.Thickness,
                    StrokeLineJoin = PenLineJoin.Round,
                    Tag = $"tracking_{trackingLine.Name}"
                };

                // Apply line style
                switch (trackingLine.Style)
                {
                    case LineStyle.Dashed:
                        linePath.StrokeDashArray = new DoubleCollection { 5, 3 };
                        break;
                    case LineStyle.Dotted:
                        linePath.StrokeDashArray = new DoubleCollection { 2, 2 };
                        break;
                    case LineStyle.DashDot:
                        linePath.StrokeDashArray = new DoubleCollection { 5, 3, 2, 3 };
                        break;
                }

                TrackCanvas.Children.Add(linePath);
                _trackElements.Add(linePath);
            }
        }

        private void UpdateCarPosition()
        {
            if (_currentCarPosition == default) return;

            // Remove existing car marker
            var existingCar = _trackElements.OfType<Ellipse>()
                .FirstOrDefault(e => e.Tag?.ToString() == "current_car");

            if (existingCar != null)
            {
                TrackCanvas.Children.Remove(existingCar);
                _trackElements.Remove(existingCar);
            }

            // Add new car marker
            var carPosition = WorldToCanvas(_currentCarPosition);

            var carMarker = new Ellipse
            {
                Width = 12,
                Height = 12,
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.White),
                StrokeThickness = 2,
                Tag = "current_car"
            };

            Canvas.SetLeft(carMarker, carPosition.X - 6);
            Canvas.SetTop(carMarker, carPosition.Y - 6);

            TrackCanvas.Children.Add(carMarker);
            _trackElements.Add(carMarker);
        }

        private Point WorldToCanvas(Vector3 worldPosition)
        {
            var x = (worldPosition.X - _trackCenter.X) * _scaleFactor + TrackCanvas.Width / 2;
            var y = (worldPosition.Z - _trackCenter.Y) * _scaleFactor + TrackCanvas.Height / 2;
            
            return new Point(x, y);
        }

        private Vector3 CanvasToWorld(Point canvasPoint)
        {
            var x = (canvasPoint.X - TrackCanvas.Width / 2) / _scaleFactor + _trackCenter.X;
            var z = (canvasPoint.Y - TrackCanvas.Height / 2) / _scaleFactor + _trackCenter.Y;
            
            return new Vector3((float)x, 0, (float)z);
        }

        // Event Handlers
        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            MapScrollViewer.ZoomToFactor(MapScrollViewer.ZoomFactor * 1.2);
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            MapScrollViewer.ZoomToFactor(MapScrollViewer.ZoomFactor * 0.8);
        }

        private void FitToScreenButton_Click(object sender, RoutedEventArgs e)
        {
            MapScrollViewer.ZoomToFactor(1.0);
        }

        private void TrackCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_currentTrackMap?.TrackPoints == null) return;

            var mousePos = e.GetPosition(TrackCanvas);
            var worldPos = CanvasToWorld(mousePos);

            // Find nearest track element for tooltip
            var nearestCorner = _currentTrackMap.Corners?
                .OrderBy(c => Vector3.Distance(c.ApexPosition, worldPos))
                .FirstOrDefault();

            if (nearestCorner != null && Vector3.Distance(nearestCorner.ApexPosition, worldPos) < 50)
            {
                TooltipTitle.Text = nearestCorner.Name;
                TooltipContent.Text = $"Type: {nearestCorner.Type}\nRadius: {nearestCorner.Radius:F1}m";
                TrackTooltip.IsOpen = true;
            }
            else
            {
                TrackTooltip.IsOpen = false;
            }
        }

        private void TrackCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            TrackTooltip.IsOpen = false;
        }
    }
}
