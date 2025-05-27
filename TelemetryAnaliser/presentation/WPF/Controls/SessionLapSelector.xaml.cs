using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class SessionLapSelector : UserControl
    {
        private readonly ITelemetryRepository _repository;
        private readonly LapAnalysisService _lapAnalysisService;
        private List<TelemetrySession> _sessions = new();
        private readonly List<LapData> _selectedLaps = new();

        public event EventHandler<LapData> LapSelected;
        public event EventHandler<List<LapData>> LapsSelectedForComparison;

        public SessionLapSelector()
        {
            InitializeComponent();
        }

        public SessionLapSelector(ITelemetryRepository repository, LapAnalysisService lapAnalysisService)
        {
            InitializeComponent();
            _repository = repository;
            _lapAnalysisService = lapAnalysisService;
        }

        public async Task RefreshSessionsAsync()
        {
            try
            {
                _sessions = await _repository.GetSessionsAsync();
                await PopulateTreeViewAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading sessions: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task PopulateTreeViewAsync()
        {
            SessionTreeView.Items.Clear();

            foreach (var session in _sessions.OrderByDescending(s => s.ImportedAt))
            {
                var sessionItem = new TreeViewItem
                {
                    Header = $"📁 {session.Name} ({session.ImportedAt:yyyy-MM-dd HH:mm})",
                    Tag = session
                };

                // Add laps to session
                if (session.Data?.Laps != null)
                {
                    foreach (var lap in session.Data.Laps.OrderBy(l => l.LapNumber))
                    {
                        var lapHeader = $"🏁 Lap {lap.LapNumber} - {FormatLapTime(lap.LapTime)}";
                        if (!lap.IsValid)
                            lapHeader += " ❌";
                        else if (lap.IsPersonalBest)
                            lapHeader += " 🏆";

                        var lapItem = new TreeViewItem
                        {
                            Header = lapHeader,
                            Tag = lap
                        };

                        sessionItem.Items.Add(lapItem);
                    }
                }

                SessionTreeView.Items.Add(sessionItem);
            }
        }

        private void SessionTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selectedItem = e.NewValue as TreeViewItem;
            if (selectedItem?.Tag is LapData lapData)
            {
                // Single lap selected
                _selectedLaps.Clear();
                _selectedLaps.Add(lapData);
                
                AnalyzeLapButton.IsEnabled = true;
                CompareLapsButton.IsEnabled = false;
                ExportLapButton.IsEnabled = true;
                
                LapSelected?.Invoke(this, lapData);
            }
            else
            {
                // Session or nothing selected
                AnalyzeLapButton.IsEnabled = false;
                CompareLapsButton.IsEnabled = false;
                ExportLapButton.IsEnabled = false;
            }
        }

        private async void AnalyzeLapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedLaps.Any()) return;

            try
            {
                var lapData = _selectedLaps.First();
                
                // Create detailed lap analysis
                var analysis = await _lapAnalysisService.CreateLapDataAsync(lapData.TelemetryPoints);
                
                // Trigger analysis display
                LapSelected?.Invoke(this, analysis);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing lap: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CompareLapsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open lap comparison dialog
            var comparisonDialog = new LapComparisonDialog(_sessions);
            comparisonDialog.Owner = Window.GetWindow(this);
            
            if (comparisonDialog.ShowDialog() == true)
            {
                var selectedLaps = comparisonDialog.SelectedLaps;
                if (selectedLaps.Count >= 2)
                {
                    LapsSelectedForComparison?.Invoke(this, selectedLaps);
                }
            }
        }

        private void ExportLapButton_Click(object sender, RoutedEventArgs e)
        {
            if (!_selectedLaps.Any()) return;

            var saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export Lap Data",
                Filter = "JSON Files|*.json|CSV Files|*.csv|All Files|*.*",
                DefaultExt = "json"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    // Implement export functionality
                    ExportLapData(_selectedLaps.First(), saveDialog.FileName);
                    MessageBox.Show("Lap exported successfully!", "Export Complete", 
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting lap: {ex.Message}", "Export Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ExportLapData(LapData lapData, string filePath)
        {
            // Implement export logic based on file extension
            var extension = System.IO.Path.GetExtension(filePath).ToLower();
            
            switch (extension)
            {
                case ".json":
                    ExportAsJson(lapData, filePath);
                    break;
                case ".csv":
                    ExportAsCsv(lapData, filePath);
                    break;
                default:
                    throw new NotSupportedException($"Export format {extension} not supported");
            }
        }

        private void ExportAsJson(LapData lapData, string filePath)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(lapData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(filePath, json);
        }

        private void ExportAsCsv(LapData lapData, string filePath)
        {
            using var writer = new System.IO.StreamWriter(filePath);
            
            // Write header
            writer.WriteLine("Time,Speed,RPM,Gear,Throttle,Brake,PosX,PosY,PosZ");
            
            // Write data points
            foreach (var point in lapData.TelemetryPoints)
            {
                writer.WriteLine($"{point.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                               $"{point.Car.Speed}," +
                               $"{point.Car.RPM}," +
                               $"{point.Car.Gear}," +
                               $"{point.Car.Throttle}," +
                               $"{point.Car.Brake}," +
                               $"{point.Car.Position.X}," +
                               $"{point.Car.Position.Y}," +
                               $"{point.Car.Position.Z}");
            }
        }

        private string FormatLapTime(TimeSpan lapTime)
        {
            if (lapTime == TimeSpan.Zero) return "00:00.000";
            return $"{lapTime.Minutes:D2}:{lapTime.Seconds:D2}.{lapTime.Milliseconds:D3}";
        }
    }
}