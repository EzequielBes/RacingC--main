using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Application.UseCases;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Infrastructure.Services;

namespace TelemetryAnalyzer.Presentation.WPF
{
    public partial class MainWindow : Window
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly RealTimeTelemetryUseCase _realTimeTelemetry;
        private readonly ImportTelemetryUseCase _importTelemetry;
        private readonly LapAnalysisService _lapAnalysisService;
        private readonly LapComparisonService _lapComparisonService;
        private readonly SimulatorDetectionService _simulatorDetection;

        private bool _isConnected = false;
        private System.Windows.Threading.DispatcherTimer _statusUpdateTimer;
        private System.Windows.Threading.DispatcherTimer _performanceTimer;

        public MainWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            
            // Resolver dependências
            _realTimeTelemetry = serviceProvider.GetRequiredService<RealTimeTelemetryUseCase>();
            _importTelemetry = serviceProvider.GetRequiredService<ImportTelemetryUseCase>();
            _lapAnalysisService = serviceProvider.GetRequiredService<LapAnalysisService>();
            _lapComparisonService = serviceProvider.GetRequiredService<LapComparisonService>();
            _simulatorDetection = serviceProvider.GetRequiredService<SimulatorDetectionService>();

            InitializeComponent();
            InitializeTimers();
            InitializeEventHandlers();
            
            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
        }

        private void InitializeTimers()
        {
            // Timer para atualizações de status
            _statusUpdateTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _statusUpdateTimer.Tick += StatusUpdateTimer_Tick;
            _statusUpdateTimer.Start();

            // Timer para monitoramento de performance
            _performanceTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _performanceTimer.Tick += PerformanceTimer_Tick;
            _performanceTimer.Start();
        }

        private void InitializeEventHandlers()
        {
            // Real-time telemetry events
            _realTimeTelemetry.TelemetryDataReceived += OnTelemetryDataReceived;
            _realTimeTelemetry.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Initializing...";
            
            try
            {
                // Detectar simuladores rodando
                await DetectSimulatorsAsync();
                
                StatusText.Text = "Ready";
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error: {ex.Message}";
                MessageBox.Show($"Initialization error: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task DetectSimulatorsAsync()
        {
            var simulators = await _simulatorDetection.DetectRunningSimulatorsAsync();
            
            Dispatcher.Invoke(() =>
            {
                SimulatorComboBox.Items.Clear();
                SimulatorComboBox.Items.Add("Auto-Detect");
                
                foreach (var sim in simulators)
                {
                    SimulatorComboBox.Items.Add(sim);
                }
                
                if (simulators.Count > 0)
                {
                    SimulatorComboBox.SelectedIndex = 1; // Select first detected
                }
                else
                {
                    SimulatorComboBox.SelectedIndex = 0; // Auto-detect
                }
            });
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isConnected)
            {
                await DisconnectAsync();
            }
            else
            {
                await ConnectAsync();
            }
        }

        private async Task ConnectAsync()
        {
            try
            {
                ConnectButton.Content = "Connecting...";
                ConnectButton.IsEnabled = false;
                
                var selectedSim = SimulatorComboBox.SelectedItem?.ToString();
                
                if (selectedSim == "Auto-Detect" || string.IsNullOrEmpty(selectedSim))
                {
                    // Auto-detect logic
                    var simulators = await _simulatorDetection.DetectRunningSimulatorsAsync();
                    selectedSim = simulators.FirstOrDefault();
                }

                if (string.IsNullOrEmpty(selectedSim))
                {
                    throw new InvalidOperationException("No simulator detected. Please start a supported simulator first.");
                }

                var connected = await _realTimeTelemetry.StartMonitoringAsync(selectedSim);
                
                if (connected)
                {
                    _isConnected = true;
                    UpdateConnectionStatus(true, $"Connected to {selectedSim}");
                    ConnectButton.Content = "🔌 Disconnect";
                }
                else
                {
                    throw new InvalidOperationException($"Failed to connect to {selectedSim}");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Connection failed: {ex.Message}", "Connection Error", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                UpdateConnectionStatus(false, "Connection failed");
            }
            finally
            {
                ConnectButton.IsEnabled = true;
            }
        }

        private async Task DisconnectAsync()
        {
            try
            {
                await _realTimeTelemetry.StopMonitoringAsync();
                _isConnected = false;
                UpdateConnectionStatus(false, "Disconnected");
                ConnectButton.Content = "🔌 Connect";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Disconnect error: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateConnectionStatus(bool connected, string message)
        {
            Dispatcher.Invoke(() =>
            {
                ConnectionStatusIndicator.Fill = connected ? 
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 255, 0)) :
                    new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                
                ConnectionStatusText.Text = message;
            });
        }

        private void OnTelemetryDataReceived(TelemetryData data)
        {
            Dispatcher.InvokeAsync(() =>
            {
                // Update real-time displays
                RealTimeTrackMap.UpdatePosition(data.Car.Position);
                RealTimeTelemetryChart.AddDataPoint(data);
                LiveDataPanel.UpdateData(data);
                
                // Update current session info
                CurrentSessionText.Text = $"{data.SimulatorName} - {data.Track?.Name ?? "Unknown Track"}";
            });
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            UpdateConnectionStatus(connected, message);
            _isConnected = connected;
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Import Telemetry Data",
                Filter = "Telemetry Files|*.ldx;*.ld;*.csv;*.json|All Files|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                var progressWindow = new ProgressWindow("Importing files...");
                progressWindow.Show();

                try
                {
                    foreach (var file in dialog.FileNames)
                    {
                        progressWindow.UpdateProgress($"Importing {Path.GetFileName(file)}...");
                        var result = await _importTelemetry.ImportFileAsync(file);
                        
                        if (!result.IsSuccess)
                        {
                            MessageBox.Show($"Failed to import {Path.GetFileName(file)}: {result.ErrorMessage}", 
                                          "Import Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }

                    // Refresh session list
                    await RefreshSessionListAsync();
                    
                    StatusText.Text = $"Imported {dialog.FileNames.Length} file(s)";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import error: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    progressWindow.Close();
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            // Implement export functionality
            MessageBox.Show("Export functionality coming soon!", "Info", 
                          MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private async Task RefreshSessionListAsync()
        {
            try
            {
                // Refresh session selector with new data
                await SessionLapSelector.RefreshSessionsAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error refreshing sessions: {ex.Message}";
            }
        }

        private void StatusUpdateTimer_Tick(object sender, EventArgs e)
        {
            // Update status information periodically
            if (_isConnected)
            {
                StatusText.Text = "Receiving telemetry data...";
            }
        }

        private void PerformanceTimer_Tick(object sender, EventArgs e)
        {
            // Update performance metrics
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024 * 1024);
            MemoryUsageText.Text = $"Memory: {memoryMB} MB";
            
            // Update FPS (placeholder - would need actual FPS calculation)
            if (_isConnected)
            {
                FpsText.Text = "FPS: 60";
            }
            else
            {
                FpsText.Text = "FPS: 0";
            }
        }

        // Window Control Events
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(sender, e);
            }
            else
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? 
                         WindowState.Normal : WindowState.Maximized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            try
            {
                _statusUpdateTimer?.Stop();
                _performanceTimer?.Stop();
                
                if (_isConnected)
                {
                    await DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't prevent closing
                System.Diagnostics.Debug.WriteLine($"Error during shutdown: {ex.Message}");
            }
        }
    }
}