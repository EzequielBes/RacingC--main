public class CarData
{
    public float Speed { get; set; }
    public float RPM { get; set; }
    public int Gear { get; set; }
    public float Throttle { get; set; }
    public float Brake { get; set; }
    public float Steering { get; set; }
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public Vector3 Acceleration { get; set; }
    public TireData[] Tires { get; set; } = new TireData[4];
    public float FuelLevel { get; set; }
    public float WaterTemperature { get; set; }
    public float OilTemperature { get; set; }
}
