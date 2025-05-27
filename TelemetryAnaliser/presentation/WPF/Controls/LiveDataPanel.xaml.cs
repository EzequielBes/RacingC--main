using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Presentation.WPF.Controls
{
    public partial class LiveDataPanel : UserControl
    {
        public LiveDataPanel()
        {
            InitializeComponent();
        }

        public void UpdateData(TelemetryData data)
        {
            if (data?.Car == null) return;

            Dispatcher.InvokeAsync(() =>
            {
                // Car data
                SpeedText.Text = $"{data.Car.Speed:F1} km/h";
                RpmText.Text = $"{data.Car.RPM:F0}";
                GearText.Text = data.Car.Gear == 0 ? "N" : data.Car.Gear.ToString();
                FuelText.Text = $"{data.Car.FuelLevel:F1} L";

                // Pedals
                var throttlePercent = data.Car.Throttle * 100;
                var brakePercent = data.Car.Brake * 100;
                
                ThrottlePercent.Text = $"{throttlePercent:F0}%";
                BrakePercent.Text = $"{brakePercent:F0}%";
                
                ThrottleBar.Width = Math.Max(0, throttlePercent / 100 * 120); // 120 is approximate bar width
                BrakeBar.Width = Math.Max(0, brakePercent / 100 * 120);

                // Session data
                if (data.Session != null)
                {
                    CurrentLapText.Text = data.Session.CurrentLap.ToString();
                    PositionText.Text = GetPositionString(data.Session.Position);
                    LapTimeText.Text = FormatTime(data.Session.CurrentLapTime);
                    BestTimeText.Text = FormatTime(data.Session.BestLapTime);
                }

                // Tire temperatures
                if (data.Car.Tires != null && data.Car.Tires.Length >= 4)
                {
                    FrontLeftTemp.Text = $"{data.Car.Tires[0].Temperature:F0}°C";
                    FrontRightTemp.Text = $"{data.Car.Tires[1].Temperature:F0}°C";
                    RearLeftTemp.Text = $"{data.Car.Tires[2].Temperature:F0}°C";
                    RearRightTemp.Text = $"{data.Car.Tires[3].Temperature:F0}°C";

                    // Color code temperatures
                    UpdateTireTemperatureColors(data.Car.Tires);
                }
            });
        }

        private void UpdateTireTemperatureColors(TireData[] tires)
        {
            var tempControls = new[] { FrontLeftTemp, FrontRightTemp, RearLeftTemp, RearRightTemp };

            for (int i = 0; i < Math.Min(tires.Length, tempControls.Length); i++)
            {
                var temp = tires[i].Temperature;
                var color = GetTemperatureColor(temp);
                tempControls[i].Foreground = new SolidColorBrush(color);
            }
        }

        private Color GetTemperatureColor(float temperature)
        {
            // Color coding for tire temperatures
            if (temperature < 60) return Colors.Cyan;      // Cold
            if (temperature < 80) return Colors.Green;     // Optimal
            if (temperature < 100) return Colors.Yellow;   // Warm
            if (temperature < 120) return Colors.Orange;   // Hot
            return Colors.Red;                             // Overheating
        }

        private string GetPositionString(int position)
        {
            switch (position)
            {
                case 1: return "1st";
                case 2: return "2nd";
                case 3: return "3rd";
                default: return $"{position}th";
            }
        }

        private string FormatTime(TimeSpan time)
        {
            if (time == TimeSpan.Zero) return "00:00.000";
            
            return $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds:D3}";
        }
    }
}