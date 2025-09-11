using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Handles all collision detection and physics queries
/// </summary>
public class CollisionSystem : ICollisionSystem
{
    // Constants
    private const float PROJECTILE_RADIUS = 0.1f;
    private const float ENEMY_RADIUS = 0.5f;
    private const float BOSS_RADIUS = 1.5f;
    
    public void Initialize()
    {
        // No initialization needed for basic collision system
        // Future: Could initialize spatial partitioning structures here
    }
    
    public void CheckProjectileEnemyCollisions(IReadOnlyList<Projectile> projectiles, 
                                              IReadOnlyList<Enemy> enemies)
    {
        // O(n*m) collision detection - should be optimized with spatial partitioning
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;
            
            foreach (var enemy in enemies)
            {
                if (!enemy.IsAlive) continue;
                
                // Check sphere-sphere collision
                float enemyRadius = enemy is Boss ? BOSS_RADIUS : ENEMY_RADIUS;
                float distanceSqr = Vector3.DistanceSquared(projectile.Position, enemy.Position);
                float radiusSum = PROJECTILE_RADIUS + enemyRadius;
                
                if (distanceSqr <= radiusSum * radiusSum)
                {
                    // Hit detected
                    enemy.TakeDamage(projectile.Damage);
                    
                    // Call hit callback if provided
                    projectile.OnHit?.Invoke(enemy);
                    
                    // Deactivate projectile
                    projectile.IsActive = false;
                    break; // This projectile is done
                }
            }
        }
    }
    
    public bool CheckObstacleCollision(Vector3 position, float radius, 
                                      IReadOnlyList<Obstacle> obstacles)
    {
        foreach (var obstacle in obstacles)
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
        
        // Normalize direction
        direction = Vector3.Normalize(direction);
        
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
        // No resources to dispose
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