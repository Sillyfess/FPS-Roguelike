using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;
using FPSRoguelike.Physics;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Handles all collision detection and physics queries
/// Uses spatial hash grid for O(n) average case performance
/// </summary>
public class CollisionSystem : ICollisionSystem
{
    // Constants
    private const float PROJECTILE_RADIUS = 0.1f;
    private const float ENEMY_RADIUS = 0.5f;
    private const float BOSS_RADIUS = 1.5f;
    private const float SPATIAL_GRID_CELL_SIZE = 5.0f; // Tuned for typical enemy spacing
    
    // Spatial partitioning for efficient collision detection
    private readonly SpatialHashGrid<Projectile> projectileGrid;
    private readonly SpatialHashGrid<Enemy> enemyGrid;
    private readonly SpatialHashGrid<Obstacle> obstacleGrid;
    
    // Performance tracking
    private int lastFrameChecks = 0;
    private int totalChecksWithoutGrid = 0;
    
    public CollisionSystem()
    {
        projectileGrid = new SpatialHashGrid<Projectile>(SPATIAL_GRID_CELL_SIZE);
        enemyGrid = new SpatialHashGrid<Enemy>(SPATIAL_GRID_CELL_SIZE);
        obstacleGrid = new SpatialHashGrid<Obstacle>(SPATIAL_GRID_CELL_SIZE * 2); // Larger cells for static obstacles
    }
    
    public void Initialize()
    {
        // Clear grids on initialization
        projectileGrid.Clear();
        enemyGrid.Clear();
        obstacleGrid.Clear();
    }
    
    public void CheckProjectileEnemyCollisions(IReadOnlyList<Projectile> projectiles, 
                                              IReadOnlyList<Enemy> enemies)
    {
        // Track performance improvement
        totalChecksWithoutGrid = projectiles.Count * enemies.Count;
        lastFrameChecks = 0;
        
        // Build spatial grids for this frame
        projectileGrid.Clear();
        enemyGrid.Clear();
        
        // Insert active projectiles into grid
        foreach (var projectile in projectiles)
        {
            if (projectile.IsActive)
            {
                projectileGrid.Insert(projectile, projectile.Position, PROJECTILE_RADIUS);
            }
        }
        
        // Insert alive enemies into grid
        foreach (var enemy in enemies)
        {
            if (enemy.IsAlive)
            {
                float radius = enemy is Boss ? BOSS_RADIUS : ENEMY_RADIUS;
                enemyGrid.Insert(enemy, enemy.Position, radius);
            }
        }
        
        // Now check collisions using spatial queries - O(n) average case!
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;
            
            // Query for nearby enemies (only checks ~3-5 enemies instead of all 30!)
            var nearbyEnemies = enemyGrid.Query(projectile.Position, PROJECTILE_RADIUS);
            lastFrameChecks += nearbyEnemies.Count;
            
            foreach (var enemy in nearbyEnemies)
            {
                if (!enemy.IsAlive) continue;
                
                // Precise sphere-sphere collision check
                float enemyRadius = enemy is Boss ? BOSS_RADIUS : ENEMY_RADIUS;
                float distanceSqr = Vector3.DistanceSquared(projectile.Position, enemy.Position);
                float radiusSum = PROJECTILE_RADIUS + enemyRadius;
                
                if (distanceSqr <= radiusSum * radiusSum)
                {
                    // Hit detected
                    enemy.TakeDamage(projectile.Damage);
                    
                    // Call hit callback if provided (safe invocation)
                    var callback = projectile.OnHit;
                    callback?.Invoke(enemy);
                    
                    // Deactivate projectile
                    projectile.IsActive = false;
                    break; // This projectile is done
                }
            }
        }
        
