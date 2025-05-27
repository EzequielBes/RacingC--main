using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class TelemetryChart : UserControl
    {
        private readonly PlotModel _plotModel;
        private readonly List<TelemetryData> _dataPoints = new();
        private readonly Dictionary<string, LineSeries> _series = new();
        private int _maxDataPoints = 1000;
        private DateTime _startTime;

        public TelemetryChart()
        {
            InitializeComponent();
            InitializePlot();
        }

        private void InitializePlot()
        {
            _plotModel = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColor.FromRgb(64, 64, 64),
                TextColor = OxyColors.White,
                TitleColor = OxyColors.White
            };

            // Configure axes
            var timeAxis = new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Time (seconds)",
                TitleColor = OxyColors.White,
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64),
                TicklineColor = OxyColor.FromRgb(64, 64, 64),
                MajorGridlineColor = OxyColor.FromRgb(32, 32, 32),
                MajorGridlineStyle = LineStyle.Solid
            };

            var valueAxis = new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Value",
                TitleColor = OxyColors.White,
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64),
                TicklineColor = OxyColor.FromRgb(64, 64, 64),
                MajorGridlineColor = OxyColor.FromRgb(32, 32, 32),
                MajorGridlineStyle = LineStyle.Solid
            };

            _plotModel.Axes.Add(timeAxis);
            _plotModel.Axes.Add(valueAxis);

            // Initialize series
            InitializeSeries();

            TelemetryPlot.Model = _plotModel;
            _startTime = DateTime.Now;
        }

        private void InitializeSeries()
        {
            // Speed series
            var speedSeries = new LineSeries
            {
                Title = "Speed (km/h)",
                Color = OxyColors.Cyan,
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            _series["Speed"] = speedSeries;
            _plotModel.Series.Add(speedSeries);

            // RPM series (scaled down)
            var rpmSeries = new LineSeries
            {
                Title = "RPM (/100)",
                Color = OxyColors.Red,
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            _series["RPM"] = rpmSeries;
            _plotModel.Series.Add(rpmSeries);

            // Throttle series (0-100%)
            var throttleSeries = new LineSeries
            {
                Title = "Throttle (%)",
                Color = OxyColors.Green,
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            _series["Throttle"] = throttleSeries;
            _plotModel.Series.Add(throttleSeries);

            // Brake series (0-100%)
            var brakeSeries = new LineSeries
            {
                Title = "Brake (%)",
                Color = OxyColors.Orange,
                StrokeThickness = 2,
                MarkerType = MarkerType.None
            };
            _series["Brake"] = brakeSeries;
            _plotModel.Series.Add(brakeSeries);

            // Gear series
            var gearSeries = new LineSeries
            {
                Title = "Gear",
                Color = OxyColors.Yellow,
                StrokeThickness = 2,
                MarkerType = MarkerType.Circle,
                MarkerSize = 3
            };
            _series["Gear"] = gearSeries;
            _plotModel.Series.Add(gearSeries);

            UpdateSeriesVisibility();
        }

        public void AddDataPoint(TelemetryData data)
        {
            if (data?.Car == null) return;

            _dataPoints.Add(data);

            // Limit data points to prevent memory issues
            if (_dataPoints.Count > _maxDataPoints)
            {
                _dataPoints.RemoveAt(0);
                // Update start time accordingly
                if (_dataPoints.Any())
                {
                    _startTime = _dataPoints.First().Timestamp;
                }
            }

            Dispatcher.InvokeAsync(() =>
            {
                UpdateChart();
            });
        }

        public void LoadHistoricalData(List<TelemetryData> data)
        {
            _dataPoints.Clear();
            _dataPoints.AddRange(data);

            if (_dataPoints.Any())
            {
                _startTime = _dataPoints.First().Timestamp;
            }

            Dispatcher.InvokeAsync(() =>
            {
                UpdateChart();
            });
        }

        private void UpdateChart()
        {
            if (!_dataPoints.Any()) return;

            // Clear existing points
            foreach (var series in _series.Values)
            {
                series.Points.Clear();
            }

            // Add new points
            foreach (var data in _dataPoints)
            {
                var timeSeconds = (data.Timestamp - _startTime).TotalSeconds;

                _series["Speed"].Points.Add(new DataPoint(timeSeconds, data.Car.Speed));
                _series["RPM"].Points.Add(new DataPoint(timeSeconds, data.Car.RPM / 100.0)); // Scale down RPM
                _series["Throttle"].Points.Add(new DataPoint(timeSeconds, data.Car.Throttle * 100));
                _series["Brake"].Points.Add(new DataPoint(timeSeconds, data.Car.Brake * 100));
                _series["Gear"].Points.Add(new DataPoint(timeSeconds, data.Car.Gear * 10)); // Scale up gear for visibility
            }

            _plotModel.InvalidatePlot(true);
        }

        private void UpdateSeriesVisibility()
        {
            _series["Speed"].IsVisible = SpeedCheckBox.IsChecked == true;
            _series["RPM"].IsVisible = RpmCheckBox.IsChecked == true;
            _series["Throttle"].IsVisible = ThrottleCheckBox.IsChecked == true;
            _series["Brake"].IsVisible = BrakeCheckBox.IsChecked == true;
            _series["Gear"].IsVisible = GearCheckBox.IsChecked == true;

            _plotModel.InvalidatePlot(true);
        }

        private void ChartOption_Changed(object sender, RoutedEventArgs e)
        {
            UpdateSeriesVisibility();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            _dataPoints.Clear();
            
            foreach (var series in _series.Values)
            {
                series.Points.Clear();
            }

            _plotModel.InvalidatePlot(true);
            _startTime = DateTime.Now;
        }
    }
}
