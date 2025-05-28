using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Application.Services;
using TelemetryAnalyzer.Application.UseCases;
using TelemetryAnalyzer.Infrastructure.MemoryReaders.ACC;
using TelemetryAnalyzer.Infrastructure.MemoryReaders.LMU;
using TelemetryAnalyzer.Infrastructure.MemoryReaders;
using TelemetryAnalyzer.Infrastructure.FileImporters;
using TelemetryAnalyzer.Infrastructure.DataProcessors;
using TelemetryAnalyzer.Infrastructure.Repositories;
using TelemetryAnalyzer.Infrastructure.Data;
using TelemetryAnalyzer.Infrastructure.Services;
using TelemetryAnalyzer.Presentation.WPF;

namespace TelemetryAnalyzer
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                // Setup logging
                var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddNLog("NLog.config");
                    builder.SetMinimumLevel(LogLevel.Information);
                });

                // Create host builder
                var hostBuilder = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        ConfigureServices(services, loggerFactory);
                    })
                    .UseConsoleLifetime();

                var host = hostBuilder.Build();

                // Ensure database is created
                using (var scope = host.Services.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
                    dbContext.Database.EnsureCreated();
                }

                // Create and run WPF application
                var app = new App();
                app.InitializeComponent();
                
                // Set service provider in App
                var serviceProvider = host.Services;
                typeof(App).GetProperty("ServiceProvider")?.SetValue(app, serviceProvider);

                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro crítico na inicialização: {ex.Message}\n\n{ex.StackTrace}", 
                               "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        private static void ConfigureServices(IServiceCollection services, ILoggerFactory loggerFactory)
        {
            // Logging
            services.AddSingleton(loggerFactory);
            services.AddLogging();

            // Database
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "telemetry.db");
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
            
            services.AddDbContext<TelemetryDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            // Memory Readers
            services.AddSingleton<IMemoryReader, ACCMemoryReader>();
            services.AddSingleton<IMemoryReader, LMUMemoryReader>();
            services.AddSingleton<IMemoryReader, iRacingMemoryReader>();

            // File Importers
            services.AddSingleton<IFileImporter, MotecFileImporter>();
            services.AddSingleton<IFileImporter, CSVTelemetryImporter>();

            // Services
            services.AddSingleton<ITelemetryProcessor, TelemetryProcessor>();
            services.AddSingleton<ITelemetryRepository, SqliteTelemetryRepository>();
            services.AddSingleton<SimulatorDetectionService>();
            services.AddSingleton<LapAnalysisService>();
            services.AddSingleton<LapComparisonService>();
            services.AddSingleton<TrackMapService>();
            services.AddSingleton<PerformanceAnalysisService>();

            // Use Cases
            services.AddSingleton<RealTimeTelemetryUseCase>();
            services.AddSingleton<ImportTelemetryUseCase>();
            services.AddSingleton<AnalyzeLapUseCase>();

            // WPF Windows and ViewModels
            services.AddTransient<MainWindow>();
            services.AddTransient<MainViewModel>();
        }
    }
}