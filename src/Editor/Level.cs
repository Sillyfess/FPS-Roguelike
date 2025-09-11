using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Editor;

/// <summary>
/// Represents a serializable level with all its components
/// </summary>
public class Level
{
    public string Name { get; set; } = "Untitled Level";
    public string Author { get; set; } = "Unknown";
    public string Version { get; set; } = "1.0.0";
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    
    public Vector3 PlayerSpawnPosition { get; set; } = new Vector3(0, 1, 0);
    public float PlayerSpawnRotation { get; set; } = 0f;
    
    public List<ObstacleData> Obstacles { get; set; } = new List<ObstacleData>();
    public List<EnemySpawnPoint> EnemySpawns { get; set; } = new List<EnemySpawnPoint>();
    public LevelSettings Settings { get; set; } = new LevelSettings();
    
    /// <summary>
    /// Serializable obstacle data
    /// </summary>
    public class ObstacleData
    {
        public Vector3 Position { get; set; }
        public ObstacleType Type { get; set; }
        public float Rotation { get; set; }
        public bool IsDestructible { get; set; }
        public float Health { get; set; }
        public Vector3? CustomColor { get; set; }
        public Vector3? CustomSize { get; set; }
        
        public ObstacleData() { }
        
        public ObstacleData(Obstacle obstacle)
        {
            Position = obstacle.Position;
            Type = obstacle.Type;
            Rotation = obstacle.Rotation;
            IsDestructible = obstacle.IsDestructible;
            Health = obstacle.Health;
            // Only store custom values if they differ from defaults
            // This keeps the JSON cleaner
        }
        
        public Obstacle ToObstacle()
        {
            var obstacle = new Obstacle(Position, Type, Rotation);
            
            // Apply custom properties if present
            if (CustomColor.HasValue)
                obstacle.Color = CustomColor.Value;
            if (CustomSize.HasValue)
                obstacle.Size = CustomSize.Value;
                
            return obstacle;
        }
    }
    
    /// <summary>
    /// Enemy spawn point configuration
    /// </summary>
    public class EnemySpawnPoint
    {
        public Vector3 Position { get; set; }
        public string EnemyType { get; set; } = "Normal"; // Normal, Boss, etc.
        public float Health { get; set; } = 100f;
        public float SpawnDelay { get; set; } = 0f;
        public int WaveNumber { get; set; } = 1;
        
        public EnemySpawnPoint() { }
        
        public EnemySpawnPoint(Vector3 position, string type = "Normal")
        {
            Position = position;
            EnemyType = type;
        }
    }
    
    /// <summary>
    /// Level-specific settings
    /// </summary>
    public class LevelSettings
    {
        public float Gravity { get; set; } = -20f;
        public float AmbientLight { get; set; } = 0.4f;
        public Vector3 SkyColor { get; set; } = new Vector3(0.2f, 0.3f, 0.5f);
        public Vector3 FogColor { get; set; } = new Vector3(0.5f, 0.5f, 0.5f);
        public float FogDensity { get; set; } = 0.01f;
        public bool EnableFog { get; set; } = false;
        public float ArenaRadius { get; set; } = 50f;
    }
    
    /// <summary>
    /// Save level to JSON file
    /// </summary>
    public void SaveToFile(string filepath)
    {
        ModifiedDate = DateTime.Now;
        
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new Vector3JsonConverter() }
        };
        
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(filepath, json);
    }
    
    /// <summary>
    /// Load level from JSON file
    /// </summary>
    public static Level LoadFromFile(string filepath)
    {
        if (!File.Exists(filepath))
            throw new FileNotFoundException($"Level file not found: {filepath}");
            
        var options = new JsonSerializerOptions
        {
            Converters = { new Vector3JsonConverter() }
        };
        
        string json = File.ReadAllText(filepath);
        return JsonSerializer.Deserialize<Level>(json, options) 
            ?? throw new InvalidOperationException("Failed to deserialize level");
    }
    
    /// <summary>
    /// Convert level data to game obstacles
    /// </summary>
    public List<Obstacle> GetObstacles()
    {
        return Obstacles.Select(o => o.ToObstacle()).ToList();
    }
}

/// <summary>
/// Custom JSON converter for Vector3 to make JSON more readable
/// </summary>
public class Vector3JsonConverter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();
            
        float x = 0, y = 0, z = 0;
        
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                return new Vector3(x, y, z);
                
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString();
                reader.Read();
                
                switch (propertyName?.ToLower())
                {
                    case "x":
                        x = reader.GetSingle();
                        break;
                    case "y":
                        y = reader.GetSingle();
                        break;
                    case "z":
                        z = reader.GetSingle();
                        break;
                }
            }
        }
        
        throw new JsonException();
    }
    
    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteNumber("z", value.Z);
        writer.WriteEndObject();
    }
}