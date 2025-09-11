using FPSRoguelike.Entities;
using FPSRoguelike.Combat;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Coordinates between different UI systems
/// </summary>
public interface IUICoordinator : IGameSystem
{
    /// <summary>
    /// Show or hide settings menu
    /// </summary>
    void ToggleSettingsMenu();
    
    /// <summary>
    /// Is settings menu visible
    /// </summary>
    bool IsSettingsMenuVisible { get; }
    
    /// <summary>
    /// Update HUD with game state
    /// </summary>
    void UpdateHUD(PlayerHealth? health, Weapon? currentWeapon, int score, 
                   int wave, int enemiesAlive, float deltaTime);
    
    /// <summary>
    /// Render all UI elements
    /// </summary>
    void Render(float deltaTime);
    
    /// <summary>
    /// Get current mouse sensitivity setting
    /// </summary>
    float MouseSensitivity { get; }
    
    /// <summary>
    /// Get current field of view setting
    /// </summary>
    float FieldOfView { get; }
}