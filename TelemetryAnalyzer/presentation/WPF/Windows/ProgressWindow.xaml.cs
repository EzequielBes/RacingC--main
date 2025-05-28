using System.Windows;
using System.Threading.Tasks;
using System;

namespace TelemetryAnalyzer.Presentation.WPF.Windows
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
