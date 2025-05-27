using System.Windows;

namespace TelemetryAnalyzer.Presentation.WPF
{
    public partial class ProgressWindow : Window
    {
        public ProgressWindow(string title)
        {
            InitializeComponent();
            Title = title;
        }

        public void UpdateProgress(string message)
        {
            Dispatcher.Invoke(() =>
            {
                // Update progress display
                // Implementation depends on XAML structure
            });
        }
    }
}

// Presentation/WPF/Windows/SettingsWindow.xaml.cs
using System.Windows;

namespace TelemetryAnalyzer.Presentation.WPF
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
        }
    }
}