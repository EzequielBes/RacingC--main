using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Application.UseCases;
using TelemetryAnalyzer.Core.Models;
using TelemetryAnalyzer.Core.Models.LapAnalysis;

namespace TelemetryAnalyzer.Presentation.WPF.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly RealTimeTelemetryUseCase _realTimeUseCase;
        private readonly ImportTelemetryUseCase _importUseCase;
        private readonly AnalyzeLapUseCase _analyzeLapUseCase;

        private TelemetryData _currentTelemetry;
        private LapAnalysisResult _currentLapAnalysis;
        private string _connectionStatus = "Disconnected";
        private bool _isConnected;

        public MainViewModel(
            RealTimeTelemetryUseCase realTimeUseCase,
            ImportTelemetryUseCase importUseCase,
            AnalyzeLapUseCase analyzeLapUseCase)
        {
            _realTimeUseCase = realTimeUseCase;
            _importUseCase = importUseCase;
            _analyzeLapUseCase = analyzeLapUseCase;

            Sessions = new ObservableCollection<TelemetrySession>();
            
            InitializeCommands();
            InitializeEventHandlers();
        }

        // Properties
        public ObservableCollection<TelemetrySession> Sessions { get; set; }

        public TelemetryData CurrentTelemetry
        {
            get => _currentTelemetry;
            set => SetProperty(ref _currentTelemetry, value);
        }

        public LapAnalysisResult CurrentLapAnalysis
        {
            get => _currentLapAnalysis;
            set => SetProperty(ref _currentLapAnalysis, value);
        }

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

        // Commands
        public ICommand StartMonitoringCommand { get; private set; }
        public ICommand StopMonitoringCommand { get; private set; }
        public ICommand ImportFileCommand { get; private set; }
        public ICommand AnalyzeLapCommand { get; private set; }

        private void InitializeCommands()
        {
            StartMonitoringCommand = new RelayCommand(async () => await StartMonitoringAsync());
            StopMonitoringCommand = new RelayCommand(async () => await StopMonitoringAsync());
            ImportFileCommand = new RelayCommand(async () => await ImportFileAsync());
            AnalyzeLapCommand = new RelayCommand<object>(async (param) => await AnalyzeLapAsync(param));
        }

        private void InitializeEventHandlers()
        {
            _realTimeUseCase.TelemetryDataReceived += OnTelemetryDataReceived;
            _realTimeUseCase.ConnectionStatusChanged += OnConnectionStatusChanged;
        }

        private async Task StartMonitoringAsync()
        {
            var success = await _realTimeUseCase.StartMonitoringAsync();
            if (success)
            {
                IsConnected = true;
                ConnectionStatus = "Connected";
            }
        }

        private async Task StopMonitoringAsync()
        {
            await _realTimeUseCase.StopMonitoringAsync();
            IsConnected = false;
            ConnectionStatus = "Disconnected";
        }

        private async Task ImportFileAsync()
        {
            // Implementação do import - normalmente abriria um dialog
        }

        private async Task AnalyzeLapAsync(object parameter)
        {
            // Implementação da análise de volta
        }

        private void OnTelemetryDataReceived(TelemetryData data)
        {
            CurrentTelemetry = data;
        }

        private void OnConnectionStatusChanged(bool connected, string message)
        {
            IsConnected = connected;
            ConnectionStatus = message;
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

    // Helper command class
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();

        public void Execute(object parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T> _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute((T)parameter);

        public void Execute(object parameter) => _execute((T)parameter);
    }
}