using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Systems.Core;
using FPSRoguelike.Input;

namespace FPSRoguelike.Core;

/// <summary>
/// Manages the game update loop, extracted from GameRefactored to reduce God Object
/// </summary>
public class UpdateManager
{
    private readonly IEntityManager entityManager;
    private readonly ICollisionSystem collisionSystem;
    private readonly IWaveManager waveManager;
    private readonly IWeaponSystem weaponSystem;
    private readonly IPlayerSystem playerSystem;
    private readonly IGameStateManager stateManager;
    private readonly ILogger logger;
    
    // Timing
    private double accumulator = 0.0;
    
    public UpdateManager(
        IEntityManager entityManager,
        ICollisionSystem collisionSystem,
        IWaveManager waveManager,
        IWeaponSystem weaponSystem,
        IPlayerSystem playerSystem,
        IGameStateManager stateManager,
        ILogger? logger = null)
    {
        this.entityManager = entityManager;
        this.collisionSystem = collisionSystem;
        this.waveManager = waveManager;
        this.weaponSystem = weaponSystem;
        this.playerSystem = playerSystem;
        this.stateManager = stateManager;
        this.logger = logger ?? new NullLogger();
    }
    
    /// <summary>
    /// Update game with fixed timestep
    /// </summary>
    public void Update(double deltaTime)
    {
        accumulator += deltaTime;
        
        // Fixed timestep update loop
        int updates = 0;
        while (accumulator >= Constants.FIXED_TIMESTEP && updates < Constants.MAX_PHYSICS_UPDATES)
        {
            UpdateGameLogic(Constants.FIXED_TIMESTEP);
            accumulator -= Constants.FIXED_TIMESTEP;
            updates++;
        }
        
        if (updates >= Constants.MAX_PHYSICS_UPDATES)
        {
            logger.LogWarning($"Update loop hit max updates ({Constants.MAX_PHYSICS_UPDATES}), game may be lagging");
        }
    }
    
    /// <summary>
    /// Get interpolation factor for rendering
    /// </summary>
    public double GetInterpolation()
    {
        return accumulator / Constants.FIXED_TIMESTEP;
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        float dt = (float)fixedDeltaTime;
        
        // Update all game systems
        playerSystem?.Update(dt);
        playerSystem?.ProcessInput(dt);
        weaponSystem?.Update(dt);
        waveManager?.Update(dt);
        entityManager?.Update(dt);
        
        // Update enemies with player position
        if (entityManager is EntityManager em && playerSystem != null)
        {
            em.UpdateEnemies(dt, playerSystem.Position);
        }
        
        // Check collisions
        if (collisionSystem != null && entityManager != null)
        {
            collisionSystem.CheckProjectileEnemyCollisions(
                entityManager.Projectiles,
                entityManager.Enemies
            );
        }
        
        // Check for enemy deaths and update score
        CheckEnemyDeaths();
        
        // Check for player damage from enemies
        CheckEnemyMeleeAttacks();
        
        // Check wave completion and victory condition
        CheckWaveStatus();
    }
    
    private void CheckEnemyDeaths()
    {
        if (entityManager == null || playerSystem == null) return;
        
        // Check for enemy deaths and award points
        foreach (var enemy in entityManager.Enemies)
        {
            if (!enemy.IsAlive && enemy.IsActive)
            {
                // Award points
                if (playerSystem is PlayerSystem ps)
                {
                    ps.OnEnemyKilled(enemy);
                }
                
                // Mark enemy as inactive
                enemy.IsActive = false;
            }
        }
        
        // Clean up dead enemies periodically
        entityManager.RemoveDeadEnemies();
    }
    
    private void CheckEnemyMeleeAttacks()
    {
        if (entityManager == null || playerSystem == null || collisionSystem == null) return;
        
        foreach (var enemy in entityManager.Enemies)
        {
            if (!enemy.IsAlive) continue;
            
            // Check if enemy is in melee range
            if (collisionSystem.CheckMeleeRange(enemy.Position, playerSystem.Position, Constants.MELEE_ENEMY_RANGE))
            {
                // Apply damage to player
                playerSystem.TakeDamage(Constants.MELEE_ENEMY_DAMAGE, enemy.Position);
            }
        }
    }
    
    private void CheckWaveStatus()
    {
        if (waveManager == null || entityManager == null || stateManager == null) return;
        
        // Check if wave is complete
        if (entityManager.GetAliveEnemyCount() == 0 && waveManager.CurrentWave > 0)
        {
            // Check for victory (10 waves completed)
            if (waveManager.CurrentWave >= 10)
            {
                stateManager.ChangeState(GameState.Victory);
            }
            else
            {
                // Start next wave after a short delay
                waveManager.StartNextWave();
            }
        }
    }
}