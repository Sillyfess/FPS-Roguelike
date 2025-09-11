namespace FPSRoguelike.Core;

/// <summary>
/// Central location for all game constants following CODE_STANDARDS.md
/// Prefixes: DEFAULT_, MAX_, MIN_, BASE_
/// </summary>
public static class Constants
{
    // ===== TIMING AND PHYSICS =====
    public const double FIXED_TIMESTEP = 1.0 / 60.0;
    public const int MAX_PHYSICS_UPDATES = 3; // Reduced from 10 to prevent 50ms+ jumps
    public const float PHYSICS_EPSILON = 0.01f; // For floating point comparisons
    
    // ===== RENDERING =====
    public const float DEFAULT_FOV = 90f;
    public const float MIN_FOV = 60f;
    public const float MAX_FOV = 120f;
    public const float CAMERA_NEAR_PLANE = 0.1f;
    public const float CAMERA_FAR_PLANE = 1000f;
    public const float FOV_TO_RADIANS = MathF.PI / 180f;
    
    // ===== ENTITY LIMITS =====
    public const int MAX_ENEMIES = 30;
    public const int MAX_PROJECTILES = 500;
    public const int MAX_OBSTACLES = 50;
    public const int MAX_ENEMY_INSTANCES = 30; // For GPU instancing
    public const int MAX_PROJECTILE_INSTANCES = 500;
    
    // ===== ENTITY SCALES =====
    public const float DEFAULT_ENEMY_SCALE = 1f;
    public const float BOSS_SCALE = 3f;
    public const float PROJECTILE_SCALE = 0.1f;
    public const float OBSTACLE_DEFAULT_SCALE = 1f;
    
    // ===== WORLD GEOMETRY =====
    public const float GROUND_WIDTH = 100f;
    public const float GROUND_HEIGHT = 0.1f;
    public const float GROUND_DEPTH = 100f;
    public const float GROUND_Y_POSITION = -1f;
    
    // ===== COLORS (as Vector3 RGB) =====
    public const float GROUND_COLOR_R = 0.2f;
    public const float GROUND_COLOR_G = 0.3f;
    public const float GROUND_COLOR_B = 0.2f;
    public const float OBSTACLE_COLOR_R = 0.3f;
    public const float OBSTACLE_COLOR_G = 0.3f;
    public const float OBSTACLE_COLOR_B = 0.3f;
    public const float PROJECTILE_COLOR_R = 1f;
    public const float PROJECTILE_COLOR_G = 1f;
    public const float PROJECTILE_COLOR_B = 0f;
    
    // ===== COMBAT =====
    public const float MELEE_ENEMY_DAMAGE = 10f;
    public const float MELEE_ENEMY_RANGE = 2f;
    public const float KATANA_RANGE = 3f;
    public const float KATANA_ARC_ANGLE = 120f; // degrees
    public const float KATANA_VERTICAL_ARC = 60f; // degrees for Y-axis
    public const float DEFAULT_PROJECTILE_SPEED = 50f;
    public const float PROJECTILE_LIFETIME = 5f;
    public const float PROJECTILE_RADIUS = 0.2f;
    
    // ===== INPUT =====
    public const float DEFAULT_MOUSE_SENSITIVITY = 0.3f;
    public const float MIN_MOUSE_SENSITIVITY = 0.1f;
    public const float MAX_MOUSE_SENSITIVITY = 2.0f;
    public const float MAX_MOUSE_DELTA = 1000f; // Clamp for high DPI mice
    public const int MAX_INPUT_ERRORS = 10; // Before attempting recovery
    
    // ===== PLAYER =====
    public const float DEFAULT_PLAYER_HEALTH = 100f;
    public const float DEFAULT_MOVE_SPEED = 5f;
    public const float DEFAULT_JUMP_FORCE = 8f;
    public const float GRAVITY = -20f;
    public const float GROUND_LEVEL = 0f;
    
    // ===== UI =====
    public const float AUTO_SAVE_INTERVAL = 30f; // seconds
    public const float FPS_UPDATE_INTERVAL = 1.0f; // Update FPS counter every second
    
    // ===== WEAPON STATS =====
    public const float REVOLVER_DAMAGE = 50f;
    public const float REVOLVER_FIRE_RATE = 0.5f;
    public const int REVOLVER_MAG_SIZE = 6;
    public const float REVOLVER_RELOAD_TIME = 2f;
    
    public const float SMG_DAMAGE = 15f;
    public const float SMG_FIRE_RATE = 0.1f;
    public const int SMG_MAG_SIZE = 30;
    public const float SMG_RELOAD_TIME = 1.5f;
    
    public const float KATANA_DAMAGE = 75f;
    public const float KATANA_FIRE_RATE = 0.8f;
    
    // ===== WAVE SYSTEM =====
    public const int BASE_ENEMIES_PER_WAVE = 5;
    public const float WAVE_ENEMY_HEALTH_MULTIPLIER = 1.2f;
    public const float BOSS_HEALTH_MULTIPLIER = 5f;
    
    // ===== PERFORMANCE =====
    public const int INSTANCE_DATA_STRIDE = 19; // floats per instance (16 for matrix + 3 for color)
    public const int VERTEX_STRIDE = 6; // Position (3) + Normal (3)
    
    // ===== TOLERANCES =====
    public const float VECTOR_NORMALIZATION_EPSILON = 0.01f; // Much more reasonable than 0.0001f
    public const float POSITION_EPSILON = 0.01f; // For position comparisons
    public const float ROTATION_EPSILON = 0.001f; // For rotation comparisons
    
    // ===== DEBUG =====
    public const bool DEFAULT_DEBUG_INFO_ENABLED = false;
    public const bool DEFAULT_VSYNC = false;
    
    // ===== WINDOW =====
    public const int DEFAULT_WINDOW_WIDTH = 1280;
    public const int DEFAULT_WINDOW_HEIGHT = 720;
    public const string WINDOW_TITLE = "FPS Roguelike Prototype";
}