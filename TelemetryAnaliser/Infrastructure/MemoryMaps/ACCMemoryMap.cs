public static class ACCMemoryMap
{
    // Physics data offsets (baseados na documentação oficial ACC)
    public const int SPEED_OFFSET = 0x00;          // float - km/h
    public const int RPM_OFFSET = 0x04;            // float
    public const int GEAR_OFFSET = 0x2C;           // int
    public const int THROTTLE_OFFSET = 0x30;       // float 0-1
    public const int BRAKE_OFFSET = 0x34;          // float 0-1
    public const int STEERING_OFFSET = 0x38;       // float -1 to 1
    
    // Tire temperatures (4 tires * 3 sections each)
    public const int TIRE_TEMP_FL_OFFSET = 0x60;   // Front Left
    public const int TIRE_TEMP_FR_OFFSET = 0x6C;   // Front Right
    public const int TIRE_TEMP_RL_OFFSET = 0x78;   // Rear Left
    public const int TIRE_TEMP_RR_OFFSET = 0x84;   // Rear Right
    
    // Car position (world coordinates)
    public const int POS_X_OFFSET = 0x90;          // float
    public const int POS_Y_OFFSET = 0x94;          // float  
    public const int POS_Z_OFFSET = 0x98;          // float
    
    // Graphics data offsets
    public const int CURRENT_LAP_OFFSET = 0x00;    // int
    public const int LAST_LAP_TIME_OFFSET = 0x04;  // int (milliseconds)
    public const int BEST_LAP_TIME_OFFSET = 0x08;  // int (milliseconds)
}