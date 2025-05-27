using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using TelemetryAnalyzer.Core.Interfaces;
using TelemetryAnalyzer.Core.Models;

namespace TelemetryAnalyzer.Infrastructure.MemoryReaders.ACC
{
    public class ACCMemoryReader : IMemoryReader
    {
        private const string PHYSICS_MAP_NAME = "Local\\acpmf_physics";
        private const string GRAPHICS_MAP_NAME = "Local\\acpmf_graphics";
        private const string STATIC_MAP_NAME = "Local\\acpmf_static";

        private MemoryMappedFile _physicsFile;
        private MemoryMappedFile _graphicsFile;
        private MemoryMappedFile _staticFile;
        private MemoryMappedViewAccessor _physicsAccessor;
        private MemoryMappedViewAccessor _graphicsAccessor;
        private MemoryMappedViewAccessor _staticAccessor;

        private bool _isReading;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _readingTask;

        public bool IsConnected { get; private set; }
        public event EventHandler<TelemetryData> DataReceived;

        public bool Initialize()
        {
            try
            {
                Cleanup();

                _physicsFile = MemoryMappedFile.OpenExisting(PHYSICS_MAP_NAME);
                _graphicsFile = MemoryMappedFile.OpenExisting(GRAPHICS_MAP_NAME);
                _staticFile = MemoryMappedFile.OpenExisting(STATIC_MAP_NAME);

                _physicsAccessor = _physicsFile.CreateViewAccessor();
                _graphicsAccessor = _graphicsFile.CreateViewAccessor();
                _staticAccessor = _staticFile.CreateViewAccessor();

                IsConnected = true;
                return true;
            }
            catch (FileNotFoundException)
            {
                IsConnected = false;
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ACC Initialize error: {ex.Message}");
                IsConnected = false;
                return false;
            }
        }

