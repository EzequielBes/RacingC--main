using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace TelemetryAnalyzer.Presentation.WPF
{
    public partial class App : Application
    {
        public IServiceProvider ServiceProvider { get; set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                if (ServiceProvider == null)
                {
                    MessageBox.Show("Erro na inicialização dos serviços.", "Erro", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                    Shutdown(1);
                    return;
                }

                var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir janela principal: {ex.Message}", "Erro", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Cleanup
            if (ServiceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
            
            base.OnExit(e);
        }
    }
}
