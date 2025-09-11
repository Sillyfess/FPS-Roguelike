using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Numerics;
using FPSRoguelike.Input;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Systems.Core;
using FPSRoguelike.Editor;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Core;

/// <summary>
/// Main game coordinator - orchestrates all game systems following SOLID principles
/// </summary>
public class GameRefactored : IDisposable
{
    // Core dependencies
    private readonly IWindow window;
    private readonly GL gl;
    
    // Game systems
    private InputSystem? inputSystem;
    private IRenderingSystem? renderingSystem;
    private IEntityManager? entityManager;
    private ICollisionSystem? collisionSystem;
    private IWaveManager? waveManager;
    private IWeaponSystem? weaponSystem;
    private IPlayerSystem? playerSystem;
    private IGameStateManager? stateManager;
    private IUICoordinator? uiCoordinator;
    
    // Level Editor (kept separate as it's a special mode)
    private LevelEditor? levelEditor;
    
    // Timing
    private double accumulator = 0.0;
    private const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const int MAX_UPDATES = 10;
    
    // Performance tracking
    private double fps = 0;
    private double fpsTimer = 0;
    private int frameCount = 0;
    
    public GameRefactored(IWindow window, GL gl)
    {
        this.window = window;
        this.gl = gl;
    }
    
    public void Initialize()
    {
        // Initialize input system first (needed by other systems)
        inputSystem = new InputSystem();
        inputSystem.Initialize(window);
        
        // Create all game systems with proper dependency injection
        entityManager = new EntityManager();
        collisionSystem = new CollisionSystem();
        stateManager = new GameStateManager();
        
        // Create systems with proper dependency injection (no circular dependencies)
        weaponSystem = new WeaponSystem(entityManager, collisionSystem, gl);
        playerSystem = new PlayerSystem(inputSystem, entityManager, weaponSystem);
        
        waveManager = new WaveManager(entityManager);
        renderingSystem = new RenderingSystem();
        uiCoordinator = new UICoordinator(gl, window, inputSystem, stateManager);
        
        // Initialize all systems
        entityManager.Initialize();
        collisionSystem.Initialize();
        stateManager.Initialize();
        weaponSystem.Initialize();
        waveManager.Initialize();
        playerSystem.Initialize();
        renderingSystem.InitializeGraphics(gl, window.Size.X, window.Size.Y);
        uiCoordinator.Initialize();
        
        // Create initial level obstacles
        GenerateInitialObstacles();
        
        // Initialize level editor
        levelEditor = new LevelEditor(gl);
        
        // Start first wave
        waveManager.StartNextWave();
        
        // Lock cursor for FPS
        inputSystem.SetCursorMode(CursorMode.Raw);
    }
    
    public void Update(double deltaTime)
    {
        // Update FPS counter
        UpdateFPSCounter(deltaTime);
        
        // Handle global input (ESC for settings, F-keys for debug)
        HandleGlobalInput();
        
        // Update UI systems
        uiCoordinator?.Update((float)deltaTime);
        
        // Update based on game state
        switch (stateManager?.CurrentState)
        {
            case GameState.Playing:
                UpdatePlaying(deltaTime);
                break;
                
            case GameState.Editor:
                UpdateEditor(deltaTime);
                break;
                
            case GameState.Paused:
            case GameState.MainMenu:
            case GameState.GameOver:
            case GameState.Victory:
                // These states don't update game logic
                break;
        }
        
        // Poll input at end of frame
        inputSystem?.Poll();
    }
    