        public void StartReading()
        {
            if (_isReading || !IsConnected) return;

            _isReading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            _readingTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var data = ReadTelemetryData();
                        if (data != null)
                        {
                            DataReceived?.Invoke(this, data);
                        }
                        await Task.Delay(16, _cancellationTokenSource.Token); // ~60 FPS
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ACC Reading error: {ex.Message}");
                        // Tentar reconectar
                        if (!Initialize())
                        {
                            await Task.Delay(1000, _cancellationTokenSource.Token);
                        }
                    }
                }
            }, _cancellationTokenSource.Token);
        }

        public TelemetryData ReadTelemetryData()
        {
            if (!IsConnected) return null;

            try
            {
                var physics = ReadPhysicsData();
                var graphics = ReadGraphicsData();
                var staticData = ReadStaticData();

                // Verificar se os dados são válidos
                if (graphics.Status == ACCStatus.AC_OFF)
                    return null;

                return new TelemetryData
                {
                    Timestamp = DateTime.Now,
                    SimulatorName = "Assetto Corsa Competizione",
                    Car = MapCarData(physics, graphics),
                    Track = MapTrackData(staticData, graphics),
                    Session = MapSessionData(graphics, staticData)
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ACC ReadTelemetryData error: {ex.Message}");
                return null;
            }
        }

        private ACCPhysicsData ReadPhysicsData()
        {
            var size = Marshal.SizeOf<ACCPhysicsData>();
            var bytes = new byte[size];
            _physicsAccessor.ReadArray(0, bytes, 0, size);

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<ACCPhysicsData>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private ACCGraphicsData ReadGraphicsData()
        {
            var size = Marshal.SizeOf<ACCGraphicsData>();
            var bytes = new byte[size];
            _graphicsAccessor.ReadArray(0, bytes, 0, size);

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<ACCGraphicsData>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private ACCStaticData ReadStaticData()
        {
            var size = Marshal.SizeOf<ACCStaticData>();
            var bytes = new byte[size];
            _staticAccessor.ReadArray(0, bytes, 0, size);

            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return Marshal.PtrToStructure<ACCStaticData>(handle.AddrOfPinnedObject());
            }
            finally
            {
                handle.Free();
            }
        }

        private CarData MapCarData(ACCPhysicsData physics, ACCGraphicsData graphics)
        {
            return new CarData
            {
                Speed = physics.SpeedKmh,
                RPM = physics.Rpm,
                Gear = physics.Gear,
                Throttle = physics.Gas,
                Brake = physics.Brake,
                Steering = physics.SteerAngle,
                Position = new Vector3(
                    graphics.CarCoordinates[graphics.PlayerCarID * 3],
                    graphics.CarCoordinates[graphics.PlayerCarID * 3 + 1],
                    graphics.CarCoordinates[graphics.PlayerCarID * 3 + 2]
                ),
                Velocity = new Vector3(physics.Velocity[0], physics.Velocity[1], physics.Velocity[2]),
                Acceleration = new Vector3(physics.AccG[0], physics.AccG[1], physics.AccG[2]),
                Tires = new TireData[]
                {
                    new TireData // Front Left
                    {
                        Temperature = (physics.TyreTempI[0] + physics.TyreTempM[0] + physics.TyreTempO[0]) / 3f,
                        Pressure = physics.WheelsPressure[0],
                        Wear = physics.TyreWear[0],
                        IsInContact = physics.WheelLoad[0] > 0
                    },
                    new TireData // Front Right
                    {
                        Temperature = (physics.TyreTempI[1] + physics.TyreTempM[1] + physics.TyreTempO[1]) / 3f,
                        Pressure = physics.WheelsPressure[1],
                        Wear = physics.TyreWear[1],
                        IsInContact = physics.WheelLoad[1] > 0
                    },
                    new TireData // Rear Left
                    {
                        Temperature = (physics.TyreTempI[2] + physics.TyreTempM[2] + physics.TyreTempO[2]) / 3f,
                        Pressure = physics.WheelsPressure[2],
                        Wear = physics.TyreWear[2],
                        IsInContact = physics.WheelLoad[2] > 0
                    },
                    new TireData // Rear Right
                    {
                        Temperature = (physics.TyreTempI[3] + physics.TyreTempM[3] + physics.TyreTempO[3]) / 3f,
                        Pressure = physics.WheelsPressure[3],
                        Wear = physics.TyreWear[3],
                        IsInContact = physics.WheelLoad[3] > 0
                    }
                },
                FuelLevel = physics.Fuel,
                WaterTemperature = 0, // ACC não expõe temperatura da água diretamente
                OilTemperature = 0    // ACC não expõe temperatura do óleo diretamente
            };
        }

        private TrackData MapTrackData(ACCStaticData staticData, ACCGraphicsData graphics)
        {
            return new TrackData
            {
                Name = staticData.Track,
                Length = staticData.TrackSPlineLength,
                TrackMap = new System.Collections.Generic.List<Vector3>(), // Será preenchido durante análise
                TrackTemperature = staticData.RoadTemp,
                AmbientTemperature = staticData.AirTemp
            };
        }

        private SessionData MapSessionData(ACCGraphicsData graphics, ACCStaticData staticData)
        {
            return new SessionData
            {
                Type = MapSessionType(graphics.Session),
                SessionTime = TimeSpan.FromSeconds(graphics.SessionTimeLeft),
                CurrentLapTime = graphics.iCurrentTime > 0 ? TimeSpan.FromMilliseconds(graphics.iCurrentTime) : TimeSpan.Zero,
                BestLapTime = graphics.iBestTime > 0 ? TimeSpan.FromMilliseconds(graphics.iBestTime) : TimeSpan.Zero,
                CurrentLap = graphics.CompletedLaps + 1,
                Position = graphics.Position
            };
        }

        private SessionType MapSessionType(ACCSessionType accSessionType)
        {
            return accSessionType switch
            {
                ACCSessionType.AC_PRACTICE => SessionType.Practice,
                ACCSessionType.AC_QUALIFY => SessionType.Qualifying,
                ACCSessionType.AC_RACE => SessionType.Race,
                ACCSessionType.AC_HOTLAP => SessionType.Hotlap,
                ACCSessionType.AC_HOTSTINT => SessionType.Hotlap,
                ACCSessionType.AC_HOTLAPSUPERPOLE => SessionType.Qualifying,
                _ => SessionType.Practice
            };
        }

        public void StopReading()
        {
            _isReading = false;
            _cancellationTokenSource?.Cancel();
            _readingTask?.Wait(TimeSpan.FromSeconds(2));
        }

        private void Cleanup()
        {
            _physicsAccessor?.Dispose();
            _graphicsAccessor?.Dispose();
            _staticAccessor?.Dispose();
            _physicsFile?.Dispose();
            _graphicsFile?.Dispose();
            _staticFile?.Dispose();
        }

        public void Dispose()
        {
            StopReading();
            Cleanup();
            _cancellationTokenSource?.Dispose();
        }
    }
}