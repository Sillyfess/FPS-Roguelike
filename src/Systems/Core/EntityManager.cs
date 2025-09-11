using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;
using FPSRoguelike.Core;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Unified management of all game entities
/// </summary>
public class EntityManager : IEntityManager
{
    // Constants are in Core.Constants
    
    // Thread safety
    private readonly object enemyLock = new();
    private readonly object projectileLock = new();
    private readonly object obstacleLock = new();
    
    // Entity lists
    private readonly List<Enemy> enemies = new();
    private readonly List<Projectile> projectiles = new();
    private readonly List<Obstacle> obstacles = new();
    
    // Cached read-only lists to avoid allocations
    private List<Enemy>? cachedEnemiesList;
    private List<Obstacle>? cachedObstaclesList;
    private bool enemiesListDirty = true;
    private bool obstaclesListDirty = true;
    
    // Object pool - all projectiles are pre-allocated and reused
    private readonly Projectile[] allProjectiles = new Projectile[Constants.MAX_PROJECTILES];
    private int nextProjectileIndex = 0; // Round-robin allocation
    
    // Public accessors - return cached copies to avoid per-frame allocations
    public IReadOnlyList<Enemy> Enemies
    {
        get
        {
            lock (enemyLock)
            {
                if (enemiesListDirty || cachedEnemiesList == null)
                {
                    cachedEnemiesList = enemies.ToList();
                    enemiesListDirty = false;
                }
                return cachedEnemiesList.AsReadOnly();
            }
        }
    }
    
    public IReadOnlyList<Projectile> Projectiles
    {
        get
        {
            lock (projectileLock)
            {
                // For projectiles, we return the actual list as read-only since it's pre-allocated
                return projectiles.AsReadOnly();
            }
        }
    }
    
    public IReadOnlyList<Obstacle> Obstacles
    {
        get
        {
            lock (obstacleLock)
            {
                if (obstaclesListDirty || cachedObstaclesList == null)
                {
                    cachedObstaclesList = obstacles.ToList();
                    obstaclesListDirty = false;
                }
                return cachedObstaclesList.AsReadOnly();
            }
        }
    }
    
    public void Initialize()
    {
        lock (projectileLock)
        {
            // Pre-allocate all projectiles
            for (int i = 0; i < Constants.MAX_PROJECTILES; i++)
            {
                allProjectiles[i] = new Projectile();
                projectiles.Add(allProjectiles[i]);
            }
            nextProjectileIndex = 0;
        }
    }
    
