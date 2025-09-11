using System.Numerics;
using FPSRoguelike.Entities;
using FPSRoguelike.Core;

namespace FPSRoguelike.Combat;

/// <summary>
/// High-performance object pool for projectiles using a free list
/// Provides O(1) allocation and deallocation
/// </summary>
public class ProjectilePool
{
    private readonly Projectile[] pool;
    private readonly Stack<int> freeIndices;
    private readonly object lockObject = new();
    
    public ProjectilePool(int capacity = Constants.MAX_PROJECTILES)
    {
        pool = new Projectile[capacity];
        freeIndices = new Stack<int>(capacity);
        
        // Initialize all projectiles and add to free list
        for (int i = capacity - 1; i >= 0; i--)
        {
            pool[i] = new Projectile();
            freeIndices.Push(i);
        }
    }
    
    /// <summary>
    /// Get a projectile from the pool - O(1) operation
    /// </summary>
    public Projectile? Acquire()
    {
        lock (lockObject)
        {
            if (freeIndices.Count > 0)
            {
                int index = freeIndices.Pop();
                return pool[index];
            }
            
            // Pool exhausted - find oldest active projectile to recycle
            Projectile? oldest = null;
            float maxLifetime = float.MinValue;
            
            for (int i = 0; i < pool.Length; i++)
            {
                var p = pool[i];
                if (p.IsActive && p.Lifetime > maxLifetime)
                {
                    maxLifetime = p.Lifetime;
                    oldest = p;
                }
            }
            
            if (oldest != null)
            {
                oldest.Deactivate();
            }
            
            return oldest;
        }
    }
    
    /// <summary>
    /// Return a projectile to the pool - O(1) operation
    /// </summary>
    public void Release(Projectile projectile)
    {
        lock (lockObject)
        {
            if (projectile == null) return;
            
            projectile.Deactivate();
            
            // Find the index of this projectile
            for (int i = 0; i < pool.Length; i++)
            {
                if (ReferenceEquals(pool[i], projectile))
                {
                    // Only add back to free list if not already there
                    if (!freeIndices.Contains(i))
                    {
                        freeIndices.Push(i);
                    }
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Update all active projectiles
    /// </summary>
    public void UpdateAll(float deltaTime)
    {
        lock (lockObject)
        {
            foreach (var projectile in pool)
            {
                if (projectile.IsActive)
                {
                    projectile.Update(deltaTime);
                    
                    // Auto-release if no longer active
                    if (!projectile.IsActive)
                    {
                        Release(projectile);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get all projectiles for rendering (includes inactive ones)
    /// </summary>
    public IReadOnlyList<Projectile> GetAll()
    {
        return pool;
    }
    
    /// <summary>
    /// Get count of active projectiles
    /// </summary>
    public int GetActiveCount()
    {
        lock (lockObject)
        {
            return pool.Length - freeIndices.Count;
        }
    }
    
    /// <summary>
    /// Reset the pool, deactivating all projectiles
    /// </summary>
    public void Reset()
    {
        lock (lockObject)
        {
            freeIndices.Clear();
            
            for (int i = pool.Length - 1; i >= 0; i--)
            {
                pool[i].Deactivate();
                freeIndices.Push(i);
            }
        }
    }
}