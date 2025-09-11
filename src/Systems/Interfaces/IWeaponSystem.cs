using System.Numerics;
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
    /// <param name="position">Position to fire from</param>
    /// <param name="aimDirection">Direction to fire in</param>
    bool TryFire(Vector3 position, Vector3 aimDirection);
    
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