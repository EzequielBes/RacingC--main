using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class ComparisonSetupControl : UserControl
    {
        private List<LapData> _availableLaps = new();
        private LapData _referenceLap;
        private LapData _comparisonLap;

        public event EventHandler<LapComparisonEventArgs> ComparisonRequested;

        public ComparisonSetupControl()
        {
            InitializeComponent();
        }

        public void LoadAvailableLaps(List<LapData> laps)
        {
            _availableLaps = laps;
            PopulateComboBoxes();
        }

        private void PopulateComboBoxes()
        {
            ReferenceLapCombo.Items.Clear();
            ComparisonLapCombo.Items.Clear();

            foreach (var lap in _availableLaps.OrderBy(l => l.LapTime))
            {
                var displayText = $"{lap.DriverName} - Lap {lap.LapNumber} ({FormatTime(lap.LapTime)})";
                
                ReferenceLapCombo.Items.Add(new ComboBoxItem 
                { 
                    Content = displayText, 
                    Tag = lap 
                });
                
                ComparisonLapCombo.Items.Add(new ComboBoxItem 
                { 
                    Content = displayText, 
                    Tag = lap 
                });
            }

            // Auto-select fastest lap as reference if available
            if (ReferenceLapCombo.Items.Count > 0)
            {
                ReferenceLapCombo.SelectedIndex = 0;
            }
        }

        private void ReferenceLapCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ReferenceLapCombo.SelectedItem is ComboBoxItem item && item.Tag is LapData lap)
            {
                _referenceLap = lap;
                ReferenceTimeText.Text = $"Time: {FormatTime(lap.LapTime)}";
                ReferenceDriverText.Text = $"Driver: {lap.DriverName}";
                
                UpdateCompareButtonState();
            }
        }

        private void ComparisonLapCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComparisonLapCombo.SelectedItem is ComboBoxItem item && item.Tag is LapData lap)
            {
                _comparisonLap = lap;
                ComparisonTimeText.Text = $"Time: {FormatTime(lap.LapTime)}";
                ComparisonDriverText.Text = $"Driver: {lap.DriverName}";
                
                UpdateCompareButtonState();
            }
        }

        private void UpdateCompareButtonState()
        {
            CompareButton.IsEnabled = _referenceLap != null && _comparisonLap != null && 
                                    _referenceLap.Id != _comparisonLap.Id;
        }

        private void CompareButton_Click(object sender, RoutedEventArgs e)
        {
            if (_referenceLap != null && _comparisonLap != null)
            {
                ComparisonRequested?.Invoke(this, new LapComparisonEventArgs
                {
                    ReferenceLap = _referenceLap,
                    ComparisonLap = _comparisonLap
                });
            }
        }

        private string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero) return "--:--:---";
            return $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }
    }

    public class LapComparisonEventArgs : EventArgs
    {
        public LapData ReferenceLap { get; set; }
        public LapData ComparisonLap { get; set; }
    }
}