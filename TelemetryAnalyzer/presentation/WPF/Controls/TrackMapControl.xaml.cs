using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TelemetryAnalyzer.Application.Services; // Assuming TrackMapService exists
using TelemetryAnalyzer.Core.Models; // For TrackMap, TrackingLine, TelemetryData etc.
using TelemetryAnalyzer.Core.Models.LapAnalysis; // For LapData
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using TelemetryAnalyzer.Presentation.WPF.Converters; // For GearToStringConverter

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class TrackMapControl : UserControl
    {
        // Dependency Property for TrackMap Data
        public static readonly DependencyProperty TrackMapDataProperty =
            DependencyProperty.Register("TrackMapData", typeof(TrackMap), typeof(TrackMapControl),
            new PropertyMetadata(null, OnTrackMapDataChanged));

        public TrackMap TrackMapData
        {
            get { return (TrackMap)GetValue(TrackMapDataProperty); }
            set { SetValue(TrackMapDataProperty, value); }
        }

        // Dependency Property for Tracking Lines (ObservableCollection for dynamic updates)
        public static readonly DependencyProperty TrackingLinesProperty =
            DependencyProperty.Register("TrackingLines", typeof(ObservableCollection<TrackingLine>), typeof(TrackMapControl),
            new PropertyMetadata(new ObservableCollection<TrackingLine>(), OnTrackingLinesChanged));

        public ObservableCollection<TrackingLine> TrackingLines
        {
            get { return (ObservableCollection<TrackingLine>)GetValue(TrackingLinesProperty); }
            set { SetValue(TrackingLinesProperty, value); }
        }

        // Dependency Property for Current Telemetry Data (includes position, speed, gear etc.)
        public static readonly DependencyProperty CurrentTelemetryDataProperty =
            DependencyProperty.Register("CurrentTelemetryData", typeof(TelemetryData), typeof(TrackMapControl),
            new PropertyMetadata(null, OnCurrentTelemetryDataChanged));

        public TelemetryData CurrentTelemetryData
        {
            get { return (TelemetryData)GetValue(CurrentTelemetryDataProperty); }
            set { SetValue(CurrentTelemetryDataProperty, value); }
        }

        private readonly Dictionary<string, Brush> _lineBrushes = new();
        private double _scaleFactor = 1.0;
        private Point _trackCenter;
        private readonly List<UIElement> _trackElements = new(); // Elements related to the base track map
        private readonly Dictionary<TrackingLine, Path> _trackingLinePaths = new(); // Map TrackingLine object to its Path element
        private Ellipse _carMarker;
        private TextBlock _carSpeedText;
        private TextBlock _carGearText;
        private readonly GearToStringConverter _gearConverter = new GearToStringConverter();

        public TrackMapControl()
        {
            InitializeComponent();
            InitializeBrushes();
            // Ensure the TrackingLines collection is initialized
            if (TrackingLines == null)
            {
                TrackingLines = new ObservableCollection<TrackingLine>();
            }
            TrackingLines.CollectionChanged += TrackingLines_CollectionChanged;
            this.SizeChanged += TrackMapControl_SizeChanged; // Recalculate scale on resize
        }

        private void InitializeBrushes()
        {
            // Define default brushes, could be customizable later
            _lineBrushes["track"] = Brushes.LightGray;
            _lineBrushes["sectors"] = Brushes.Yellow;
            _lineBrushes["corners"] = Brushes.Cyan;
            _lineBrushes["braking"] = Brushes.OrangeRed;
            _lineBrushes["startfinish"] = Brushes.White;
            // Default brushes for tracking lines if not specified
            _lineBrushes["defaultLine1"] = Brushes.Red;
            _lineBrushes["defaultLine2"] = Brushes.Blue;
            _lineBrushes["defaultLine3"] = Brushes.Green;
            _lineBrushes["defaultLine4"] = Brushes.Magenta;
        }

        private static void OnTrackMapDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackMapControl control && e.NewValue is TrackMap trackMap)
            {
                control.LoadTrackMapInternal(trackMap);
            }
            else if (d is TrackMapControl control)
            {
                 control.ClearCanvas(); // Clear if track map is null
            }
        }

        private static void OnTrackingLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackMapControl control)
            {
                // Unsubscribe from old collection's changes
                if (e.OldValue is ObservableCollection<TrackingLine> oldCollection)
                {
                    oldCollection.CollectionChanged -= control.TrackingLines_CollectionChanged;
                }
                // Subscribe to new collection's changes
                if (e.NewValue is ObservableCollection<TrackingLine> newCollection)
                {
                    newCollection.CollectionChanged += control.TrackingLines_CollectionChanged;
                }
                // Redraw all lines from the new collection
                control.RedrawAllTrackingLines();
            }
        }

        private void TrackingLines_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Handle changes to the TrackingLines collection (add, remove, replace, reset)
            Dispatcher.Invoke(() =>
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    ClearTrackingLinePaths();
                }
                if (e.OldItems != null)
                {
                    foreach (TrackingLine oldLine in e.OldItems)
                    {
                        RemoveTrackingLinePath(oldLine);
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (TrackingLine newLine in e.NewItems)
                    {
                        DrawSingleTrackingLine(newLine);
                    }
                }
            });
        }

        // Changed from OnCurrentCarPositionChanged
        private static void OnCurrentTelemetryDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TrackMapControl control && e.NewValue is TelemetryData telemetryData)
            {
                control.UpdateCarPositionAndData(telemetryData);
            }
             else if (d is TrackMapControl control)
            {
                 // Clear marker if data is null
                 control.UpdateCarPositionAndData(null);
            }
        }

        private void TrackMapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Redraw the map when the control size changes to maintain aspect ratio and fit
            if (TrackMapData != null)
            {
                LoadTrackMapInternal(TrackMapData);
                RedrawAllTrackingLines(); // Also redraw tracking lines
                UpdateCarPositionAndData(CurrentTelemetryData); // And car position/data
            }
        }

        private void LoadTrackMapInternal(TrackMap trackMap)
        {
            Dispatcher.Invoke(() =>
            {
                TrackNameText.Text = trackMap?.Name ?? "No Track Loaded";
                TrackLengthText.Text = trackMap != null ? $"{trackMap.TrackLength:F0}m" : "";
                DrawTrackMapBase(trackMap);
            });
        }

        private void DrawTrackMapBase(TrackMap trackMap)
        {
            ClearCanvas();
            if (trackMap?.TrackPoints == null || !trackMap.TrackPoints.Any())
                return;

            CalculateScaleAndCenter(trackMap);

            // Draw track outline
            DrawTrackOutline(trackMap);
            // Draw sectors, corners, etc. (add methods as needed)
            DrawSectors(trackMap);
            DrawStartFinishLine(trackMap);
            // DrawCorners(trackMap); // Add if needed
            // DrawBrakingZones(trackMap); // Add if needed
        }

        private void ClearCanvas()
        {
            TrackCanvas.Children.Clear();
            _trackElements.Clear();
            _trackingLinePaths.Clear();
            _carMarker = null;
            _carSpeedText = null;
            _carGearText = null;
        }

        private void ClearTrackingLinePaths()
        {
             foreach (var path in _trackingLinePaths.Values)
             {
                 TrackCanvas.Children.Remove(path);
             }
             _trackingLinePaths.Clear();
        }

        private void RemoveTrackingLinePath(TrackingLine line)
        {
            if (_trackingLinePaths.TryGetValue(line, out var path))
            {
                TrackCanvas.Children.Remove(path);
                _trackingLinePaths.Remove(line);
            }
        }

        private void CalculateScaleAndCenter(TrackMap trackMap)
        {
            if (trackMap?.TrackPoints == null || !trackMap.TrackPoints.Any()) return;

            var minX = trackMap.TrackPoints.Min(p => p.X);
            var maxX = trackMap.TrackPoints.Max(p => p.X);
            var minZ = trackMap.TrackPoints.Min(p => p.Z); // Assuming Z is the vertical axis on the map
            var maxZ = trackMap.TrackPoints.Max(p => p.Z);

            var trackWidth = Math.Abs(maxX - minX);
            var trackHeight = Math.Abs(maxZ - minZ);

            if (trackWidth < 1 || trackHeight < 1) // Avoid division by zero/tiny tracks
            {
                 _scaleFactor = 1.0;
                 _trackCenter = new Point(0,0);
                 return;
            }

            // Use ActualWidth/Height if available and > 0, otherwise fallback
            var canvasWidth = ActualWidth > 0 ? ActualWidth - 40 : TrackCanvas.Width - 40; // Margin
            var canvasHeight = ActualHeight > 0 ? ActualHeight - 40 : TrackCanvas.Height - 40;
            if (canvasWidth <= 0) canvasWidth = 600; // Default fallback
            if (canvasHeight <= 0) canvasHeight = 400;

            var scaleX = canvasWidth / trackWidth;
            var scaleZ = canvasHeight / trackHeight;

            _scaleFactor = Math.Min(scaleX, scaleZ) * 0.9; // 90% to leave margin

            _trackCenter = new Point(
                (minX + maxX) / 2,
                (minZ + maxZ) / 2
            );
        }

        private Path CreatePath(Brush stroke, double thickness, string tag = null)
        {
            var path = new Path
            {
                Stroke = stroke,
                StrokeThickness = thickness,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                Tag = tag
            };
            return path;
        }

        private void DrawTrackOutline(TrackMap trackMap)
        {
            var pathGeometry = CreatePathGeometry(trackMap.TrackPoints);
            if (pathGeometry == null) return;

            var trackPath = CreatePath(_lineBrushes["track"], 3, "track_outline");
            trackPath.Data = pathGeometry;

            TrackCanvas.Children.Add(trackPath);
            _trackElements.Add(trackPath);
        }

        private void DrawSectors(TrackMap trackMap)
        {
            if (trackMap?.Sectors == null) return;

            foreach (var sector in trackMap.Sectors)
            {
                // Assuming Sector has StartPosition and EndPosition defining the line
                var startPoint = WorldToCanvas(sector.StartPosition);
                // var endPoint = WorldToCanvas(sector.EndPosition); // Need sector line definition

                // Draw a simple marker at the start position for now
                var sectorLine = new Line
                {
                    X1 = startPoint.X - 5, Y1 = startPoint.Y - 10,
                    X2 = startPoint.X + 5, Y2 = startPoint.Y + 10,
                    Stroke = _lineBrushes["sectors"],
                    StrokeThickness = 2,
                    Tag = $"sector_{sector.Number}"
                };
                TrackCanvas.Children.Add(sectorLine);
                _trackElements.Add(sectorLine);

                var sectorText = new TextBlock
                {
                    Text = $"S{sector.Number}",
                    Foreground = _lineBrushes["sectors"],
                    FontSize = 10,
                    FontWeight = FontWeights.Bold
                };
                Canvas.SetLeft(sectorText, startPoint.X + 8);
                Canvas.SetTop(sectorText, startPoint.Y - 8);
                TrackCanvas.Children.Add(sectorText);
                _trackElements.Add(sectorText);
            }
        }

        private void DrawStartFinishLine(TrackMap trackMap)
        {
            if (trackMap?.StartFinishLine == default) return;

            var sfPoint = WorldToCanvas(trackMap.StartFinishLine);
            var sfLine = new Line
            {
                 X1 = sfPoint.X - 15, Y1 = sfPoint.Y,
                 X2 = sfPoint.X + 15, Y2 = sfPoint.Y,
                 Stroke = _lineBrushes["startfinish"],
                 StrokeThickness = 4,
                 StrokeDashArray = new DoubleCollection { 4, 4 },
                 Tag = "start_finish"
            };
            TrackCanvas.Children.Add(sfLine);
            _trackElements.Add(sfLine);
        }

        private void RedrawAllTrackingLines()
        {
            ClearTrackingLinePaths();
            if (TrackingLines == null) return;

            foreach (var trackingLine in TrackingLines)
            {
                DrawSingleTrackingLine(trackingLine);
            }
        }

        private void DrawSingleTrackingLine(TrackingLine trackingLine)
        {
            if (trackingLine?.Points == null || !trackingLine.Points.Any())
                return;

            var pathGeometry = CreatePathGeometry(trackingLine.Points);
            if (pathGeometry == null) return;

            Brush strokeBrush;
            if (!string.IsNullOrEmpty(trackingLine.Color))
            {
                try { strokeBrush = (Brush)new BrushConverter().ConvertFrom(trackingLine.Color); }
                catch { strokeBrush = GetDefaultLineBrush(_trackingLinePaths.Count); }
            }
            else
            {
                strokeBrush = GetDefaultLineBrush(_trackingLinePaths.Count);
            }

            var linePath = CreatePath(strokeBrush, trackingLine.Thickness, $"tracking_{trackingLine.Name ?? Guid.NewGuid().ToString()}");
            linePath.Data = pathGeometry;

            // Apply line style
            switch (trackingLine.Style)
            {
                case LineStyle.Dashed: linePath.StrokeDashArray = new DoubleCollection { 5, 3 }; break;
                case LineStyle.Dotted: linePath.StrokeDashArray = new DoubleCollection { 1, 2 }; break;
                case LineStyle.DashDot: linePath.StrokeDashArray = new DoubleCollection { 5, 3, 1, 3 }; break;
            }

            TrackCanvas.Children.Add(linePath);
            _trackingLinePaths[trackingLine] = linePath; // Store reference
        }

        private Brush GetDefaultLineBrush(int index)
        {
            var brushes = new[] { _lineBrushes["defaultLine1"], _lineBrushes["defaultLine2"], _lineBrushes["defaultLine3"], _lineBrushes["defaultLine4"] };
            return brushes[index % brushes.Length];
        }

        private PathGeometry CreatePathGeometry(IEnumerable<Vector3> worldPoints)
        {
            if (worldPoints == null || !worldPoints.Any()) return null;

            var pathGeometry = new PathGeometry();
            var pathFigure = new PathFigure();
            bool first = true;

            foreach (var point in worldPoints)
            {
                var canvasPoint = WorldToCanvas(point);
                if (first)
                {
                    pathFigure.StartPoint = canvasPoint;
                    first = false;
                }
                else
                {
                    pathFigure.Segments.Add(new LineSegment(canvasPoint, true));
                }
            }
            // Don't close the figure for tracking lines
            // pathFigure.IsClosed = true;
            pathGeometry.Figures.Add(pathFigure);
            return pathGeometry;
        }

        // Updated method to handle TelemetryData
        private void UpdateCarPositionAndData(TelemetryData telemetryData)
        {
             Dispatcher.Invoke(() =>
             {
                // Clear or hide existing marker and text if data is null
                if (telemetryData == null || telemetryData.Car == null || telemetryData.Car.Position == default)
                {
                    if (_carMarker != null) _carMarker.Visibility = Visibility.Collapsed;
                    if (_carSpeedText != null) _carSpeedText.Visibility = Visibility.Collapsed;
                    if (_carGearText != null) _carGearText.Visibility = Visibility.Collapsed;
                    return;
                }

                var worldPosition = telemetryData.Car.Position;
                var carCanvasPos = WorldToCanvas(worldPosition);

                // Create or update car marker
                if (_carMarker == null)
                {
                    _carMarker = new Ellipse
                    {
                        Width = 10,
                        Height = 10,
                        Fill = Brushes.OrangeRed, // Use a distinct color
                        Stroke = Brushes.White,
                        StrokeThickness = 1.5,
                        Tag = "current_car"
                    };
                    TrackCanvas.Children.Add(_carMarker);
                    Panel.SetZIndex(_carMarker, 100); // Ensure it's on top
                }
                _carMarker.Visibility = Visibility.Visible;
                Canvas.SetLeft(_carMarker, carCanvasPos.X - (_carMarker.Width / 2));
                Canvas.SetTop(_carMarker, carCanvasPos.Y - (_carMarker.Height / 2));

                // Create or update speed text
                if (_carSpeedText == null)
                {
                    _carSpeedText = new TextBlock
                    {
                        FontSize = 9,
                        Foreground = Brushes.White,
                        Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), // Semi-transparent black
                        Padding = new Thickness(2,1),
                        Tag = "car_speed_text"
                    };
                    TrackCanvas.Children.Add(_carSpeedText);
                    Panel.SetZIndex(_carSpeedText, 101);
                }
                _carSpeedText.Text = $"{telemetryData.Car.Speed:F0} km/h";
                _carSpeedText.Visibility = Visibility.Visible;
                Canvas.SetLeft(_carSpeedText, carCanvasPos.X + 8); // Position relative to marker
                Canvas.SetTop(_carSpeedText, carCanvasPos.Y - 12);

                // Create or update gear text
                if (_carGearText == null)
                {
                    _carGearText = new TextBlock
                    {
                        FontSize = 10,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Yellow,
                        Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
                        Padding = new Thickness(2,1),
                        Tag = "car_gear_text"
                    };
                    TrackCanvas.Children.Add(_carGearText);
                    Panel.SetZIndex(_carGearText, 101);
                }
                _carGearText.Text = (string)_gearConverter.Convert(telemetryData.Car.Gear, typeof(string), null, CultureInfo.CurrentCulture);
                _carGearText.Visibility = Visibility.Visible;
                Canvas.SetLeft(_carGearText, carCanvasPos.X + 8); // Position relative to marker
                Canvas.SetTop(_carGearText, carCanvasPos.Y + 2);

             });
        }

        // Convert world coordinates (X, Z) to canvas coordinates (X, Y)
        private Point WorldToCanvas(Vector3 worldPosition)
        {
            if (_scaleFactor == 0) return new Point(ActualWidth / 2, ActualHeight / 2);

            var x = (worldPosition.X - _trackCenter.X) * _scaleFactor + (ActualWidth > 0 ? ActualWidth / 2 : TrackCanvas.Width / 2);
            // Assuming Z is vertical on the map, and positive Z is down in world?
            // If positive Z is up in world, invert the Z calculation for canvas Y
            var y = (worldPosition.Z - _trackCenter.Y) * _scaleFactor + (ActualHeight > 0 ? ActualHeight / 2 : TrackCanvas.Height / 2);
            // If Z needs inversion: var y = (-worldPosition.Z - _trackCenter.Y) * _scaleFactor + ...

            return new Point(x, y);
        }

        // --- Event Handlers for Zoom/Pan (Placeholder - Requires ScrollViewer in XAML) ---
        // private void ZoomInButton_Click(object sender, RoutedEventArgs e) { ... }
        // private void ZoomOutButton_Click(object sender, RoutedEventArgs e) { ... }
        // private void FitToScreenButton_Click(object sender, RoutedEventArgs e) { ... }
        // private void TrackCanvas_MouseMove(object sender, MouseEventArgs e) { ... }
        // private void TrackCanvas_MouseLeave(object sender, MouseEventArgs e) { ... }
    }
}

