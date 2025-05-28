using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TelemetryAnalyzer.Application.Services; // Assuming a service exists to get sessions
using TelemetryAnalyzer.Application.UseCases;
using TelemetryAnalyzer.Core.Interfaces; // Need ITelemetryRepository
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;
using System.Collections.Generic; // For List
using TelemetryAnalyzer.Presentation.WPF.Models; // For LapViewModel
using Microsoft.Extensions.Logging;
using System.Windows.Media;

namespace TelemetryAnalyzer.Presentation.WPF.ViewModels
{
    // Represents a session in the UI list, now includes Laps collection
    public class SessionViewModel : INotifyPropertyChanged
    {
        private readonly TelemetrySession _session;
        private readonly ITelemetryRepository _repository; // To load laps on demand
        private ObservableCollection<LapViewModel> _laps;
        private bool _areLapsLoaded = false;

        public SessionViewModel(TelemetrySession session, ITelemetryRepository repository)
        {
            _session = session;
            _repository = repository;
            Laps = new ObservableCollection<LapViewModel>();
        }

        public Guid Id => _session.Id;
        public string Name => _session.Name ?? "Unnamed Session";
        public string Game => _session.GameName ?? "N/A";
        public string Track => _session.TrackName ?? "N/A";
        public string Car => _session.CarName ?? "N/A";
        public DateTime Timestamp => _session.Timestamp;
        public int LapCount => _session.Data?.Laps?.Count ?? 0; // Initial count if available
        public TimeSpan BestLapTime => _session.Data?.Laps?.Where(l => l.IsValid).DefaultIfEmpty().Min(l => l?.LapTime ?? TimeSpan.MaxValue) ?? TimeSpan.Zero; // Initial best lap if available

        public ObservableCollection<LapViewModel> Laps
        {
            get => _laps;
            set => SetProperty(ref _laps, value);
        }

        public async Task LoadLapsAsync()
        {
            if (_areLapsLoaded) return;

            try
            {
                // Load the full session data which includes laps
                var fullSession = await _repository.GetSessionAsync(Id);
                if (fullSession?.Data?.Laps != null)
                {
                    Laps.Clear();
                    foreach (var lapData in fullSession.Data.Laps.OrderBy(l => l.LapNumber))
                    {
                        Laps.Add(new LapViewModel(lapData));
                    }
                    _areLapsLoaded = true;
                    OnPropertyChanged(nameof(LapCount)); // Update count after loading
                    OnPropertyChanged(nameof(BestLapTime)); // Update best lap after loading
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading laps for session {Name}: {ex.Message}");
                // Handle error appropriately
            }
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingStore, value))
                return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RealTimeTelemetryUseCase _realTimeUseCase;
        private readonly ImportTelemetryUseCase _importUseCase;
        private readonly AnalyzeLapUseCase _analyzeLapUseCase;
        private readonly ITelemetryRepository _telemetryRepository; // Inject repository
        private readonly TrackMapService _trackMapService; // Service to get track map data

        private TelemetryData _currentTelemetry;
        private LapAnalysisResult _currentLapAnalysis;
        private string _connectionStatus = "Disconnected";
        private bool _isConnected;
        private ObservableCollection<SessionViewModel> _sessions;
        private object _selectedTreeViewItem; // Can be SessionViewModel or LapViewModel
        private string _filterText;
        private TrackMap _currentTrackMap;
        private ObservableCollection<TrackingLine> _currentTrackingLines;
        private List<LapViewModel> _lapsToCompare = new List<LapViewModel>();

        public MainViewModel(
            RealTimeTelemetryUseCase realTimeUseCase,
            ImportTelemetryUseCase importUseCase,
            AnalyzeLapUseCase analyzeLapUseCase,
            ITelemetryRepository telemetryRepository,
            TrackMapService trackMapService) // Add repository and track map service
        {
            _realTimeUseCase = realTimeUseCase;
            _importUseCase = importUseCase;
            _analyzeLapUseCase = analyzeLapUseCase;
            _telemetryRepository = telemetryRepository; // Store repository
            _trackMapService = trackMapService;

            Sessions = new ObservableCollection<SessionViewModel>();
            CurrentTrackingLines = new ObservableCollection<TrackingLine>();

            InitializeCommands();
            InitializeEventHandlers();

            // Load sessions on startup
            _ = LoadSessionsAsync();
        }

