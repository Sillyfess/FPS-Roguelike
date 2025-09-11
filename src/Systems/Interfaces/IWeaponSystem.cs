using FPSRoguelike.Combat;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Manages weapon inventory and switching
/// </summary>
public interface IWeaponSystem : IGameSystem
{
    /// <summary>
    /// Currently equipped weapon
    /// </summary>
    Weapon? CurrentWeapon { get; }
    
    /// <summary>
    /// Current weapon index
    /// </summary>
    int CurrentWeaponIndex { get; }
    
    /// <summary>
    /// Switch to weapon by index
    /// </summary>
    void SelectWeapon(int index);
    
    /// <summary>
    /// Try to fire current weapon
    /// </summary>
    bool TryFire();
    
    /// <summary>
    /// Reload current weapon if applicable
    /// </summary>
    void Reload();
    
    /// <summary>
    /// Get weapon at index
    /// </summary>
    Weapon? GetWeapon(int index);
    
    /// <summary>
    /// Get total weapon count
    /// </summary>
    int WeaponCount { get; }
}