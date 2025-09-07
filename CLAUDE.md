# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Run
```bash
# Build the project
dotnet build FPSRoguelike.csproj

# Run the project
dotnet run --project FPSRoguelike.csproj

# Clean build artifacts
dotnet clean

# Build and run in one command
dotnet build FPSRoguelike.csproj && dotnet run --project FPSRoguelike.csproj
```

### Development
```bash
# Restore packages
dotnet restore

# Watch for changes and rebuild
dotnet watch run
```

## Architecture

### Core Game Loop
The game uses a **fixed timestep game loop** with interpolated rendering for smooth visuals while maintaining deterministic physics:
- Fixed physics/logic updates at 60 Hz (FIXED_TIMESTEP = 1/60)
- Interpolated rendering between physics frames
- Input polling occurs before each fixed update
- Located in `src/Core/Game.cs`

### Project Structure
- **src/Core/** - Main game class, game loop, timing systems
- **src/Input/** - Raw input processing, action mapping, mouse/keyboard handling
- **src/Entities/** - Entity system, components (Transform, Velocity, Collider, Health)
- **src/Physics/** - Physics simulation, character controller, collision detection
- **src/Combat/** - Weapons, projectiles, hit detection, damage calculation
- **src/Rendering/** - OpenGL renderer, camera, shaders, mesh handling

### Key Design Decisions
- **Movement Style**: Fast arcade-style movement with instant direction changes (not momentum-based)
- **Entity System**: Simple composition-based system, not full ECS yet
- **Networking Ready**: Fixed timestep architecture supports deterministic simulation for future multiplayer
- **Graphics**: Silk.NET with OpenGL, targeting Windows first
- **Physics**: Custom character controller initially, may integrate BEPUphysics v2 later

### Current Implementation Status
The project has a **playable prototype** with:
- Window creation and OpenGL context setup ✓
- Raw mouse input system with keyboard/mouse polling ✓
- FPS camera with mouse look controls ✓
- Character controller with WASD movement, gravity, and jumping ✓
- Basic pistol weapon with raycast hit detection ✓
- Destructible cube targets with hit markers ✓
- Classic CS-style green crosshair UI ✓
- Test arena with ground plane and multiple targets ✓
- Fixed timestep game loop with interpolated rendering ✓

### Next Implementation Steps
1. Enemy AI system with basic movement patterns
2. Enemy projectile attacks and health system
3. Wave spawning and arena progression
4. Score/UI overlay system
5. Additional weapon types (shotgun, machine gun, rocket launcher)
6. Movement abilities (dash, double jump)

## Technical Notes

### Dependencies
- **Silk.NET** (v2.20.0) - Windowing, Input, OpenGL, Math libraries
- **.NET 9.0** - Target framework
- **AllowUnsafeBlocks** enabled for OpenGL interop

### Performance Targets
- Consistent 60 FPS minimum
- Input latency < 16ms
- No frame time spikes > 33ms
- Memory usage stable (no leaks)

### Important Patterns
- **Fixed Timestep**: All physics/game logic uses fixed delta time for determinism
- **Interpolation**: Rendering interpolates between physics frames for smoothness
- **Raw Mouse Mode**: FPS controls use raw mouse input to bypass OS acceleration
- **Component System**: Entities use composition for flexibility without deep inheritance

## Game Controls

### In-Game Controls
- **WASD** - Move forward/backward/left/right
- **Mouse** - Look around (FPS camera)
- **Space** - Jump
- **Left Mouse** - Shoot weapon
- **F1** - Toggle debug info (position, velocity, grounded status)
- **ESC** - Exit game

## Key Implementation Details

### Shader System
The game uses embedded GLSL shaders (version 330 core) for rendering:
- **Main shader** (src/Core/Game.cs:303-397): Handles cube rendering with directional lighting and specular highlights
- **Crosshair shader** (src/Rendering/Crosshair.cs:67-147): Renders 2D UI elements in screen space

### Hit Detection System
- Raycast-based shooting against cube targets (src/Combat/Weapon.cs:42-96)
- Visual hit markers that pulse and fade over 2 seconds
- Destructible targets tracked in HashSet to prevent re-shooting destroyed cubes

### Input Handling
- Raw mouse mode for precise FPS aiming (bypasses OS acceleration)
- Mouse delta accumulation between fixed timestep updates
- Separate previous/current state tracking for "just pressed" detection

## Common Issues

### Mouse Input Lag
Ensure `CursorMode.Raw` is set and input polling happens before physics updates. Check that mouse delta is accumulated properly in InputSystem.cs.

### Movement Stuttering
Check fixed timestep implementation and ensure interpolation is working correctly in rendering. Verify accumulator logic in Game.cs Update method.

### Build Errors
If packages are missing, run `dotnet restore` to fetch NuGet dependencies.

### Mouse Look Inverted
The camera rotation has been fixed with negated X-axis (src/Rendering/Camera.cs:30). If issues persist, check mouseSensitivity value.