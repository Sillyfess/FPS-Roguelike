using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Unified management of all game entities
/// </summary>
public class EntityManager : IEntityManager
{
    // Constants
    private const int MAX_PROJECTILES = 500;
    private const int MAX_ENEMIES = 30;
    private const int MAX_OBSTACLES = 50;
    
    // Entity lists
    private readonly List<Enemy> enemies = new();
    private readonly List<Projectile> projectiles = new();
    private readonly List<Obstacle> obstacles = new();
    
    // Object pools
    private readonly Queue<Projectile> projectilePool = new();
    
    // Public accessors
    public IReadOnlyList<Enemy> Enemies => enemies;
    public IReadOnlyList<Projectile> Projectiles => projectiles;
    public IReadOnlyList<Obstacle> Obstacles => obstacles;
    
    public void Initialize()
    {
        // Pre-allocate projectile pool
        for (int i = 0; i < MAX_PROJECTILES; i++)
        {
            projectilePool.Enqueue(new Projectile());
        }
    }
    
    public Enemy SpawnEnemy(Vector3 position, float health, bool isBoss = false)
    {
        if (enemies.Count >= MAX_ENEMIES)
        {
            // Find and replace first dead enemy
            var deadEnemy = enemies.FirstOrDefault(e => !e.IsAlive);
            if (deadEnemy != null)
            {
                enemies.Remove(deadEnemy);
            }
        }
        
        Enemy enemy;
        if (isBoss)
        {
            enemy = new Boss(position, health);
        }
        else
        {
            enemy = new Enemy(position, health);
        }
        
        enemies.Add(enemy);
        return enemy;
    }
    
    public void FireProjectile(Vector3 position, Vector3 direction, float speed, float damage, 
                              Action<Enemy>? onHit = null)
    {
        // Validate inputs
        if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z) ||
            float.IsInfinity(direction.X) || float.IsInfinity(direction.Y) || float.IsInfinity(direction.Z))
        {
            return; // Invalid direction
        }
        
        if (speed <= 0 || float.IsNaN(speed) || float.IsInfinity(speed))
        {
            return; // Invalid speed
        }
        
        if (damage <= 0 || float.IsNaN(damage) || float.IsInfinity(damage))
        {
            return; // Invalid damage
        }
        
        // Find inactive projectile from pool
        Projectile? projectile = null;
        
        // First check active projectiles for reuse
        foreach (var p in projectiles)
        {
            if (!p.IsActive)
            {
                projectile = p;
                break;
            }
        }
        
        // If no inactive projectile found, get from pool or create new
        if (projectile == null)
        {
            if (projectilePool.Count > 0)
            {
                projectile = projectilePool.Dequeue();
                projectiles.Add(projectile);
            }
            else if (projectiles.Count < MAX_PROJECTILES)
            {
                projectile = new Projectile();
                projectiles.Add(projectile);
            }
            else
            {
                // All projectiles in use, find oldest one
                projectile = projectiles.OrderBy(p => p.Lifetime).FirstOrDefault();
            }
        }
        
        // Fire the projectile
        projectile?.Fire(position, direction, speed, damage, onHit);
    }
    
    public void RemoveDeadEnemies()
    {
        enemies.RemoveAll(e => !e.IsAlive && !e.IsActive);
    }
    
    public void CreateObstacle(Vector3 position, Vector3 size, float health = 100f)
    {
        if (obstacles.Count >= MAX_OBSTACLES)
        {
            // Remove first destroyed obstacle
            var destroyed = obstacles.FirstOrDefault(o => o.IsDestroyed);
            if (destroyed != null)
            {
                obstacles.Remove(destroyed);
            }
        }
        
        // Create obstacle with default type (Crate)
        var obstacle = new Obstacle(position, ObstacleType.Crate, 0f);
        obstacles.Add(obstacle);
    }
    
    public Enemy? GetEnemyById(int id)
    {
        return enemies.FirstOrDefault(e => e.Id == id);
    }
    
    public List<Enemy> GetEnemiesInRange(Vector3 position, float range)
    {
        var rangeSqr = range * range;
        return enemies
            .Where(e => e.IsAlive && Vector3.DistanceSquared(e.Position, position) <= rangeSqr)
            .ToList();
    }
    
    public void Update(float deltaTime)
    {
        // Update all active projectiles
        foreach (var projectile in projectiles)
        {
            if (projectile.IsActive)
            {
                projectile.Update(deltaTime);
            }
        }
        
        // Note: Enemy updates should be handled by a separate system that has access to player position
        // This is just for projectile updates which are self-contained
    }
    
    public void UpdateEnemies(float deltaTime, Vector3 playerPosition)
    {
        // Update all living enemies
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                enemy.Update(deltaTime, playerPosition, obstacles);
            }
        }
    }
    
    public void ClearAll()
    {
        // Return projectiles to pool
        foreach (var projectile in projectiles)
        {
            projectile.IsActive = false;
            if (!projectilePool.Contains(projectile))
            {
                projectilePool.Enqueue(projectile);
            }
        }
        
        projectiles.Clear();
        enemies.Clear();
        obstacles.Clear();
    }
    
    public void Reset()
    {
        ClearAll();
        Initialize();
    }
    
    public void Dispose()
    {
        ClearAll();
    }
    
    /// <summary>
    /// Get count of alive enemies
    /// </summary>
    public int GetAliveEnemyCount()
    {
        return enemies.Count(e => e.IsAlive);
    }
    
    /// <summary>
    /// Get count of active projectiles
    /// </summary>
    public int GetActiveProjectileCount()
    {
        return projectiles.Count(p => p.IsActive);
    }
}