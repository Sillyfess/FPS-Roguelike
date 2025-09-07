# CLAUDE.md

Quick reference for AI assistants working with this FPS Roguelike codebase.

## Build & Run
```bash
dotnet build FPSRoguelike.csproj
dotnet run --project FPSRoguelike.csproj
```

## Project Structure
```
src/
├── Core/          # Game.cs - Main game loop (fixed timestep @ 60Hz)
├── Input/         # InputSystem.cs - Thread-safe input with raw mouse
├── Entities/      # Enemy.cs, PlayerHealth.cs
├── Physics/       # CharacterController.cs
├── Combat/        # Weapon.cs, Projectile.cs
├── Rendering/     # Camera.cs, Renderer.cs, Crosshair.cs
├── UI/            # HUD.cs, SimpleUIManager.cs - Settings menu and UI
└── Environment/   # Obstacle.cs - Environmental obstacles
```

## Key Architecture
- **Fixed timestep**: Physics at 60Hz, interpolated rendering
- **Input**: Poll once per frame, mouse look every frame (not in fixed step)
- **Object pooling**: 100 projectiles pre-allocated
- **Thread safety**: Locks in InputSystem, shared Random in Enemy


## Code Standards (Brief)
- **Constants**: UPPER_SNAKE_CASE (DEFAULT_*, MAX_*, MIN_*)
- **Classes**: PascalCase
- **Private fields**: camelCase
- **No magic numbers**: Use constants for all values
- **Comments**: Explain WHY not WHAT
- **Disposal**: IDisposable for resources

See CODE_STANDARDS.md for complete guidelines.

## Controls
- **WASD**: Move
- **Mouse**: Look
- **Space**: Jump
- **LMB**: Shoot
- **R**: Respawn when dead
- **F1**: Toggle debug info
- **F2**: Spawn enemies (debug)
- **ESC**: Toggle settings menu
- **Arrow Keys/WASD**: Navigate settings menu
- **Enter/Space**: Select menu item

## Important Files
- **README.md**: Project overview
- **ISSUES.md**: Known technical debt
- **CODE_STANDARDS.md**: Detailed coding guidelines