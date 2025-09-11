namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Manages enemy wave spawning and difficulty progression
/// </summary>
public interface IWaveManager : IGameSystem
{
    /// <summary>
    /// Current wave number
    /// </summary>
    int CurrentWave { get; }
    
    /// <summary>
    /// Is a wave currently active
    /// </summary>
    bool IsWaveActive { get; }
    
    /// <summary>
    /// Time until next wave
    /// </summary>
    float TimeToNextWave { get; }
    
    /// <summary>
    /// Start the next wave
    /// </summary>
    void StartNextWave();
    
    /// <summary>
    /// Check if current wave is complete
    /// </summary>
    bool IsWaveComplete();
    
    /// <summary>
    /// Force spawn enemies (debug)
    /// </summary>
    void ForceSpawnEnemies(int count);
}