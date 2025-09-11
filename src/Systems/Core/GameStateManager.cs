using FPSRoguelike.Systems.Interfaces;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Manages high-level game state transitions
/// </summary>
public class GameStateManager : IGameStateManager
{
    // State
    private GameState currentState = GameState.Playing;
    private bool isPaused = false;
    private bool isEditorMode = false;
    
    // Events
    public event Action<GameState>? StateChanged;
    
    // Properties
    public GameState CurrentState => currentState;
    public bool IsPaused => isPaused;
    public bool IsEditorMode => isEditorMode;
    
    public void Initialize()
    {
        currentState = GameState.Playing;
        isPaused = false;
        isEditorMode = false;
    }
    
    public void ChangeState(GameState newState)
    {
        if (currentState != newState)
        {
            var oldState = currentState;
            currentState = newState;
            
            // Handle state-specific logic
            switch (newState)
            {
                case GameState.MainMenu:
                    isPaused = true;
                    break;
                    
                case GameState.Playing:
                    isPaused = false;
                    isEditorMode = false;
                    break;
                    
                case GameState.Paused:
                    isPaused = true;
                    break;
                    
                case GameState.GameOver:
                    isPaused = true;
                    break;
                    
                case GameState.Victory:
                    isPaused = true;
                    break;
                    
                case GameState.Editor:
                    isPaused = true;
                    isEditorMode = true;
                    break;
            }
            
            StateChanged?.Invoke(newState);
        }
    }
    
    public void TogglePause()
    {
        if (currentState == GameState.Playing)
        {
            ChangeState(GameState.Paused);
        }
        else if (currentState == GameState.Paused)
        {
            ChangeState(GameState.Playing);
        }
    }
    
    public void ToggleEditor()
    {
        if (isEditorMode)
        {
            ChangeState(GameState.Playing);
        }
        else
        {
            ChangeState(GameState.Editor);
        }
    }
    
    public void Update(float deltaTime)
    {
        // State manager doesn't need per-frame updates
        // State changes are event-driven
    }
    
    public void Reset()
    {
        Initialize();
    }
    
    public void Dispose()
    {
        // No resources to dispose
    }
}