using System.Numerics;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Manages all game entities (enemies, projectiles, obstacles)
/// </summary>
public interface IEntityManager : IGameSystem
{
    /// <summary>
    /// Get all active enemies
    /// </summary>
    IReadOnlyList<Enemy> Enemies { get; }
    
    /// <summary>
    /// Get all active projectiles
    /// </summary>
    IReadOnlyList<Projectile> Projectiles { get; }
    
    /// <summary>
    /// Get all obstacles
    /// </summary>
    IReadOnlyList<Obstacle> Obstacles { get; }
    
    /// <summary>
    /// Spawn a new enemy at position
    /// </summary>
    Enemy SpawnEnemy(Vector3 position, float health, bool isBoss = false);
    
    /// <summary>
    /// Fire a projectile
    /// </summary>
    void FireProjectile(Vector3 position, Vector3 direction, float speed, float damage, 
                       Action<Enemy>? onHit = null);
    
    /// <summary>
    /// Remove destroyed enemies
    /// </summary>
    void RemoveDeadEnemies();
    
    /// <summary>
    /// Create obstacle at position
    /// </summary>
    void CreateObstacle(Vector3 position, Vector3 size, float health = 100f);
    
    /// <summary>
    /// Get enemy by ID
    /// </summary>
    Enemy? GetEnemyById(int id);
    
    /// <summary>
    /// Get enemies within range of position
    /// </summary>
    List<Enemy> GetEnemiesInRange(Vector3 position, float range);
    
    /// <summary>
    /// Clear all entities
    /// </summary>
    void ClearAll();
    
    /// <summary>
    /// Get count of alive enemies
    /// </summary>
    int GetAliveEnemyCount();
}