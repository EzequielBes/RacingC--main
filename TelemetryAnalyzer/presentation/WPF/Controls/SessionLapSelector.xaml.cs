using System.Windows.Controls;
using System.Windows;
using TelemetryAnalyzer.Presentation.WPF.ViewModels; // Need access to MainViewModel and SessionListItemViewModel
using TelemetryAnalyzer.Core.Models; // For TelemetrySession and LapData if needed directly
using System.Linq;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class SessionLapSelector : UserControl
    {
        public SessionLapSelector()
        {
            InitializeComponent();
            // DataContext should be set by the parent window (MainWindow)
            // We might need to listen for DataContext changes if it's not set immediately.
            DataContextChanged += SessionLapSelector_DataContextChanged;
        }

        private MainViewModel ViewModel => DataContext as MainViewModel;

        private void SessionLapSelector_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            // When the DataContext (ViewModel) is set, populate the TreeView
            // This assumes the ViewModel loads sessions initially.
            // A more robust approach might involve binding ItemsSource directly in XAML
            // or using an event/messaging system.
            PopulateTreeView();
        }

        // Method to populate the TreeView based on ViewModel data
        // This is a basic implementation. Binding ItemsSource in XAML is generally preferred for MVVM.
        private async void PopulateTreeView()
        {
            if (ViewModel == null || ViewModel.Sessions == null)
            {
                SessionTreeView.ItemsSource = null;
                return;
            }

            // If sessions aren't loaded yet, wait or trigger load (depends on ViewModel logic)
            // For simplicity, assume sessions are loaded or being loaded by ViewModel.

            // Create TreeViewItems manually (less MVVM-friendly) or bind ItemsSource (better)
            // Let's assume XAML binding is preferred. We'll adjust XAML later if needed.
            // For now, just ensure the ViewModel's Sessions collection is the source.
            SessionTreeView.ItemsSource = ViewModel.Sessions; 
            // We need to define HierarchicalDataTemplates in XAML for this binding to work correctly
            // to show sessions and their laps.
        }

        private void SessionTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            object selectedItem = SessionTreeView.SelectedItem;
            bool lapSelected = selectedItem is LapData; // Assuming LapData is used as TreeViewItem content
            bool sessionSelected = selectedItem is SessionListItemViewModel;

            AnalyzeLapButton.IsEnabled = lapSelected;
            CompareLapsButton.IsEnabled = lapSelected; // Or maybe need multiple laps selected?
            ExportLapButton.IsEnabled = lapSelected;

            // Update the ViewModel's SelectedSession property
            if (ViewModel != null)
            {
                if (sessionSelected)
                {
                    ViewModel.SelectedSession = selectedItem as SessionListItemViewModel;
                    // Load laps for the selected session if not already loaded?
                }
                else if (lapSelected)
                {
                    // Find the parent session? This depends on how the TreeView is structured.
                    // If using HierarchicalDataTemplate, finding the parent might be tricky here.
                    // It's often better to handle lap selection within the ViewModel or via commands.
                }
                else
                {
                    ViewModel.SelectedSession = null;
                }
            }
        }

        private void AnalyzeLapButton_Click(object sender, RoutedEventArgs e)
        {
            // Trigger analysis command in ViewModel, passing the selected lap
            // This requires knowing which lap is selected.
            object selectedLap = SessionTreeView.SelectedItem; // Assuming it's LapData
            // ViewModel?.AnalyzeLapCommand.Execute(selectedLap);
            MessageBox.Show("Analyze Lap clicked (implementation pending in ViewModel)");
        }

        private void CompareLapsButton_Click(object sender, RoutedEventArgs e)
        {
            // Trigger comparison logic - might need a separate view/dialog
            MessageBox.Show("Compare Laps clicked (implementation pending)");
        }

        private void ExportLapButton_Click(object sender, RoutedEventArgs e)
        {
            // Trigger export logic
            MessageBox.Show("Export Lap clicked (implementation pending)");
        }
    }
}

