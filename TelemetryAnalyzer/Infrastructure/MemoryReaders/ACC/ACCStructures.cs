using System;
using System.Runtime.InteropServices;

namespace TelemetryAnalyzer.Infrastructure.MemoryReaders.ACC
{
    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct ACCPhysicsData
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
        public float[] CarDamage;            // Car damage levels (front, rear, left, right, center)
        
        public int NumberOfTyresOut;         // Number of tyres out of track
        public int PitLimiterOn;            // Pit limiter on
        public float Abs;                    // ABS level
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] WheelsContactPoint;    // Wheel contact point info
        
        public float AutoShifterOn;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] RideHeight;           // Ride height front and rear
        
        public float TurboBoost;
        public float Ballast;
        public float AirDensity;
        public float AirTemp;
        public float RoadTemp;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LocalAngularVel;      // Local angular velocity
        
        public float FinalFF;                // Final force feedback value
        public float PerformanceMeter;       // Performance meter
        public int EngineBrake;
        public int ErsRecoveryLevel;
        public int ErsPowerLevel;
        public int ErsHeatCharging;
        public int ErsIsCharging;
        public float KersCurrentKJ;
        public int DrsAvailable;
        public int DrsEnabled;
        
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
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreContactPoint;     // Tyre contact point
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreContactNormal;    // Tyre contact normal
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] TyreContactHeading;   // Tyre contact heading
        
        public float BrakeBias;              // Brake bias
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] LocalVelocity;        // Local velocity
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct ACCGraphicsData
    {
        public int PacketId;
        public ACCStatus Status;
        public ACCSessionType Session;
        
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
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public float[] CarCoordinates;     // x,y,z coordinates for each car (20 cars * 3 coordinates)
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public int[] CarID;                // Car IDs
        
        public int PlayerCarID;
        public float PenaltyTime;
        public ACCFlag Flag;
        public ACCPenalty PenaltyShortCut;
        public ACCPenalty IdealLineOn;
        public int IsInPitLane;
        public float SurfaceGrip;          // Surface grip 0-1
        public int MandatoryPitDone;
        public float WindSpeed;            // Wind speed
        public float WindDirection;        // Wind direction in radians
        public int IsSetupMenuVisible;
        public int MainDisplayIndex;
        public int SecondaryDisplayIndex;
        public int TC;                     // Traction Control level
        public int TCCut;                  // TC intervention
        public int EngineMap;              // Engine map
        public int ABS;                    // ABS level
        public int FuelXLap;               // Fuel consumption per lap
        public int RainLights;
        public int FlashingLights;
        public int LightsStage;
        public float ExhaustTemperature;
        public int WiperLV;                // Wiper level
        public int DriverStintTotalTimeLeft; // Driver stint time left
        public int DriverStintTimeLeft;    // Driver stint time left
        public int RainTyres;              // Rain tyres equipped
        public int SessionIndex;
        public float UsedFuel;             // Used fuel since last refuel
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string DeltaLapTime;        // Delta lap time string
        
        public int iDeltaLapTime;          // Delta lap time in milliseconds
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string EstimatedLapTime;    // Estimated lap time string
        
        public int iEstimatedLapTime;      // Estimated lap time in milliseconds
        public int IsDeltaPositive;
        public int iSplit;                 // Split time in milliseconds
        public int IsValidLap;
        public float FuelEstimatedLaps;    // Estimated laps with current fuel
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TrackStatus;         // Track status
        
        public int MissingMandatoryPits;
        public float DirectionLightsLeft;
        public float DirectionLightsRight;
        public int GlobalYellow;
        public int GlobalYellow1;
        public int GlobalYellow2;
        public int GlobalYellow3;
        public int GlobalWhite;
        public int GlobalGreen;
        public int GlobalChequered;
        public int GlobalRed;
        public int MfdTyreSet;
        public float MfdFuelToAdd;
        public float MfdTyrePressure;
        
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ACCPenalty[] Penalties;     // Penalty types for each sector
        
        public float PacketId2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Unicode)]
    public struct ACCStaticData
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string SMVersion;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 15)]
        public string ACVersion;
        
        public int NumberOfSessions;
        public int NumCars;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string CarModel;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
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
        public int PenaltiesEnabled;
        public float AidFuelRate;
        public float AidTireRate;
        public float AidMechanicalDamage;
        public int AidAllowTyreBlankets;
        public float AidStability;
        public int AidAutoClutch;
        public int AidAutoBlip;
        public int HasDRS;
        public int HasERS;
        public int HasKERS;
        public float KersMaxJ;
        public int EngineBrakeSettingsCount;
        public int ErsPowerControllerCount;
        public float TrackSPlineLength;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string TrackConfiguration;
        
        public float ErsMaxJ;
        public int IsTimedRace;
        public int HasExtraLap;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string CarSkin;
        
        public int ReversedGridPositions;
        public int PitWindowStart;
        public int PitWindowEnd;
        public int IsOnline;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string DryTyresName;
        
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 33)]
        public string WetTyresName;
    }

    public enum ACCStatus
    {
        AC_OFF = 0,
        AC_REPLAY = 1,
        AC_LIVE = 2,
        AC_PAUSE = 3
    }

    public enum ACCSessionType
    {
        AC_UNKNOWN = -1,
        AC_PRACTICE = 0,
        AC_QUALIFY = 1,
        AC_RACE = 2,
        AC_HOTLAP = 3,
        AC_TIME_ATTACK = 4,
        AC_DRIFT = 5,
        AC_DRAG = 6,
        AC_HOTSTINT = 7,
        AC_HOTLAPSUPERPOLE = 8
    }

    public enum ACCFlag
    {
        AC_NO_FLAG = 0,
        AC_BLUE_FLAG = 1,
        AC_YELLOW_FLAG = 2,
        AC_BLACK_FLAG = 3,
        AC_WHITE_FLAG = 4,
        AC_CHECKERED_FLAG = 5,
        AC_PENALTY_FLAG = 6,
        AC_GREEN_FLAG = 7,
        AC_ORANGE_FLAG = 8
    }

    public enum ACCPenalty
    {
        None = 0,
        DriveThrough_Cutting = 1,
        StopAndGo_10_Cutting = 2,
        StopAndGo_20_Cutting = 3,
        StopAndGo_30_Cutting = 4,
        Disqualified_Cutting = 5,
        RemoveBestLaptime_Cutting = 6,
        DriveThrough_PitSpeeding = 7,
        StopAndGo_10_PitSpeeding = 8,
        StopAndGo_20_PitSpeeding = 9,
        StopAndGo_30_PitSpeeding = 10,
        Disqualified_PitSpeeding = 11,
        RemoveBestLaptime_PitSpeeding = 12,
        Disqualified_IgnoredMandatoryPit = 13,
        PostRaceTime = 14,
        Disqualified_Trolling = 15,
        Disqualified_PitEntry = 16,
        Disqualified_PitExit = 17,
        Disqualified_WrongWay = 18,
        DriveThrough_IgnoredDriverStint = 19,
        Disqualified_IgnoredDriverStint = 20,
        Disqualified_ExceededDriverStintLimit = 21
    }
}