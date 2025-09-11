using System.Numerics;
using FPSRoguelike.Systems.Interfaces;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Manages enemy wave spawning and difficulty progression
/// </summary>
public class WaveManager : IWaveManager
{
    // Dependencies
    private readonly IEntityManager entityManager;
    
    // Wave constants
    private const float WAVE_DELAY = 3f;
    private const float SPAWN_RADIUS = 15f;
    private const float MIN_SPAWN_DISTANCE = 10f;
    private const float BASE_ENEMY_HEALTH = 30f;
    private const float BOSS_HEALTH = 500f;
    
    // Wave state
    private int currentWave = 0;
    private float waveTimer = 0f;
    private bool waveActive = false;
    private int enemiesSpawnedThisWave = 0;
    
    // Properties
    public int CurrentWave => currentWave;
    public bool IsWaveActive => waveActive;
    public float TimeToNextWave => Math.Max(0, WAVE_DELAY - waveTimer);
    
    public WaveManager(IEntityManager entityManager)
    {
        this.entityManager = entityManager;
    }
    
    public void Initialize()
    {
        currentWave = 0;
        waveTimer = 0f;
        waveActive = false;
    }
    
    public void Update(float deltaTime)
    {
        if (!waveActive && entityManager.GetAliveEnemyCount() == 0)
        {
            waveTimer += deltaTime;
            
            if (waveTimer >= WAVE_DELAY)
            {
                StartNextWave();
            }
        }
    }
    
    public void StartNextWave()
    {
        currentWave++;
        waveActive = true;
        waveTimer = 0f;
        enemiesSpawnedThisWave = 0;
        
        // Spawn enemies based on wave number
        if (currentWave % 5 == 0)
        {
            // Boss wave every 5 waves
            SpawnBoss();
        }
        else
        {
            // Regular wave
            int enemyCount = Math.Min(5 + currentWave * 2, 20); // Cap at 20 enemies
            SpawnEnemies(enemyCount);
        }
    }
    
    public bool IsWaveComplete()
    {
        return !waveActive && entityManager.GetAliveEnemyCount() == 0;
    }
    
    public void ForceSpawnEnemies(int count)
    {
        SpawnEnemies(count);
    }
    
    private void SpawnEnemies(int count)
    {
        Random random = new Random();
        
        for (int i = 0; i < count; i++)
        {
            // Generate random spawn position around origin
            float angle = (float)(random.NextDouble() * Math.PI * 2);
            float distance = MIN_SPAWN_DISTANCE + (float)(random.NextDouble() * (SPAWN_RADIUS - MIN_SPAWN_DISTANCE));
            
            Vector3 spawnPos = new Vector3(
                MathF.Cos(angle) * distance,
                1f, // Ground level + half enemy height
                MathF.Sin(angle) * distance
            );
            
            // Scale health with wave number
            float health = BASE_ENEMY_HEALTH + (currentWave - 1) * 10;
            
            entityManager.SpawnEnemy(spawnPos, health, false);
            enemiesSpawnedThisWave++;
        }
    }
    
    private void SpawnBoss()
    {
        // Spawn boss at a specific location
        Vector3 bossSpawnPos = new Vector3(0, 2f, 20f);
        
        entityManager.SpawnEnemy(bossSpawnPos, BOSS_HEALTH, true);
        enemiesSpawnedThisWave = 1;
    }
    
    public void Reset()
    {
        currentWave = 0;
        waveTimer = 0f;
        waveActive = false;
        enemiesSpawnedThisWave = 0;
    }
    
    public void Dispose()
    {
        // No resources to dispose
    }
}