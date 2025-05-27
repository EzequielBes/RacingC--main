using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Infrastructure.FileImporters
{
    public class CSVTelemetryImporter : IFileImporter
    {
        public string[] SupportedExtensions => new[] { ".csv", ".txt" };

        public bool CanImport(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return SupportedExtensions.Contains(extension);
        }

        public async Task<List<TelemetryData>> ImportAsync(string filePath)
        {
            var telemetryData = new List<TelemetryData>();

            try
            {
                var lines = await File.ReadAllLinesAsync(filePath);
                if (lines.Length < 2) return telemetryData;

                // Parse header
                var header = lines[0].Split(',').Select(h => h.Trim().ToLower()).ToArray();
                var columnMap = MapColumns(header);

                // Parse data lines
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = lines[i].Split(',');
                        if (values.Length != header.Length) continue;

                        var data = ParseDataLine(values, columnMap);
                        if (data != null)
                        {
                            telemetryData.Add(data);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.WriteLine($"Error parsing line {i}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ImportException($"Error importing CSV file: {ex.Message}", ex);
            }

            return telemetryData;
        }

        private Dictionary<string, int> MapColumns(string[] header)
        {
            var map = new Dictionary<string, int>();

            for (int i = 0; i < header.Length; i++)
            {
                var column = header[i];
                
                // Map common column names
                if (column.Contains("time") || column.Contains("timestamp"))
                    map["timestamp"] = i;
                else if (column.Contains("speed") || column.Contains("velocity"))
                    map["speed"] = i;
                else if (column.Contains("rpm"))
                    map["rpm"] = i;
                else if (column.Contains("gear"))
                    map["gear"] = i;
                else if (column.Contains("throttle") || column.Contains("gas"))
                    map["throttle"] = i;
                else if (column.Contains("brake"))
                    map["brake"] = i;
                else if (column.Contains("steering") || column.Contains("steer"))
                    map["steering"] = i;
                else if (column.Contains("posx") || column.Contains("pos_x"))
                    map["posx"] = i;
                else if (column.Contains("posy") || column.Contains("pos_y"))
                    map["posy"] = i;
                else if (column.Contains("posz") || column.Contains("pos_z"))
                    map["posz"] = i;
                else if (column.Contains("fuel"))
                    map["fuel"] = i;
            }

            return map;
        }

        private TelemetryData ParseDataLine(string[] values, Dictionary<string, int> columnMap)
        {
            var data = new TelemetryData
            {
                SimulatorName = "CSV Import",
                Car = new CarData(),
                Track = new TrackData(),
                Session = new SessionData()
            };

            // Parse timestamp
            if (columnMap.TryGetValue("timestamp", out int timestampIndex))
            {
                if (DateTime.TryParse(values[timestampIndex], out DateTime timestamp))
                {
                    data.Timestamp = timestamp;
                }
                else if (double.TryParse(values[timestampIndex], NumberStyles.Float, 
                         CultureInfo.InvariantCulture, out double unixTime))
                {
                    data.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)unixTime).DateTime;
                }
            }

            if (data.Timestamp == default)
            {
                data.Timestamp = DateTime.Now;
            }

            // Parse car data
            if (columnMap.TryGetValue("speed", out int speedIndex))
            {
                float.TryParse(values[speedIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out data.Car.Speed);
            }

            if (columnMap.TryGetValue("rpm", out int rpmIndex))
            {
                float.TryParse(values[rpmIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out var rpm);
                data.Car.RPM = rpm;
            }

            if (columnMap.TryGetValue("gear", out int gearIndex))
            {
                int.TryParse(values[gearIndex], out data.Car.Gear);
            }

            if (columnMap.TryGetValue("throttle", out int throttleIndex))
            {
                float.TryParse(values[throttleIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out data.Car.Throttle);
            }

            if (columnMap.TryGetValue("brake", out int brakeIndex))
            {
                float.TryParse(values[brakeIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out data.Car.Brake);
            }

            if (columnMap.TryGetValue("steering", out int steeringIndex))
            {
                float.TryParse(values[steeringIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out data.Car.Steering);
            }

            if (columnMap.TryGetValue("fuel", out int fuelIndex))
            {
                float.TryParse(values[fuelIndex], NumberStyles.Float, 
                              CultureInfo.InvariantCulture, out data.Car.FuelLevel);
            }

            // Parse position
            var posX = 0f; var posY = 0f; var posZ = 0f;
            
            if (columnMap.TryGetValue("posx", out int posXIndex))
                float.TryParse(values[posXIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out posX);
            
            if (columnMap.TryGetValue("posy", out int posYIndex))
                float.TryParse(values[posYIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out posY);
            
            if (columnMap.TryGetValue("posz", out int posZIndex))
                float.TryParse(values[posZIndex], NumberStyles.Float, CultureInfo.InvariantCulture, out posZ);

            data.Car.Position = new Vector3(posX, posY, posZ);

            return data;
        }
    }

    public class ImportException : Exception
    {
        public ImportException(string message) : base(message) { }
        public ImportException(string message, Exception innerException) : base(message, innerException) { }
    }
}
