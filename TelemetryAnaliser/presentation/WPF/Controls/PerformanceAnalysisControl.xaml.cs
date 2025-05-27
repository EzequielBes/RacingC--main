using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class PerformanceAnalysisControl : UserControl
    {
        private LapData _currentLap;

        public PerformanceAnalysisControl()
        {
            InitializeComponent();
            InitializePlots();
        }

        private void InitializePlots()
        {
            SpeedProfilePlot.Model = CreateSpeedProfilePlot();
            PedalUsagePlot.Model = CreatePedalUsagePlot();
            GearChangesPlot.Model = CreateGearChangesPlot();
        }

        public void LoadLapAnalysis(LapData lapData)
        {
            _currentLap = lapData;
            UpdateAnalysisDisplay();
        }

        private void UpdateAnalysisDisplay()
        {
            if (_currentLap == null) return;

            // Update summary
            LapTimeText.Text = FormatTime(_currentLap.LapTime);
            MaxSpeedText.Text = $"{_currentLap.Performance.MaxSpeed:F1} km/h";
            AvgSpeedText.Text = $"{_currentLap.Performance.AverageSpeed:F1} km/h";
            MaxGForceText.Text = $"{_currentLap.Performance.MaxGForce:F1}g";

            // Update charts
            UpdateSpeedProfilePlot();
            UpdatePedalUsagePlot();
            UpdateGearChangesPlot();

            // Update sectors
            UpdateSectorsList();

            // Update issues
            UpdateIssuesList();
        }

        private PlotModel CreateSpeedProfilePlot()
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColor.FromRgb(64, 64, 64),
                TextColor = OxyColors.White,
                Title = "Speed Profile"
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Distance (m)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64)
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Speed (km/h)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64)
            });

            return model;
        }

        private PlotModel CreatePedalUsagePlot()
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColor.FromRgb(64, 64, 64),
                TextColor = OxyColors.White,
                Title = "Pedal Usage"
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Distance (m)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64)
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Input (%)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64),
                Minimum = 0,
                Maximum = 100
            });

            return model;
        }

        private PlotModel CreateGearChangesPlot()
        {
            var model = new PlotModel
            {
                Background = OxyColors.Transparent,
                PlotAreaBorderColor = OxyColor.FromRgb(64, 64, 64),
                TextColor = OxyColors.White,
                Title = "Gear Changes"
            };

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Bottom,
                Title = "Distance (m)",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64)
            });

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Title = "Gear",
                TextColor = OxyColors.White,
                AxislineColor = OxyColor.FromRgb(64, 64, 64),
                Minimum = 0,
                Maximum = 8
            });

            return model;
        }

        private void UpdateSpeedProfilePlot()
        {
            if (_currentLap?.TelemetryPoints == null) return;

            var model = SpeedProfilePlot.Model;
            model.Series.Clear();

            var speedSeries = new LineSeries
            {
                Title = "Speed",
                Color = OxyColors.Cyan,
                StrokeThickness = 2
            };

            var distance = 0.0;
            foreach (var point in _currentLap.TelemetryPoints)
            {
                speedSeries.Points.Add(new DataPoint(distance, point.Car.Speed));
                distance += 5; // Approximate 5m intervals
            }

            model.Series.Add(speedSeries);
            model.InvalidatePlot(true);
        }

        private void UpdatePedalUsagePlot()
        {
            if (_currentLap?.TelemetryPoints == null) return;

            var model = PedalUsagePlot.Model;
            model.Series.Clear();

            var throttleSeries = new LineSeries
            {
                Title = "Throttle",
                Color = OxyColors.Green,
                StrokeThickness = 2
            };

            var brakeSeries = new LineSeries
            {
                Title = "Brake",
                Color = OxyColors.Red,
                StrokeThickness = 2
            };

            var distance = 0.0;
            foreach (var point in _currentLap.TelemetryPoints)
            {
                throttleSeries.Points.Add(new DataPoint(distance, point.Car.Throttle * 100));
                brakeSeries.Points.Add(new DataPoint(distance, point.Car.Brake * 100));
                distance += 5;
            }

            model.Series.Add(throttleSeries);
            model.Series.Add(brakeSeries);
            model.InvalidatePlot(true);
        }

        private void UpdateGearChangesPlot()
        {
            if (_currentLap?.TelemetryPoints == null) return;

            var model = GearChangesPlot.Model;
            model.Series.Clear();

            var gearSeries = new LineSeries
            {
                Title = "Gear",
                Color = OxyColors.Yellow,
                StrokeThickness = 3,
                MarkerType = MarkerType.Circle,
                MarkerSize = 4
            };

            var distance = 0.0;
            foreach (var point in _currentLap.TelemetryPoints)
            {
                gearSeries.Points.Add(new DataPoint(distance, point.Car.Gear));
                distance += 5;
            }

            model.Series.Add(gearSeries);
            model.InvalidatePlot(true);
        }

        private void UpdateSectorsList()
        {
            if (_currentLap?.Sectors == null) return;

            var sectorViewModels = _currentLap.Sectors.Select(s => new SectorViewModel
            {
                SectorName = $"Sector {s.SectorNumber}",
                SectorTime = s.SectorTime,
                EfficiencyScore = s.Analysis?.EfficiencyScore ?? 0
            }).ToList();

            SectorsList.ItemsSource = sectorViewModels;
        }

        private void UpdateIssuesList()
        {
            var allIssues = new List<PerformanceIssue>();

            // Collect issues from all sectors
            if (_currentLap?.Sectors != null)
            {
                foreach (var sector in _currentLap.Sectors)
                {
                    if (sector.Analysis?.Issues != null)
                    {
                        allIssues.AddRange(sector.Analysis.Issues);
                    }
                }
            }

            // Sort by severity (highest first)
            var sortedIssues = allIssues.OrderByDescending(i => i.Severity).ToList();

            IssuesList.ItemsSource = sortedIssues;
        }

        private string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero) return "00:00.000";
            return $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }
    }

    // View models for data binding
    public class SectorViewModel
    {
        public string SectorName { get; set; }
        public TimeSpan SectorTime { get; set; }
        public float EfficiencyScore { get; set; }
    }
}