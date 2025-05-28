using System.ComponentModel;
using System.Runtime.CompilerServices;
using TelemetryAnalyzer.Core.Models.LapAnalysis; // Assuming LapData is here
using System;

namespace TelemetryAnalyzer.Presentation.WPF.Models
{
    public class LapViewModel : INotifyPropertyChanged
    {
        public LapData Lap { get; }
        private bool _isSelectedForComparison;

        public LapViewModel(LapData lap)
        {
            Lap = lap ?? throw new ArgumentNullException(nameof(lap));
        }

        public int LapNumber => Lap.LapNumber;
        public TimeSpan LapTime => Lap.LapTime;
        public bool IsValid => Lap.IsValid;
        public bool IsPersonalBest => Lap.IsPersonalBest;

        public bool IsSelectedForComparison
        {
            get => _isSelectedForComparison;
            set => SetProperty(ref _isSelectedForComparison, value);
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
}

