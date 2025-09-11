using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;
using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.UI;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Core;
using FPSRoguelike.Input;
using FPSRoguelike.Physics;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Coordinates all UI systems (ImGui, HUD, Settings)
/// </summary>
public class UICoordinator : IUICoordinator
{
    // UI Systems
    private ImGuiWrapper? imGuiController;
    private ImGuiHUD? imGuiHud;
    private SimpleUIManager? uiManager;
    
    // Dependencies
    private readonly GL gl;
    private readonly IWindow window;
    private readonly InputSystem inputSystem;
    private readonly IGameStateManager stateManager;
    
    // Settings
    private Settings? settings;
    private bool showSettingsMenu = false;
    
    // Properties
    public bool IsSettingsMenuVisible => showSettingsMenu;
    public float MouseSensitivity => uiManager?.MouseSensitivity ?? 0.3f;
    public float FieldOfView => uiManager?.FieldOfView ?? 90f;
    
    public UICoordinator(GL gl, IWindow window, InputSystem inputSystem, 
                        IGameStateManager stateManager)
    {
        this.gl = gl;
        this.window = window;
        this.inputSystem = inputSystem;
        this.stateManager = stateManager;
    }
    
    public void Initialize()
    {
        // Load settings
        settings = Settings.Load();
        
        // Initialize ImGui
        imGuiController = new ImGuiWrapper(gl, window, inputSystem.GetInputContext());
        imGuiController.Initialize();
        
        // Initialize HUD
        imGuiHud = new ImGuiHUD();
        
        // Initialize UI Manager
        uiManager = new SimpleUIManager();
        
        // Apply loaded settings
        if (settings != null)
        {
            if (uiManager != null)
            {
                uiManager.FieldOfView = settings.FieldOfView;
                uiManager.MouseSensitivity = settings.MouseSensitivity;
            }
            
            if (imGuiHud != null)
            {
                // Pass settings reference to ImGuiHUD
                imGuiHud.SetSettings(settings);
                
                if (settings.ShowDebugInfo)
                {
                    imGuiHud.ToggleDebugInfo();
                }
            }
        }
    }
    
    public void Update(float deltaTime)
    {
        // Update ImGui
        imGuiController?.Update(deltaTime);
        imGuiHud?.Update(deltaTime);
        
        // Check if settings menu should close
        if (showSettingsMenu && imGuiHud?.ShouldCloseSettings() == true)
        {
            ToggleSettingsMenu();
        }
    }
    
    public void UpdateHUD(PlayerHealth? health, Weapon? currentWeapon, int score, 
                         int wave, int enemiesAlive, float deltaTime)
    {
        // Pass data to HUD for rendering
        // This is called from the main game loop with current state
    }
    
    public void Render(float deltaTime)
    {
        // Render ImGui elements
        imGuiController?.Render();
    }
    
    public void RenderGameHUD(PlayerHealth? health, Weapon? currentWeapon, int score,
                             int wave, int enemiesAlive, bool isPaused, float deltaTime,
                             float playerYaw)
    {
        imGuiHud?.Render(health, currentWeapon, score, wave, enemiesAlive, 
                        isPaused, deltaTime, playerYaw, showSettingsMenu);
    }
    
    public void ToggleSettingsMenu()
    {
        showSettingsMenu = !showSettingsMenu;
        
        if (!showSettingsMenu)
        {
            // Save settings when closing menu
            if (settings != null && uiManager != null)
            {
                settings.FieldOfView = uiManager.FieldOfView;
                settings.MouseSensitivity = uiManager.MouseSensitivity;
                settings.ShowDebugInfo = imGuiHud?.IsDebugInfoVisible() ?? false;
                settings.Save();
            }
        }
        
        // Update cursor mode
        inputSystem.SetCursorMode(showSettingsMenu ? CursorMode.Normal : CursorMode.Raw);
        
        // Toggle pause state
        if (showSettingsMenu)
        {
            stateManager.ChangeState(GameState.Paused);
        }
        else
        {
            stateManager.ChangeState(GameState.Playing);
        }
    }
    
    public void ToggleDebugInfo()
    {
        imGuiHud?.ToggleDebugInfo();
    }
    
    public void UpdateMinimapEntities(IReadOnlyList<Enemy> enemies, 
                                     CharacterController characterController, 
                                     float cameraYaw)
    {
        imGuiHud?.UpdateMinimapEntities(enemies.ToList(), characterController, cameraYaw);
    }
    
    public void Reset()
    {
        showSettingsMenu = false;
        Initialize();
    }
    
    public void Dispose()
    {
        imGuiController?.Dispose();
        uiManager?.Dispose();
    }
}