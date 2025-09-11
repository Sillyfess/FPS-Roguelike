using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Systems.Core;
using FPSRoguelike.Entities;

namespace FPSRoguelike.Core;

/// <summary>
/// Manages rendering operations, extracted from GameRefactored to reduce God Object
/// </summary>
public class RenderManager
{
    private readonly IRenderingSystem renderingSystem;
    private readonly IEntityManager entityManager;
    private readonly IWeaponSystem weaponSystem;
    private readonly ILogger logger;
    
    // Pre-allocated arrays for rendering (avoid per-frame allocations)
    private readonly Matrix4x4[] enemyTransformBuffer = new Matrix4x4[Constants.MAX_ENEMIES];
    private readonly Vector3[] enemyColorBuffer = new Vector3[Constants.MAX_ENEMIES];
    private readonly Matrix4x4[] projectileTransformBuffer = new Matrix4x4[Constants.MAX_PROJECTILES];
    private readonly Vector3[] projectileColorBuffer = new Vector3[Constants.MAX_PROJECTILES];
    
    public RenderManager(
        IRenderingSystem renderingSystem,
        IEntityManager entityManager,
        IWeaponSystem weaponSystem,
        ILogger? logger = null)
    {
        this.renderingSystem = renderingSystem;
        this.entityManager = entityManager;
        this.weaponSystem = weaponSystem;
        this.logger = logger ?? new NullLogger();
    }
    
    /// <summary>
    /// Render the game scene with interpolation
    /// </summary>
    public void RenderScene(double interpolation)
    {
        if (renderingSystem == null || entityManager == null) return;
        
        // Render enemies
        RenderEnemies();
        
        // Render projectiles
        RenderProjectiles();
        
        // Render obstacles
        RenderObstacles();
        
        // Render ground plane
        RenderGround();
    }
    
    private void RenderEnemies()
    {
        var enemies = entityManager.Enemies;
        int enemyCount = Math.Min(enemies.Count, Constants.MAX_ENEMIES);
        
        if (enemyCount > 0)
        {
            int validEnemyCount = 0;
            for (int i = 0; i < enemyCount && validEnemyCount < Constants.MAX_ENEMIES; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;
                
                // Create transform matrix for enemy
                float scale = enemy is Boss ? Constants.BOSS_SCALE : Constants.DEFAULT_ENEMY_SCALE;
                enemyTransformBuffer[validEnemyCount] = Matrix4x4.CreateScale(scale) * 
                                    Matrix4x4.CreateTranslation(enemy.Position);
                enemyColorBuffer[validEnemyCount] = enemy.Color;
                validEnemyCount++;
            }
            
            if (validEnemyCount > 0)
            {
                renderingSystem.RenderCubesInstanced(enemyTransformBuffer, enemyColorBuffer, validEnemyCount);
            }
        }
    }
    
    private void RenderProjectiles()
    {
        var projectiles = entityManager.Projectiles;
        
        // Count active projectiles without LINQ
        int activeProjectileCount = 0;
        foreach (var p in projectiles)
        {
            if (p.IsActive) activeProjectileCount++;
        }
        int projectileCount = Math.Min(activeProjectileCount, Constants.MAX_PROJECTILES);
        
        if (projectileCount > 0)
        {
            int index = 0;
            foreach (var proj in projectiles)
            {
                if (!proj.IsActive) continue;
                if (index >= projectileCount || index >= Constants.MAX_PROJECTILES) break;
                
                projectileTransformBuffer[index] = Matrix4x4.CreateScale(Constants.PROJECTILE_SCALE) * 
                                             Matrix4x4.CreateTranslation(proj.Position);
                projectileColorBuffer[index] = new Vector3(
                    Constants.PROJECTILE_COLOR_R, 
                    Constants.PROJECTILE_COLOR_G, 
                    Constants.PROJECTILE_COLOR_B);
                index++;
            }
            
            if (index > 0)
            {
                renderingSystem.RenderCubesInstanced(projectileTransformBuffer, projectileColorBuffer, index);
            }
        }
    }
    
    private void RenderObstacles()
    {
        // Render obstacles one by one (fewer than enemies/projectiles)
        foreach (var obstacle in entityManager.Obstacles)
        {
            if (obstacle.IsDestroyed) continue;
            
            Matrix4x4 transform = Matrix4x4.CreateScale(obstacle.Size) * 
                                 Matrix4x4.CreateTranslation(obstacle.Position);
            
            Vector3 color = new Vector3(
                Constants.OBSTACLE_COLOR_R, 
                Constants.OBSTACLE_COLOR_G, 
                Constants.OBSTACLE_COLOR_B);
            
            renderingSystem.RenderCube(transform, color);
        }
    }
    
    private void RenderGround()
    {
        // Render ground plane
        Matrix4x4 groundTransform = Matrix4x4.CreateScale(
            Constants.GROUND_WIDTH, 
            Constants.GROUND_HEIGHT, 
            Constants.GROUND_DEPTH) * 
            Matrix4x4.CreateTranslation(0, Constants.GROUND_Y_POSITION, 0);
            
        renderingSystem.RenderCube(groundTransform, new Vector3(
            Constants.GROUND_COLOR_R, 
            Constants.GROUND_COLOR_G, 
            Constants.GROUND_COLOR_B));
    }
    
    /// <summary>
    /// Render weapon effects
    /// </summary>
    public void RenderWeaponEffects(Vector3 playerPosition, Matrix4x4 viewMatrix, Matrix4x4 projMatrix)
    {
        if (weaponSystem is WeaponSystem ws)
        {
            ws.RenderEffects(playerPosition, viewMatrix, projMatrix);
        }
    }
}