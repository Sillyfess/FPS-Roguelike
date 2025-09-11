using System.Text.Json;
using System.Text.Json.Serialization;
using FPSRoguelike.UI;

namespace FPSRoguelike.Core;

/// <summary>
/// Game settings that persist between sessions
/// </summary>
public class Settings
{
    private static readonly ILogger logger = new ConsoleLogger();
    // Display settings
    public float FieldOfView { get; set; } = 90f;
    public bool VSync { get; set; } = false;
    
    // Input settings  
    public float MouseSensitivity { get; set; } = 0.3f;
    
    // Audio settings
    public float MasterVolume { get; set; } = 1.0f;
    public float EffectsVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 1.0f;
    
    // Graphics settings
    public int QualityLevel { get; set; } = 1; // 0=Low, 1=Medium, 2=High
    
    // Debug settings
    public bool ShowDebugInfo { get; set; } = false;
    
    private static readonly string SettingsPath = Path.Combine(
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
        "FPSRoguelike",
        "settings.json"
    );
    
    /// <summary>
    /// Load settings from disk, or return defaults if file doesn't exist
    /// </summary>
    public static Settings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<Settings>(json);
                return settings ?? new Settings();
            }
        }
        catch (Exception ex)
        {
            // If loading fails, just use defaults
            logger.LogWarning($"Failed to load settings: {ex.Message}");
        }
        
        return new Settings();
    }
    
    /// <summary>
    /// Save settings to disk
    /// </summary>
    public void Save()
    {
        try
        {
            // Ensure directory exists
            string? directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Serialize with indentation for readability
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to save settings: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Apply settings to the game systems
    /// </summary>
    public void Apply(SimpleUIManager? uiManager, ImGuiHUD? imGuiHud)
    {
        // Apply to UI managers
        if (uiManager != null)
        {
            uiManager.FieldOfView = FieldOfView;
            uiManager.MouseSensitivity = MouseSensitivity;
        }
        
        if (imGuiHud != null && ShowDebugInfo)
        {
            imGuiHud.ToggleDebugInfo();
        }
    }
}