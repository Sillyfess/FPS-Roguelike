using System.Numerics;
using FPSRoguelike.Entities;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Manages player state, health, and input
/// </summary>
public interface IPlayerSystem : IGameSystem
{
    /// <summary>
    /// Player position in world
    /// </summary>
    Vector3 Position { get; }
    
    /// <summary>
    /// Player health component
    /// </summary>
    PlayerHealth? Health { get; }
    
    /// <summary>
    /// Is player alive
    /// </summary>
    bool IsAlive { get; }
    
    /// <summary>
    /// Current score
    /// </summary>
    int Score { get; }
    
    /// <summary>
    /// Add to player score
    /// </summary>
    void AddScore(int points);
    
    /// <summary>
    /// Damage the player
    /// </summary>
    void TakeDamage(float damage, Vector3 damageSource);
    
    /// <summary>
    /// Respawn the player
    /// </summary>
    void Respawn();
    
    /// <summary>
    /// Process player input
    /// </summary>
    void ProcessInput(float deltaTime);
}