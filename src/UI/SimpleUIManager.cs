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
        // Status now shown in HUD instead of console
    }
    
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
    }
    
    public void PrintHUD(PlayerHealth? playerHealth, Weapon? weapon, int score, 
                        int waveNumber, int enemiesRemaining, float fps)
    {
        // HUD is now rendered by HUD.cs using OpenGL
        // This method kept for compatibility but no longer outputs to console
    }
}