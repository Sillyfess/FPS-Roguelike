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

### Core Game Loop (src/Core/Game.cs)
- **Fixed timestep**: Physics runs at 60Hz (16.67ms per tick), rendering interpolates between physics frames
- **Main responsibilities**: Manages all subsystems, handles rendering, wave spawning, collision detection
- **Key pattern**: Update loop processes input → fixed physics updates → render with interpolation
- **Note**: Game.cs is 800+ lines and handles too many responsibilities - consider refactoring when adding major features

### Threading & Input System
- **InputSystem.cs**: Thread-safe with lock objects, polls once per frame
- **Raw mouse input**: Bypasses OS acceleration for precise FPS aiming
- **Mouse look**: Applied every frame (not in fixed timestep) to maintain responsiveness
- **Important**: Always use locks when accessing shared state between threads

### Combat System
- **Weapons**: Base Weapon.cs with implementations (Revolver, SMG, Katana)
- **Projectiles**: Object pooling with 100 pre-allocated instances
- **Collision**: O(n²) checks between projectiles and enemies - no spatial partitioning yet
- **Boss enemies**: Special Boss.cs with charge attacks and melee damage

### Rendering Pipeline
- **OpenGL via Silk.NET**: Direct OpenGL 3.3+ with unsafe blocks for performance
- **GPU Instancing**: Enabled for massive performance improvement (recent addition)
- **Resources**: VAOs, VBOs, shaders created but disposal chain incomplete
- **UI**: HUD.cs renders health, score, waves; SimpleUIManager handles settings menu

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
├── Core/          # Game.cs (main loop), ILogger.cs
├── Input/         # InputSystem.cs - thread-safe input handling
├── Entities/      # Enemy.cs, Boss.cs, PlayerHealth.cs
├── Physics/       # CharacterController.cs - movement & collision
├── Combat/        # Weapon.cs, Revolver.cs, SMG.cs, Katana.cs, Projectile.cs
├── Rendering/     # Camera.cs, Renderer.cs, Crosshair.cs, SlashEffect.cs
├── UI/            # HUD.cs, SimpleUIManager.cs - UI rendering
└── Environment/   # Obstacle.cs - environmental hazards
```

## Key Technical Details

### Performance Considerations
- **Object pooling**: Projectiles pre-allocated to avoid GC
- **GPU instancing**: Recently added for massive performance gains
- **Fixed timestep**: Ensures deterministic physics
- **Unsafe blocks**: Used for OpenGL interop - be careful with pointers

### Known Issues (see ISSUES.md for full list)
- **Resource disposal**: Some OpenGL resources not properly cleaned up
- **Architecture**: Game.cs too large, tight coupling between systems
- **No spatial partitioning**: Collision detection is O(n²)
- **No settings persistence**: User preferences lost on restart

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
2. Override fire behavior as needed
3. Add weapon to Game.cs weapon switching logic
4. Update HUD to display weapon-specific info

### Adding a New Enemy Type
1. Inherit from `Enemy.cs` or create similar to `Boss.cs`
2. Override state machine methods for custom behavior
3. Add spawning logic in Game.cs wave system
4. Consider adding to difficulty scaling

### Modifying UI Elements
1. HUD.cs for in-game UI (health, score, waves)
2. SimpleUIManager.cs for settings menu
3. Use unsafe blocks for OpenGL operations
4. Remember to update vertex/fragment shaders if needed

## Important Files
- **README.md**: Project overview and features
- **ISSUES.md**: Known technical debt and bugs
- **CODE_STANDARDS.md**: Detailed coding guidelines
- **FPSRoguelike.csproj**: Project configuration (.NET 9.0, unsafe blocks enabled)