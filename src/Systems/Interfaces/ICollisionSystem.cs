using System.Numerics;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Handles all collision detection and response
/// </summary>
public interface ICollisionSystem : IGameSystem
{
    /// <summary>
    /// Check collisions between projectiles and enemies
    /// </summary>
    void CheckProjectileEnemyCollisions(IReadOnlyList<Projectile> projectiles, 
                                       IReadOnlyList<Enemy> enemies);
    
    /// <summary>
    /// Check if position collides with any obstacle
    /// </summary>
    bool CheckObstacleCollision(Vector3 position, float radius, 
                               IReadOnlyList<Obstacle> obstacles);
    
    /// <summary>
    /// Check if enemy can hit player with melee
    /// </summary>
    bool CheckMeleeRange(Vector3 enemyPos, Vector3 playerPos, float range);
    
    /// <summary>
    /// Perform raycast against enemies
    /// </summary>
    Enemy? RaycastEnemies(Vector3 origin, Vector3 direction, float maxDistance,
                         IReadOnlyList<Enemy> enemies);
    
    /// <summary>
    /// Get nearest enemy to position
    /// </summary>
    Enemy? GetNearestEnemy(Vector3 position, IReadOnlyList<Enemy> enemies);
}