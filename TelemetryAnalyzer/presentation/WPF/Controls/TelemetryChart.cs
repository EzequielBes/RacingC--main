using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using TelemetryAnalyzer.Core.Models;

public partial class TelemetryChart : UserControl
{
    public static readonly DependencyProperty TelemetryDataProperty =
        DependencyProperty.Register("TelemetryData", typeof(List<TelemetryData>), 
            typeof(TelemetryChart), new PropertyMetadata(OnTelemetryDataChanged));

    public List<TelemetryData> TelemetryData
    {
        get => (List<TelemetryData>)GetValue(TelemetryDataProperty);
        set => SetValue(TelemetryDataProperty, value);
    }

    private static void OnTelemetryDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TelemetryChart chart && e.NewValue is List<TelemetryData> data)
        {
            chart.UpdateChart(data);
        }
    }

    private void UpdateChart(List<TelemetryData> data)
    {
        // Usando OxyPlot para gráficos
        var plotModel = new PlotModel { Title = "Telemetria" };
        
        // Gráfico de velocidade
        var speedSeries = new LineSeries { Title = "Velocidade (km/h)", Color = OxyColors.Blue };
        
        // Gráfico de RPM
        var rpmSeries = new LineSeries { Title = "RPM", Color = OxyColors.Red };
        
        for (int i = 0; i < data.Count; i++)
        {
            speedSeries.Points.Add(new DataPoint(i, data[i].Car.Speed));
            rpmSeries.Points.Add(new DataPoint(i, data[i].Car.RPM));
        }
        
        plotModel.Series.Add(speedSeries);
        plotModel.Series.Add(rpmSeries);
        
        ChartView.Model = plotModel;
    }
}