    public Enemy SpawnEnemy(Vector3 position, float health, bool isBoss = false)
    {
        lock (enemyLock)
        {
            if (enemies.Count >= Constants.MAX_ENEMIES)
            {
                // First try to find a dead enemy to replace
                var deadEnemy = enemies.FirstOrDefault(e => !e.IsAlive);
                if (deadEnemy != null)
                {
                    enemies.Remove(deadEnemy);
                }
                else
                {
                    // All enemies are alive - remove the oldest one
                    // This ensures the game can always spawn new enemies
                    if (enemies.Count > 0)
                    {
                        enemies.RemoveAt(0); // Remove first (oldest) enemy
                    }
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
            enemiesListDirty = true;
            return enemy;
        }
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
        
        lock (projectileLock)
        {
            // Use round-robin allocation for predictable behavior
            Projectile? projectile = null;
            int attempts = 0;
            
            // Try to find an inactive projectile, starting from nextProjectileIndex
            while (attempts < Constants.MAX_PROJECTILES)
            {
                var candidate = allProjectiles[nextProjectileIndex];
                if (!candidate.IsActive)
                {
                    projectile = candidate;
                    nextProjectileIndex = (nextProjectileIndex + 1) % Constants.MAX_PROJECTILES;
                    break;
                }
                
                nextProjectileIndex = (nextProjectileIndex + 1) % Constants.MAX_PROJECTILES;
                attempts++;
            }
            
            // If all projectiles are active, reuse the oldest one
            if (projectile == null)
            {
                float maxLifetime = float.MinValue;
                
                for (int i = 0; i < Constants.MAX_PROJECTILES; i++)
                {
                    var p = allProjectiles[i];
                    if (p.IsActive && p.Lifetime > maxLifetime)
                    {
                        maxLifetime = p.Lifetime;
                        projectile = p;
                    }
                }
            }
            
            // Fire the projectile
            projectile?.Fire(position, direction, speed, damage, onHit);
        }
    }
    
    public void RemoveDeadEnemies()
    {
        lock (enemyLock)
        {
            int removed = enemies.RemoveAll(e => !e.IsAlive && !e.IsActive);
            if (removed > 0)
            {
                enemiesListDirty = true;
            }
        }
    }
    
    public void CreateObstacle(Vector3 position, Vector3 size, float health = 100f)
    {
        lock (obstacleLock)
        {
            if (obstacles.Count >= Constants.MAX_OBSTACLES)
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
            obstaclesListDirty = true;
        }
    }
    
    public Enemy? GetEnemyById(int id)
    {
        lock (enemyLock)
        {
            return enemies.FirstOrDefault(e => e.Id == id);
        }
    }
    
    public List<Enemy> GetEnemiesInRange(Vector3 position, float range)
    {
        lock (enemyLock)
        {
            var rangeSqr = range * range;
            return enemies
                .Where(e => e.IsAlive && Vector3.DistanceSquared(e.Position, position) <= rangeSqr)
                .ToList();
        }
    }
    
    public List<Enemy> GetEnemiesInArc(Vector3 position, Vector3 forward, float range, float arcAngle)
    {
        lock (enemyLock)
        {
            var result = new List<Enemy>();
            var rangeSqr = range * range;
            
            // Normalize forward vector if not already
            if (Math.Abs(forward.LengthSquared() - 1.0f) > Constants.VECTOR_NORMALIZATION_EPSILON)
            {
                forward = Vector3.Normalize(forward);
            }
            
            // Convert arc angles to radians and calculate dot product thresholds
            float halfHorizontalArcRad = arcAngle * 0.5f * Constants.FOV_TO_RADIANS;
            float horizontalDotThreshold = MathF.Cos(halfHorizontalArcRad);
            
            // For vertical arc, use a fixed angle (e.g., 60 degrees total)
            float halfVerticalArcRad = Constants.KATANA_VERTICAL_ARC * 0.5f * Constants.FOV_TO_RADIANS;
            float verticalDotThreshold = MathF.Cos(halfVerticalArcRad);
            
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                
                // Check if enemy is within range
                Vector3 toEnemy = enemy.Position - position;
                float distSqr = toEnemy.LengthSquared();
                if (distSqr > rangeSqr || distSqr < Constants.POSITION_EPSILON) continue; // Too far or too close
                
                // Normalize direction to enemy
                Vector3 dirToEnemy = Vector3.Normalize(toEnemy);
                
                // Check horizontal arc (XZ plane)
                Vector3 forwardXZ = new Vector3(forward.X, 0, forward.Z);
                Vector3 dirToEnemyXZ = new Vector3(dirToEnemy.X, 0, dirToEnemy.Z);
                
                // Normalize XZ vectors if they're not zero
                if (forwardXZ.LengthSquared() > Constants.POSITION_EPSILON)
                    forwardXZ = Vector3.Normalize(forwardXZ);
                if (dirToEnemyXZ.LengthSquared() > Constants.POSITION_EPSILON)
                    dirToEnemyXZ = Vector3.Normalize(dirToEnemyXZ);
                
                float horizontalDot = Vector3.Dot(forwardXZ, dirToEnemyXZ);
                
                // Check vertical arc (Y component)
                float verticalAngle = MathF.Abs(MathF.Asin(dirToEnemy.Y));
                float maxVerticalAngle = halfVerticalArcRad;
                
                // Enemy must be within both horizontal and vertical arcs
                if (horizontalDot >= horizontalDotThreshold && verticalAngle <= maxVerticalAngle)
                {
                    result.Add(enemy);
                }
            }
            
            return result;
        }
    }
    
    public void Update(float deltaTime)
    {
        lock (projectileLock)
        {
            // Update all active projectiles
            foreach (var projectile in projectiles)
            {
                if (projectile.IsActive)
                {
                    projectile.Update(deltaTime);
                }
            }
        }
        
        // Note: Enemy updates should be handled by a separate system that has access to player position
        // This is just for projectile updates which are self-contained
    }
    
    public void UpdateEnemies(float deltaTime, Vector3 playerPosition)
    {
        List<Enemy> enemiesCopy;
        lock (enemyLock)
        {
            enemiesCopy = enemies.ToList();
        }
        
        List<Obstacle> obstaclesCopy;
        lock (obstacleLock)
        {
            obstaclesCopy = obstacles.ToList();
        }
        
        // Update all living enemies
        foreach (var enemy in enemiesCopy)
        {
            if (enemy.IsAlive)
            {
                enemy.Update(deltaTime, playerPosition, obstaclesCopy);
            }
        }
    }
    
    public void ClearAll()
    {
        lock (projectileLock)
        {
            lock (enemyLock)
            {
                lock (obstacleLock)
                {
                    // Deactivate all projectiles (they remain in the array for reuse)
                    for (int i = 0; i < Constants.MAX_PROJECTILES; i++)
                    {
                        if (allProjectiles[i] != null)
                        {
                            allProjectiles[i].IsActive = false;
                        }
                    }
                    
                    nextProjectileIndex = 0;
                    enemies.Clear();
                    obstacles.Clear();
                    enemiesListDirty = true;
                    obstaclesListDirty = true;
                }
            }
        }
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
        lock (enemyLock)
        {
            int count = 0;
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive) count++;
            }
            return count;
        }
    }
    
    /// <summary>
    /// Get count of active projectiles
    /// </summary>
    public int GetActiveProjectileCount()
    {
        lock (projectileLock)
        {
            int count = 0;
            foreach (var projectile in projectiles)
            {
                if (projectile.IsActive) count++;
            }
            return count;
        }
    }
}