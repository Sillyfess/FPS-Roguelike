# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run Commands
```bash
# Build the project
dotnet build FPSRoguelike.csproj

# Run the game
dotnet run --project FPSRoguelike.csproj

# Clean build artifacts
dotnet clean
```

## High-Level Architecture

### Core Game Loop (src/Core/GameRefactored.cs)
- **Fixed timestep**: Physics runs at 60Hz (16.67ms per tick), rendering interpolates between physics frames
- **Main coordinator**: Orchestrates all game systems following SOLID principles and dependency injection
- **Key pattern**: Update loop processes input → fixed physics updates → render with interpolation
- **Major refactoring**: Original monolithic Game.cs (800+ lines) replaced with modular GameRefactored.cs

### System Architecture (src/Systems/)
The game uses a modular system architecture with clear interfaces:
- **IEntityManager**: Manages all game entities (enemies, projectiles, obstacles)
- **ICollisionSystem**: Handles collision detection with spatial hashing optimization
- **IWeaponSystem**: Manages weapon switching and projectile creation
- **IPlayerSystem**: Handles player input, movement, and health
- **IWaveManager**: Controls enemy wave spawning and difficulty scaling
- **IRenderingSystem**: Manages all rendering operations
- **IGameStateManager**: Tracks game state (playing, paused, game over)
- **IUICoordinator**: Manages ImGui-based UI and settings menu

### Threading & Input System
- **InputSystem.cs**: Thread-safe with lock objects, polls once per frame
- **Raw mouse input**: Bypasses OS acceleration for precise FPS aiming
- **Mouse look**: Applied every frame (not in fixed timestep) to maintain responsiveness
- **Important**: Always use locks when accessing shared state between threads

### Combat System
- **Weapons**: Base Weapon.cs with implementations (Revolver, SMG, Katana)
- **Projectiles**: Object pooling with 100 pre-allocated instances
- **Collision**: Spatial hash grid for efficient collision detection (replaced O(n²) checks)
- **Boss enemies**: Special Boss.cs with charge attacks and melee damage

### Rendering Pipeline
- **OpenGL via Silk.NET**: Direct OpenGL 3.3+ with unsafe blocks for performance
- **GPU Instancing**: Enabled for massive performance improvement
- **Resources**: VAOs, VBOs, shaders with improved disposal chain
- **UI**: ImGui-based HUD (ImGuiHUD.cs) for health, ammo, waves; SimpleUIManager for settings

## Critical Patterns to Follow

### Resource Management
```csharp
// Always implement IDisposable for OpenGL resources
public class ResourceClass : IDisposable
{
    // OpenGL handles
    private uint vao, vbo, shader;
    
    public void Dispose()
    {
        // Clean up OpenGL resources
        gl?.DeleteVertexArray(vao);
        gl?.DeleteBuffer(vbo);
        gl?.DeleteProgram(shader);
    }
}
```

### Constants Usage
```csharp
// ALWAYS use named constants - no magic numbers
private const float DEFAULT_MOVE_SPEED = 5f;
private const int MAX_PROJECTILES = 100;
private const float FIXED_TIMESTEP = 1f / 60f;
```

### Input Validation
```csharp
// Validate all public method parameters
public void Fire(Vector3 direction, float speed)
{
    if (float.IsNaN(direction.X) || float.IsNaN(direction.Y))
        throw new ArgumentException("Invalid direction");
    if (speed <= 0 || !float.IsFinite(speed))
        throw new ArgumentException("Invalid speed");
}
```

## Project Structure
```
src/
├── Core/          # GameRefactored.cs (main coordinator), Settings.cs, ILogger.cs
├── Systems/       # Modular game systems with interfaces
│   ├── Interfaces/  # System contracts (IEntityManager, ICollisionSystem, etc.)
│   └── Core/        # System implementations
├── Input/         # InputSystem.cs - thread-safe input handling
├── Entities/      # Enemy.cs, Boss.cs, PlayerHealth.cs
├── Physics/       # CharacterController.cs, SpatialHashGrid.cs
├── Combat/        # Weapon.cs, Revolver.cs, SMG.cs, Katana.cs, Projectile.cs
├── Rendering/     # Camera.cs, Renderer.cs, SlashEffect.cs
├── UI/            # ImGuiHUD.cs, ImGuiWrapper.cs, SimpleUIManager.cs
├── Environment/   # Obstacle.cs - environmental hazards
└── Editor/        # LevelEditor.cs, EditorUI.cs (level editing tools)
```

## Key Technical Details

### Performance Considerations
- **Object pooling**: Projectiles pre-allocated to avoid GC
- **GPU instancing**: Massive performance gains for rendering
- **Spatial hashing**: Efficient collision detection replacing O(n²) checks
- **Fixed timestep**: Ensures deterministic physics
- **Unsafe blocks**: Used for OpenGL interop - be careful with pointers

### Known Issues (see ISSUES.md for full list)
- **Resource disposal**: Some OpenGL resources in Renderer not properly cleaned
- **Settings persistence**: User preferences lost on restart (Settings.cs exists but not persisted)
- **Console output**: SimpleUIManager still uses Console.WriteLine
- **Unsafe blocks**: Extensive use without bounds checking in rendering code

## Controls Reference
- **WASD**: Movement
- **Mouse**: Look around
- **Space**: Jump
- **LMB**: Fire weapon
- **R**: Respawn when dead
- **F1**: Toggle debug info
- **F2**: Spawn enemies (debug)
- **ESC**: Settings menu
- **Arrow Keys/WASD**: Navigate menu
- **Enter/Space**: Select menu item

## Code Standards Summary
- **Classes**: PascalCase
- **Private fields**: camelCase  
- **Constants**: UPPER_SNAKE_CASE with prefixes (DEFAULT_*, MAX_*, MIN_*)
- **No magic numbers**: Always use named constants
- **Comments**: Explain WHY, not WHAT
- **Disposal**: Implement IDisposable for resources
- **Validation**: Check parameters in public methods

See CODE_STANDARDS.md for complete guidelines.

## Common Development Tasks

### Adding a New Weapon
1. Inherit from `Weapon.cs` base class
2. Override Fire(), Update(), and Reload() methods as needed
3. Add weapon to WeaponSystem.cs weapon collection
4. Update ImGuiHUD.cs to display weapon-specific info

### Adding a New Enemy Type
1. Inherit from `Enemy.cs` or create similar to `Boss.cs`
2. Override state machine methods for custom behavior
3. Add spawning logic in WaveManager.cs
4. Register with EntityManager for proper lifecycle management

### Modifying UI Elements
1. ImGuiHUD.cs for in-game HUD (health, ammo, waves, kill feed)
2. SimpleUIManager.cs for settings menu (still uses console output)
3. ImGuiWrapper.cs manages ImGui context and rendering
4. UICoordinator.cs orchestrates all UI systems

### Adding a New System
1. Create interface in src/Systems/Interfaces/
2. Implement in src/Systems/Core/
3. Add to GameRefactored.cs with proper dependency injection
4. Ensure no circular dependencies between systems

## Important Files
- **README.md**: Project overview and features
- **ISSUES.md**: Known technical debt and bugs (many recently fixed)
- **CODE_STANDARDS.md**: Detailed coding guidelines
- **FPSRoguelike.csproj**: Project configuration (.NET 9.0, unsafe blocks enabled)
- **Program.cs**: Entry point using GameRefactored instead of Game