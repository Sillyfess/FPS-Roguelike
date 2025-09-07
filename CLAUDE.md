# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

### Build and Run
```bash
# Build the project
dotnet build

# Run the project
dotnet run

# Clean build artifacts
dotnet clean
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
The project is in early prototype phase with:
- Window creation and OpenGL context setup ✓
- Basic input system with raw mouse input ✓
- Test cube rendering with rotation ✓
- Fixed timestep game loop ✓
- Basic folder structure established ✓

### Next Implementation Steps
1. FPS camera with mouse look
2. WASD movement in 3D space
3. Character controller with gravity and jumping
4. Collision detection with ground plane
5. Basic weapon shooting mechanics
6. Simple enemy with projectiles

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

## Common Issues

### Mouse Input Lag
Ensure `CursorMode.Raw` is set and input polling happens before physics updates.

### Movement Stuttering
Check fixed timestep implementation and ensure interpolation is working correctly in rendering.

### Build Errors
If packages are missing, run `dotnet restore` to fetch NuGet dependencies.