        // Performance tracking available through GetPerformanceStats() method
        // Removed console logging from hot path
    }
    
    // Track obstacle grid version to detect changes
    private int obstacleGridVersion = -1;
    private int lastObstacleCount = -1;
    
    public bool CheckObstacleCollision(Vector3 position, float radius, 
                                      IReadOnlyList<Obstacle> obstacles)
    {
        // Rebuild obstacle grid if count changes or version mismatch
        // Note: This is still imperfect but better than just count comparison
        bool needsRebuild = obstacleGrid.EntityCount != obstacles.Count || 
                           lastObstacleCount != obstacles.Count;
        
        if (needsRebuild)
        {
            obstacleGrid.Clear();
            lastObstacleCount = obstacles.Count;
            obstacleGridVersion++;
            
            foreach (var obstacle in obstacles)
            {
                if (!obstacle.IsDestroyed)
                {
                    // Approximate obstacle radius from size
                    float obstacleRadius = Math.Max(obstacle.Size.X, Math.Max(obstacle.Size.Y, obstacle.Size.Z)) * 0.5f;
                    obstacleGrid.Insert(obstacle, obstacle.Position, obstacleRadius);
                }
            }
        }
        
        // Query spatial grid for nearby obstacles
        var nearbyObstacles = obstacleGrid.Query(position, radius);
        
        foreach (var obstacle in nearbyObstacles)
        {
            if (obstacle.IsDestroyed) continue;
            
            if (obstacle.CheckCollision(position, radius))
            {
                return true;
            }
        }
        
        return false;
    }
    
    public bool CheckMeleeRange(Vector3 enemyPos, Vector3 playerPos, float range)
    {
        float distanceSqr = Vector3.DistanceSquared(enemyPos, playerPos);
        return distanceSqr <= range * range;
    }
    
    public Enemy? RaycastEnemies(Vector3 origin, Vector3 direction, float maxDistance,
                                IReadOnlyList<Enemy> enemies)
    {
        Enemy? closestEnemy = null;
        float closestDistance = maxDistance;
        
        // Normalize direction (handle zero vector case)
        float lengthSq = direction.LengthSquared();
        if (lengthSq < 0.0001f) // Nearly zero vector
        {
            return null; // Can't raycast with zero direction
        }
        direction = direction / MathF.Sqrt(lengthSq);
        
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            
            // Ray-sphere intersection
            Vector3 toEnemy = enemy.Position - origin;
            float projectedDistance = Vector3.Dot(toEnemy, direction);
            
            // Enemy is behind the ray origin
            if (projectedDistance < 0) continue;
            
            // Check if projected point is within max distance
            if (projectedDistance > closestDistance) continue;
            
            // Find closest point on ray to enemy center
            Vector3 closestPoint = origin + direction * projectedDistance;
            float distanceToEnemy = Vector3.Distance(closestPoint, enemy.Position);
            
            float enemyRadius = enemy is Boss ? BOSS_RADIUS : ENEMY_RADIUS;
            
            // Check if ray passes through enemy sphere
            if (distanceToEnemy <= enemyRadius)
            {
                closestDistance = projectedDistance;
                closestEnemy = enemy;
            }
        }
        
        return closestEnemy;
    }
    
    public Enemy? GetNearestEnemy(Vector3 position, IReadOnlyList<Enemy> enemies)
    {
        Enemy? nearest = null;
        float nearestDistSqr = float.MaxValue;
        
        foreach (var enemy in enemies)
        {
            if (!enemy.IsAlive) continue;
            
            float distSqr = Vector3.DistanceSquared(position, enemy.Position);
            if (distSqr < nearestDistSqr)
            {
                nearestDistSqr = distSqr;
                nearest = enemy;
            }
        }
        
        return nearest;
    }
    
    public void Update(float deltaTime)
    {
        // Collision system doesn't need per-frame updates
        // Collisions are checked on-demand
    }
    
    public void Reset()
    {
        // No state to reset
    }
    
    public void Dispose()
    {
        // Clear spatial grids
        projectileGrid?.Clear();
        enemyGrid?.Clear();
        obstacleGrid?.Clear();
    }
    
    /// <summary>
    /// Get performance statistics for the collision system
    /// </summary>
    public string GetPerformanceStats()
    {
        float reduction = 0;
        if (totalChecksWithoutGrid > 0)
        {
            reduction = (float)(totalChecksWithoutGrid - lastFrameChecks) / totalChecksWithoutGrid * 100;
        }
        
        return $"Collision Performance: {lastFrameChecks}/{totalChecksWithoutGrid} checks ({reduction:F1}% reduction)\n" +
               $"Projectile Grid: {projectileGrid.GetDebugStats()}\n" +
               $"Enemy Grid: {enemyGrid.GetDebugStats()}\n" +
               $"Obstacle Grid: {obstacleGrid.GetDebugStats()}";
    }
    
    /// <summary>
    /// Check if a sphere collides with an axis-aligned bounding box
    /// </summary>
    private bool SphereAABBCollision(Vector3 spherePos, float radius, 
                                    Vector3 boxMin, Vector3 boxMax)
    {
        // Find closest point on AABB to sphere center
        float x = Math.Max(boxMin.X, Math.Min(spherePos.X, boxMax.X));
        float y = Math.Max(boxMin.Y, Math.Min(spherePos.Y, boxMax.Y));
        float z = Math.Max(boxMin.Z, Math.Min(spherePos.Z, boxMax.Z));
        
        Vector3 closestPoint = new Vector3(x, y, z);
        
        // Check if closest point is within sphere radius
        float distanceSqr = Vector3.DistanceSquared(closestPoint, spherePos);
        return distanceSqr <= radius * radius;
    }
}