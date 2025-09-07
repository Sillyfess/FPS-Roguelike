using System.Numerics;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;

namespace FPSRoguelike.UI;

public class SimpleUIManager
{
    private bool isPaused = false;
    private bool showDebugInfo = false;
    
    private float mouseSensitivity = 0.3f;
    private float fov = 90f;
    
    public bool IsPaused => isPaused;
    public bool IsMenuOpen => isPaused;
    public float MouseSensitivity => mouseSensitivity;
    public float FieldOfView => fov;
    
    public void Update(double deltaTime)
    {
        // UI update logic handled in game
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        Console.WriteLine(isPaused ? "=== GAME PAUSED ===" : "=== GAME RESUMED ===");
    }
    
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
    }
    
    public void PrintHUD(PlayerHealth? playerHealth, Weapon? weapon, int score, 
                        int waveNumber, int enemiesRemaining, float fps)
    {
        if (!showDebugInfo) return;
        
        Console.SetCursorPosition(0, 0);
        Console.WriteLine($"FPS: {fps:F1} | Health: {(int)(playerHealth?.Health ?? 0)}/{(int)(playerHealth?.MaxHealth ?? 100)} | Score: {score}");
        Console.WriteLine($"Wave: {waveNumber} | Enemies: {enemiesRemaining} | Weapon: {weapon?.Name ?? "None"}");
    }
}