public class ACCMemoryReader : IMemoryReader
{
    private bool _isReading;
    private CancellationTokenSource _cancellationTokenSource;
    private const string PHYSICS_MAP_NAME = "Local\\acpmf_physics";
    private const string GRAPHICS_MAP_NAME = "Local\\acpmf_graphics";
    private const string STATIC_MAP_NAME = "Local\\acpmf_static";
    
    private MemoryMappedFile _physicsFile;
    private MemoryMappedFile _graphicsFile;
    private MemoryMappedFile _staticFile;

    public bool IsConnected { get; private set; }
    public event EventHandler<TelemetryData> DataReceived;

    public bool Initialize()
    {
        try
        {
            _physicsFile = MemoryMappedFile.OpenExisting(PHYSICS_MAP_NAME);
            _graphicsFile = MemoryMappedFile.OpenExisting(GRAPHICS_MAP_NAME);
            _staticFile = MemoryMappedFile.OpenExisting(STATIC_MAP_NAME);
            IsConnected = true;
            return true;
        }
        catch (FileNotFoundException)
        {
            IsConnected = false;
            return false;
        }
    }

    public void StartReading()
    {
        if (_isReading) return;
        
        _isReading = true;
        _cancellationTokenSource = new CancellationTokenSource();
        
        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                var data = ReadTelemetryData();
                if (data != null)
                {
                    DataReceived?.Invoke(this, data);
                }
                await Task.Delay(16, _cancellationTokenSource.Token); // ~60 FPS
            }
        }, _cancellationTokenSource.Token);
    }

    public TelemetryData ReadTelemetryData()
    {
        if (!IsConnected) return null;

        try
        {
            var physicsData = ReadPhysicsData();
            var graphicsData = ReadGraphicsData();
            var staticData = ReadStaticData();

            return new TelemetryData
            {
                Timestamp = DateTime.Now,
                SimulatorName = "Assetto Corsa Competizione",
                Car = MapCarData(physicsData, graphicsData),
                Track = MapTrackData(staticData, graphicsData),
                Session = MapSessionData(graphicsData)
            };
        }
        catch (Exception ex)
        {
            // Log error
            return null;
        }
    }

    private ACCPhysicsData ReadPhysicsData()
    {
        using var accessor = _physicsFile.CreateViewAccessor();
        var data = new ACCPhysicsData();
        
        // Mapeamento dos dados da struct ACC
        data.Speed = accessor.ReadSingle(0x100); // Offset para velocidade
        data.RPM = accessor.ReadSingle(0x104);   // RPM
        data.Gear = accessor.ReadInt32(0x108);   // Marcha
        // ... mais campos conforme documentação ACC
        
        return data;
    }

    // Métodos similares para Graphics e Static data...
    
    public void StopReading()
    {
        _isReading = false;
        _cancellationTokenSource?.Cancel();
    }

    public void Dispose()
    {
        StopReading();
        _physicsFile?.Dispose();
        _graphicsFile?.Dispose();
        _staticFile?.Dispose();
    }
}

public struct ACCPhysicsData
{
    public float Speed;
    public float RPM;
    public int Gear;
    public float Throttle;
    public float Brake;
    public float Steering;
    // ... outros campos
}