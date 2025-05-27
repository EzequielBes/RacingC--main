using System;
using System.Runtime.InteropServices;

namespace TelemetryAnalyzer.Infrastructure.MemoryReaders.LMU
{
    // Le Mans Ultimate usa estrutura similar ao ACC, mas com algumas diferenças
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct LMUPhysicsData
    {
        public int PacketId;
        public float Gas;                    // 0-1
        public float Brake;                  // 0-1
        public float Fuel;                   // Liters
        public int Gear;                     // 0=N, 1=1st, 2=2nd, etc.
        public int Rpm;
        public float SteerAngle;             // Radians
        public float SpeedKmh;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] Velocity;             // World space velocity (x, y, z)
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] AccG;                 // Local acceleration (x, y, z)
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelSlip;            // Wheel slip ratio
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelLoad;            // Wheel load in Newtons
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelsPressure;       // Wheel pressure in PSI
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelAngularSpeed;    // Wheel angular speed in rad/s
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreWear;             // Tyre wear 0-1
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreDirtyLevel;       // Tyre dirt level 0-1
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreCoreTemperature;  // Tyre core temperature in Celsius
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] CamberRAD;            // Camber in radians
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionTravel;     // Suspension travel in mm
        
        public float Drs;                    // DRS 0-1
        public float TC;                     // Traction Control level
        public float Heading;                // Car heading in radians
        public float Pitch;                  // Car pitch in radians
        public float Roll;                   // Car roll in radians
        public float CgHeight;               // Center of gravity height
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public float[] CarDamage;            // Car damage levels
        
        public int NumberOfTyresOut;         // Number of tyres out of track
        public int PitLimiterOn;            // Pit limiter on
        public float Abs;                    // ABS level
        
        // Campos específicos do LMU para protótipos de endurance
        public float HybridDeployMode;       // Hybrid deployment mode
        public float HybridRegenLevel;       // Hybrid regeneration level
        public float HybridChargeLevel;      // Current hybrid charge level (0-1)
        public float HybridMaxCharge;        // Maximum hybrid charge capacity
        
        public float EngineTemperature;      // Engine temperature (specific to LMU)
        public float OilTemperature;         // Oil temperature
        public float WaterTemperature;       // Water temperature
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] BrakeTemp;            // Brake temperature for each wheel
        
        public float Clutch;                 // Clutch position 0-1
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempI;            // Inner tyre temperature
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempM;            // Middle tyre temperature
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreTempO;            // Outer tyre temperature
        
        public int IsAIControlled;
        public float BrakeBias;              // Brake bias
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LocalVelocity;        // Local velocity
        
        // Campos adicionais para Le Mans Ultimate
        public float FuelConsumptionRate;    // Current fuel consumption rate
        public int TyreSet;                  // Current tyre set number
        public float DownforceLevel;         // Current downforce level
        public int HeadlightsOn;             // Headlights status
        public float RainLightIntensity;     // Rain light intensity
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct LMUGraphicsData
    {
        public int PacketId;
        public LMUStatus Status;
        public LMUSessionType Session;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string CurrentTime;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string LastTime;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string BestTime;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string Split;
        
        public int CompletedLaps;
        public int Position;
        public int iCurrentTime;          // Current time in milliseconds
        public int iLastTime;             // Last lap time in milliseconds
        public int iBestTime;             // Best lap time in milliseconds
        public float SessionTimeLeft;     // Session time left in seconds
        public float DistanceTraveled;    // Distance traveled in meters
        public int IsInPit;
        public int CurrentSectorIndex;
        public int LastSectorTime;        // Last sector time in milliseconds
        public int NumberOfLaps;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TyreCompound;       // Tyre compound
        
        public float ReplayTimeMultiplier;
        public float NormalizedCarPosition; // 0-1 position on track
        public int ActiveCars;             // Number of active cars
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 180)] // 60 cars * 3 coordinates (LMU suporta mais carros)
        public float[] CarCoordinates;     // x,y,z coordinates for each car
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public int[] CarID;                // Car IDs (mais carros que ACC)
        
        public int PlayerCarID;
        public float PenaltyTime;
        public LMUFlag Flag;
        public int IsInPitLane;
        public float SurfaceGrip;          // Surface grip 0-1
        public int MandatoryPitDone;
        public float WindSpeed;            // Wind speed
        public float WindDirection;        // Wind direction in radians
        
        // Campos específicos LMU
        public float TimeOfDay;            // Time of day in hours (0-24) - importante para endurance
        public int WeatherType;            // Weather type
        public float RainIntensity;        // Rain intensity 0-1
        public float TrackWetness;         // Track wetness 0-1
        public int IsNightTime;            // Is it night time
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TrackStatus;         // Track status
        
        public float FuelEstimatedLaps;    // Estimated laps with current fuel
        public int DriverStintTimeLeft;    // Driver stint time left (importante para endurance)
        public int TotalDrivers;           // Total number of drivers in team
        public int CurrentDriver;          // Current driver index
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string CurrentDriverName;   // Current driver name
        
        public float TotalRaceTime;        // Total race time elapsed
        public int LapsToGo;              // Laps remaining in race
        public float TimeToGo;            // Time remaining in race
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct LMUStaticData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string SMVersion;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string LMUVersion;
        
        public int NumberOfSessions;
        public int NumCars;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)] // Nomes mais longos para protótipos
        public string CarModel;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string Track;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerSurname;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string PlayerNick;
        
        public int SectorCount;
        public float MaxTorque;
        public float MaxPower;
        public int MaxRpm;
        public float MaxFuel;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] SuspensionMaxTravel;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreRadius;
        
        public float MaxTurboBoost;
        public float AirTemp;
        public float RoadTemp;
        
        // Dados específicos LMU
        public float MaxHybridCharge;      // Maximum hybrid charge
        public int HybridCategory;         // Hybrid category (LMP1, LMP2, etc.)
        public float MaxDownforce;         // Maximum downforce setting
        public float MinDownforce;         // Minimum downforce setting
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 50)]
        public string CarCategory;         // Car category (LMP1, LMP2, GTE, etc.)
        
        public int IsMultiDriver;          // Is this a multi-driver session
        public int MaxDrivers;             // Maximum drivers allowed
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TrackConfiguration;
        
        public float TrackSPlineLength;
        public int IsTimedRace;            // Is this a timed race (typical for endurance)
        public float RaceDuration;         // Race duration in hours
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string DryTyresName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string WetTyresName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string IntermediateTyresName; // Intermediate tyres (específico LMU)
    }

    public enum LMUStatus
    {
        LMU_OFF = 0,
        LMU_REPLAY = 1,
        LMU_LIVE = 2,
        LMU_PAUSE = 3
    }

    public enum LMUSessionType
    {
        LMU_UNKNOWN = -1,
        LMU_PRACTICE = 0,
        LMU_QUALIFY = 1,
        LMU_RACE = 2,
        LMU_WARMUP = 3,
        LMU_FREE_PRACTICE_1 = 4,
        LMU_FREE_PRACTICE_2 = 5,
        LMU_FREE_PRACTICE_3 = 6,
        LMU_QUALIFYING_1 = 7,
        LMU_QUALIFYING_2 = 8,
        LMU_HYPERPOLE = 9
    }

    public enum LMUFlag
    {
        LMU_NO_FLAG = 0,
        LMU_BLUE_FLAG = 1,
        LMU_YELLOW_FLAG = 2,
        LMU_BLACK_FLAG = 3,
        LMU_WHITE_FLAG = 4,
        LMU_CHECKERED_FLAG = 5,
        LMU_PENALTY_FLAG = 6,
        LMU_GREEN_FLAG = 7,
        LMU_ORANGE_FLAG = 8,
        LMU_FULL_COURSE_YELLOW = 9,
        LMU_SAFETY_CAR = 10
    }
}