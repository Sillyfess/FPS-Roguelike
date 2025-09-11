namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Base interface for all game systems. Defines lifecycle methods.
/// </summary>
public interface IGameSystem : IDisposable
{
    /// <summary>
    /// Initialize the system with required dependencies
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Update system logic with fixed timestep
    /// </summary>
    void Update(float deltaTime);
    
    /// <summary>
    /// Called when system should reset to initial state
    /// </summary>
    void Reset();
}