    private void UpdatePlaying(double deltaTime)
    {
        accumulator += deltaTime;
        
        // Handle mouse look every frame (not in fixed timestep)
        if (inputSystem != null && !uiCoordinator!.IsSettingsMenuVisible && playerSystem is PlayerSystem ps)
        {
            Vector2 mouseDelta = inputSystem.GetMouseDelta();
            float sensitivity = uiCoordinator.MouseSensitivity;
            ps.UpdateCameraRotation(mouseDelta, sensitivity);
        }
        
        // Fixed timestep update loop
        int updates = 0;
        while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES)
        {
            UpdateGameLogic(FIXED_TIMESTEP);
            accumulator -= FIXED_TIMESTEP;
            updates++;
        }
    }
    
    private void UpdateEditor(double deltaTime)
    {
        if (levelEditor != null && inputSystem != null && playerSystem is PlayerSystem ps)
        {
            // LevelEditor.Update takes InputSystem parameter
            levelEditor.Update((float)deltaTime, inputSystem);
        }
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
        collisionSystem?.CheckProjectileEnemyCollisions(
            entityManager?.Projectiles ?? Array.Empty<Projectile>().ToList().AsReadOnly(),
            entityManager?.Enemies ?? new List<Enemy>()
        );
        
        // Check for enemy deaths and update score
        CheckEnemyDeaths();
        
        // Check for player damage from enemies
        CheckEnemyMeleeAttacks();
        
        // Check wave completion and victory condition
        CheckWaveStatus();
    }
    
    public void Render(double deltaTime)
    {
        // Begin frame
        renderingSystem?.BeginFrame();
        
        double interpolation = accumulator / FIXED_TIMESTEP;
        
        // Set camera matrices for rendering
        if (playerSystem is PlayerSystem ps)
        {
            var camera = ps.Camera;
            float fov = uiCoordinator?.FieldOfView ?? 90f;
            float aspect = renderingSystem?.GetAspectRatio() ?? 16f/9f;
            
            Matrix4x4 view = camera.GetViewMatrix();
            Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
                fov * MathF.PI / 180f, aspect, 0.1f, 1000f);
            
            renderingSystem?.SetCamera(view, projection);
        }
        
        // Render based on state
        switch (stateManager?.CurrentState)
        {
            case GameState.Editor:
                RenderEditorView();
                break;
                
            default:
                RenderGameScene(interpolation, deltaTime);
                break;
        }
        
        // Render UI
        if (uiCoordinator is UICoordinator uiCoord && playerSystem is PlayerSystem playerSys)
        {
            uiCoord.RenderGameHUD(
                playerSys.Health,
                weaponSystem?.CurrentWeapon,
                playerSys.Score,
                waveManager?.CurrentWave ?? 0,
                entityManager?.GetAliveEnemyCount() ?? 0,
                stateManager?.IsPaused ?? false,
                (float)deltaTime,
                playerSys.Camera.Yaw
            );
        }
        
        uiCoordinator?.Render((float)deltaTime);
        
        // End frame
        renderingSystem?.EndFrame();
    }
    
    private void RenderGameScene(double interpolation, double deltaTime)
    {
        if (renderingSystem == null || entityManager == null) return;
        
        // Prepare instance data for enemies
        var enemies = entityManager.Enemies;
        int enemyCount = Math.Min(enemies.Count, 30);
        
        if (enemyCount > 0)
        {
            Matrix4x4[] enemyTransforms = new Matrix4x4[enemyCount];
            Vector3[] enemyColors = new Vector3[enemyCount];
            
            for (int i = 0; i < enemyCount; i++)
            {
                var enemy = enemies[i];
                if (!enemy.IsAlive) continue;
                
                // Create transform matrix for enemy
                float scale = enemy is Boss ? 3f : 1f;
                enemyTransforms[i] = Matrix4x4.CreateScale(scale) * 
                                    Matrix4x4.CreateTranslation(enemy.Position);
                enemyColors[i] = enemy.Color;
            }
            
            renderingSystem.RenderCubesInstanced(enemyTransforms, enemyColors, enemyCount);
        }
        
        // Prepare instance data for projectiles
        var projectiles = entityManager.Projectiles;
        int projectileCount = Math.Min(projectiles.Count(p => p.IsActive), 500);
        
        if (projectileCount > 0)
        {
            Matrix4x4[] projectileTransforms = new Matrix4x4[projectileCount];
            Vector3[] projectileColors = new Vector3[projectileCount];
            
            int index = 0;
            foreach (var proj in projectiles)
            {
                if (!proj.IsActive) continue;
                if (index >= projectileCount) break;
                
                projectileTransforms[index] = Matrix4x4.CreateScale(0.1f) * 
                                             Matrix4x4.CreateTranslation(proj.Position);
                projectileColors[index] = new Vector3(1f, 1f, 0f); // Yellow
                index++;
            }
            
            renderingSystem.RenderCubesInstanced(projectileTransforms, projectileColors, projectileCount);
        }
        
        // Render obstacles
        foreach (var obstacle in entityManager.Obstacles)
        {
            if (obstacle.IsDestroyed) continue;
            
            Matrix4x4 transform = Matrix4x4.CreateScale(obstacle.Size) * 
                                 Matrix4x4.CreateTranslation(obstacle.Position);
            Vector3 color = new Vector3(0.3f, 0.3f, 0.3f); // Gray
            
            renderingSystem.RenderCube(transform, color);
        }
        
        // Render ground plane
        Matrix4x4 groundTransform = Matrix4x4.CreateScale(100f, 0.1f, 100f) * 
                                   Matrix4x4.CreateTranslation(0, -0.05f, 0);
        renderingSystem.RenderCube(groundTransform, new Vector3(0.2f, 0.3f, 0.2f));
        
        // Render weapon effects
        if (weaponSystem is WeaponSystem ws && playerSystem is PlayerSystem ps)
        {
            var view = ps.Camera.GetViewMatrix();
            float aspect = renderingSystem.GetAspectRatio();
            float fov = uiCoordinator?.FieldOfView ?? 90f;
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(
                fov * MathF.PI / 180f, aspect, 0.1f, 1000f);
            
            ws.RenderEffects(ps.Position, view, proj);
        }
    }
    
    private void RenderEditorView()
    {
        if (levelEditor != null && renderingSystem != null)
        {
            // LevelEditor.Render takes screen dimensions
            float width = window.Size.X;
            float height = window.Size.Y;
            levelEditor.Render(width, height);
        }
    }
    
    private void HandleGlobalInput()
    {
        if (inputSystem == null) return;
        
        // ESC - Toggle settings menu
        if (inputSystem.IsKeyJustPressed(Key.Escape))
        {
            uiCoordinator?.ToggleSettingsMenu();
        }
        
        // F1 - Toggle debug info
        if (inputSystem.IsKeyJustPressed(Key.F1))
        {
            if (uiCoordinator is UICoordinator uiCoord)
            {
                uiCoord.ToggleDebugInfo();
            }
        }
        
        // F2 - Debug spawn enemies
        if (inputSystem.IsKeyJustPressed(Key.F2))
        {
            waveManager?.ForceSpawnEnemies(5);
        }
        
        // F3 - Toggle level editor
        if (inputSystem.IsKeyJustPressed(Key.F3))
        {
            stateManager?.ToggleEditor();
            
            if (stateManager?.IsEditorMode == true)
            {
                inputSystem.SetCursorMode(CursorMode.Normal);
                // LevelEditor doesn't have EnterEditMode, just set active
                if (levelEditor != null)
                {
                    // Editor is activated by being in editor mode
                }
            }
            else
            {
                inputSystem.SetCursorMode(CursorMode.Raw);
                ApplyLevelChanges();
            }
        }
    }
    
    private void UpdateFPSCounter(double deltaTime)
    {
        fpsTimer += deltaTime;
        frameCount++;
        
        if (fpsTimer >= 1.0)
        {
            fps = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0;
        }
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
            if (collisionSystem.CheckMeleeRange(enemy.Position, playerSystem.Position, 2f))
            {
                // Apply damage to player
                playerSystem.TakeDamage(10f, enemy.Position);
            }
        }
    }
    
    private void CheckWaveStatus()
    {
        if (waveManager?.IsWaveComplete() == true)
        {
            // Could trigger victory conditions or special effects here
        }
    }
    
    private void GenerateInitialObstacles()
    {
        if (entityManager == null) return;
        
        // Create some test obstacles
        entityManager.CreateObstacle(new Vector3(5, 1, 5), new Vector3(2, 2, 2));
        entityManager.CreateObstacle(new Vector3(-5, 1, 5), new Vector3(2, 2, 2));
        entityManager.CreateObstacle(new Vector3(0, 1, 10), new Vector3(3, 1, 3));
        entityManager.CreateObstacle(new Vector3(-8, 1.5f, -3), new Vector3(1, 3, 1));
        entityManager.CreateObstacle(new Vector3(8, 1.5f, -3), new Vector3(1, 3, 1));
    }
    
    private void ApplyLevelChanges()
    {
        // Apply any changes made in the level editor
        // This would involve updating entity positions, adding/removing obstacles, etc.
    }
    
    public void Dispose()
    {
        // Dispose all systems in reverse order of creation
        levelEditor?.Dispose();
        uiCoordinator?.Dispose();
        renderingSystem?.Dispose();
        waveManager?.Dispose();
        weaponSystem?.Dispose();
        playerSystem?.Dispose();
        stateManager?.Dispose();
        collisionSystem?.Dispose();
        entityManager?.Dispose();
        inputSystem?.Dispose();
    }
}