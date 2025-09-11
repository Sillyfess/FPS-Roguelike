namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// High-level game state management
/// </summary>
public interface IGameStateManager : IGameSystem
{
    /// <summary>
    /// Current game state
    /// </summary>
    GameState CurrentState { get; }
    
    /// <summary>
    /// Is game paused
    /// </summary>
    bool IsPaused { get; }
    
    /// <summary>
    /// Is in editor mode
    /// </summary>
    bool IsEditorMode { get; }
    
    /// <summary>
    /// Change game state
    /// </summary>
    void ChangeState(GameState newState);
    
    /// <summary>
    /// Toggle pause
    /// </summary>
    void TogglePause();
    
    /// <summary>
    /// Toggle editor mode
    /// </summary>
    void ToggleEditor();
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
    GameOver,
    Victory,
    Editor
}