        // Properties
        public ObservableCollection<SessionViewModel> Sessions
        {
            get => _sessions;
            set => SetProperty(ref _sessions, value);
        }

        // Selected item from the TreeView
        public object SelectedTreeViewItem
        {
            get => _selectedTreeViewItem;
            set
            {
                if (SetProperty(ref _selectedTreeViewItem, value))
                {
                    // Handle selection change: Load laps, trigger analysis, etc.
                    _ = HandleTreeViewSelectionAsync(value);
                }
            }
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                {
                    ApplySessionFilter();
                }
            }
        }

        // Current real-time telemetry data for LiveDataPanel and TrackMapControl
        public TelemetryData CurrentTelemetry
        {
            get => _currentTelemetry;
            set => SetProperty(ref _currentTelemetry, value);
        }

        // Result of the lap analysis
        public LapAnalysisResult CurrentLapAnalysis
        {
            get => _currentLapAnalysis;
            set => SetProperty(ref _currentLapAnalysis, value);
        }

        // Connection status properties
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set => SetProperty(ref _connectionStatus, value);
        }
        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        // Track map data for the TrackMapControl
        public TrackMap CurrentTrackMap
        {
            get => _currentTrackMap;
            set => SetProperty(ref _currentTrackMap, value);
        }

        // Tracking lines (laps) to display on the TrackMapControl
        public ObservableCollection<TrackingLine> CurrentTrackingLines
        {
            get => _currentTrackingLines;
            set => SetProperty(ref _currentTrackingLines, value);
        }

        // Commands
        public ICommand StartMonitoringCommand { get; private set; }
        public ICommand StopMonitoringCommand { get; private set; }
        public ICommand ImportFileCommand { get; private set; }
        public ICommand RefreshSessionsCommand { get; private set; }
        public ICommand DeleteSessionCommand { get; private set; }
        public ICommand AnalyzeSelectedLapCommand { get; private set; }
        public ICommand CompareSelectedLapsCommand { get; private set; }
        public ICommand ToggleLapForComparisonCommand { get; private set; }

        private void InitializeCommands()
        {
            StartMonitoringCommand = new RelayCommand(async () => await StartMonitoringAsync(), () => !IsConnected);
            StopMonitoringCommand = new RelayCommand(async () => await StopMonitoringAsync(), () => IsConnected);
            ImportFileCommand = new RelayCommand(async () => await ImportFileAsync());
            RefreshSessionsCommand = new RelayCommand(async () => await LoadSessionsAsync());
            DeleteSessionCommand = new RelayCommand<SessionViewModel>(async (session) => await DeleteSessionAsync(session), (session) => session != null);
            AnalyzeSelectedLapCommand = new RelayCommand<LapViewModel>(async (lap) => await AnalyzeLapAsync(lap), (lap) => lap != null);
            CompareSelectedLapsCommand = new RelayCommand(async () => await CompareLapsAsync(), () => _lapsToCompare.Count > 1);
            ToggleLapForComparisonCommand = new RelayCommand<LapViewModel>(ToggleLapForComparison, (lap) => lap != null);
        }

        private void InitializeEventHandlers()
        {
            _realTimeUseCase.TelemetryDataReceived += OnRealTimeTelemetryReceived;
            _realTimeUseCase.ConnectionStatusChanged += OnConnectionStatusChanged;
            // Handle session import completion if ImportUseCase provides an event
            // _importUseCase.ImportCompleted += async (s, e) => await LoadSessionsAsync();
        }

        private async Task LoadSessionsAsync()
        {
            try
            {
                var sessionsData = await _telemetryRepository.GetSessionsAsync();
                var sessionViewModels = sessionsData.Select(s => new SessionViewModel(s, _telemetryRepository)).ToList();

                // Update the collection on the UI thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    Sessions.Clear();
                    foreach (var vm in sessionViewModels)
                    {
                        Sessions.Add(vm);
                    }
                    ApplySessionFilter();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading sessions");
                // Show error message to user
            }
        }

        private void ApplySessionFilter()
        {
            // Filtering logic - better handled by CollectionViewSource in XAML
            // For simplicity, assume the View binds to Sessions and filters itself, or:
            // var view = CollectionViewSource.GetDefaultView(Sessions);
            // view.Filter = item => { ... filter logic ... };
            // view.Refresh();
            OnPropertyChanged(nameof(Sessions)); // Notify potential view update
        }

        private async Task HandleTreeViewSelectionAsync(object selectedItem)
        {
            CurrentTrackingLines.Clear(); // Clear previous lap lines
            CurrentLapAnalysis = null; // Clear previous analysis
            // Clear comparison selection when changing main selection?
            // ClearComparisonSelection();

            if (selectedItem is SessionViewModel sessionVM)
            {
                // Load laps if not already loaded
                await sessionVM.LoadLapsAsync();
                // Load track map for the session's track
                CurrentTrackMap = await _trackMapService.GetTrackMapAsync(sessionVM.Track);
            }
            else if (selectedItem is LapViewModel lapVM)
            {
                // Lap selected, load its data for analysis and display
                // Find parent session to get track info
                var parentSession = Sessions.FirstOrDefault(s => s.Laps.Contains(lapVM));
                if (parentSession != null)
                {
                    CurrentTrackMap = await _trackMapService.GetTrackMapAsync(parentSession.Track);
                    // Analyze the selected lap
                    await AnalyzeLapAsync(lapVM);
                    // Display the selected lap's trace
                    DisplayLapTrace(lapVM, GetDefaultLineBrush(0)); // Use first default color
                }
            }
            else
            {
                CurrentTrackMap = null; // Clear track map if nothing valid selected
            }
        }

        private async Task AnalyzeLapAsync(LapViewModel lapVM)
        {
            if (lapVM == null) return;
            try
            {
                // Assuming AnalyzeLapUseCase needs LapData and maybe full session context
                // We might need to retrieve the full session data again if not cached
                // CurrentLapAnalysis = await _analyzeLapUseCase.ExecuteAsync(lapVM.Lap, /* session context */);
                Console.WriteLine($"Analyzing Lap {lapVM.LapNumber}...");
                // For now, just display its trace
                DisplayLapTrace(lapVM, GetDefaultLineBrush(0));
                await Task.CompletedTask; // Placeholder
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, $"Error analyzing lap {lapVM.LapNumber}");
            }
        }

        private void DisplayLapTrace(LapViewModel lapVM, Brush color)
        {
            if (lapVM?.Lap?.Data == null || !lapVM.Lap.Data.Any()) return;

            var points = lapVM.Lap.Data.Select(d => d.Car.Position).ToList();
            var trackingLine = new TrackingLine
            {
                Name = $"Lap {lapVM.LapNumber}",
                Points = points,
                Color = color.ToString(),
                Thickness = 2,
                Style = LineStyle.Solid
            };

            // Replace existing lines or add new one
            CurrentTrackingLines.Clear();
            CurrentTrackingLines.Add(trackingLine);
        }

        private void ToggleLapForComparison(LapViewModel lapVM)
        {
            if (lapVM == null) return;

            lapVM.IsSelectedForComparison = !lapVM.IsSelectedForComparison;

            if (lapVM.IsSelectedForComparison)
            {
                if (!_lapsToCompare.Contains(lapVM))
                    _lapsToCompare.Add(lapVM);
            }
            else
            {
                _lapsToCompare.Remove(lapVM);
            }
            // Update CanExecute for CompareLapsCommand
            ((RelayCommand)CompareSelectedLapsCommand).RaiseCanExecuteChanged();
        }

        private async Task CompareLapsAsync()
        {
            if (_lapsToCompare.Count < 2) return;

            CurrentTrackingLines.Clear();
            int colorIndex = 0;

            // Find common track map
            var firstLapSession = Sessions.FirstOrDefault(s => s.Laps.Contains(_lapsToCompare[0]));
            if (firstLapSession != null)
            {
                 CurrentTrackMap = await _trackMapService.GetTrackMapAsync(firstLapSession.Track);
            }
            else { return; } // Cannot compare without track context

            foreach (var lapVM in _lapsToCompare)
            {
                if (lapVM?.Lap?.Data == null || !lapVM.Lap.Data.Any()) continue;

                var points = lapVM.Lap.Data.Select(d => d.Car.Position).ToList();
                var trackingLine = new TrackingLine
                {
                    Name = $"Lap {lapVM.LapNumber}", // Add session/driver info later
                    Points = points,
                    Color = GetDefaultLineBrush(colorIndex++).ToString(),
                    Thickness = 2,
                    Style = LineStyle.Solid
                };
                CurrentTrackingLines.Add(trackingLine);
            }

            // TODO: Implement data comparison in charts (needs chart control update)
            Console.WriteLine($"Comparing {_lapsToCompare.Count} laps.");
        }

        private void ClearComparisonSelection()
        {
            foreach(var lap in _lapsToCompare)
            {
                lap.IsSelectedForComparison = false;
            }
            _lapsToCompare.Clear();
            ((RelayCommand)CompareSelectedLapsCommand).RaiseCanExecuteChanged();
        }

        private async Task DeleteSessionAsync(SessionViewModel sessionVM)
        {
            if (sessionVM == null) return;
            try
            {
                // Optional: Confirmation dialog
                await _telemetryRepository.DeleteSessionAsync(sessionVM.Id);
                App.Current.Dispatcher.Invoke(() => Sessions.Remove(sessionVM));
                if (SelectedTreeViewItem == sessionVM)
                {
                    SelectedTreeViewItem = null; // Clear selection
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting session {sessionVM.Name}");
                // Show error message
            }
        }

        private async Task StartMonitoringAsync()
        {
            // TODO: Get selected simulator from UI
            string selectedSim = "Auto-Detect"; // Or from a ComboBox
            var success = await _realTimeUseCase.StartMonitoringAsync(selectedSim);
            // UI updates are handled by OnConnectionStatusChanged
        }

        private async Task StopMonitoringAsync()
        {
            await _realTimeUseCase.StopMonitoringAsync();
            // UI updates are handled by OnConnectionStatusChanged
        }

        private async Task ImportFileAsync()
        {
            // Use Microsoft.Win32.OpenFileDialog to get file path
            var openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "MoTeC Log Files (*.ld;*.ldx)|*.ld;*.ldx|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                try
                {
                    IsBusy = true; // Indicate processing
                    await _importUseCase.ExecuteAsync(filePath);
                    await LoadSessionsAsync(); // Refresh session list
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error importing file: {filePath}");
                    // Show error message to user
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }

        private void OnRealTimeTelemetryReceived(TelemetryData data)
        {
            // Update UI on the correct thread
            App.Current.Dispatcher.Invoke(() =>
            {
                CurrentTelemetry = data;
                // Optionally add real-time data to a specific tracking line
                // UpdateRealTimeTrace(data);
            });
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                IsConnected = connected;
                ConnectionStatus = message;
                // Update command CanExecute status
                ((RelayCommand)StartMonitoringCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopMonitoringCommand).RaiseCanExecuteChanged();
            });
        }

        private Brush GetDefaultLineBrush(int index)
        {
            // Use a predefined list of distinct colors
            var brushes = new Brush[] { Brushes.Red, Brushes.Blue, Brushes.Green, Brushes.Magenta, Brushes.Cyan, Brushes.Yellow, Brushes.Orange };
            return brushes[index % brushes.Length];
        }

        // INotifyPropertyChanged implementation...
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(backingStore, value))
                return false;
            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        // Busy indicator property
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        // Logger instance (assuming injected or created)
        private ILogger _logger = new LoggerFactory().CreateLogger<MainViewModel>(); // Basic logger
    }

    // Helper command classes (RelayCommand, RelayCommand<T>) assumed to exist
}
