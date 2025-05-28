using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF
{
    public partial class LapComparisonDialog : Window
    {
        private readonly List<TelemetrySession> _sessions;
        public List<TelemetryAnalyzer.Core.Models.LapAnalysis.LapData> SelectedLaps { get; private set; } = new();

        public LapComparisonDialog(List<TelemetrySession> sessions)
        {
            InitializeComponent();
            _sessions = sessions;
            PopulateLapsList();
        }

        private void PopulateLapsList()
        {
            // Implementation for populating available laps for comparison
            // This would include a list view with checkboxes for multi-selection
        }
    }
}