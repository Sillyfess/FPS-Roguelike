# FPS Roguelike

A fast-paced action FPS with roguelike elements, built from scratch using C# and OpenGL via Silk.NET.

## 🎮 Game Overview

**Genre**: Arena FPS with roguelike progression  
**Inspiration**: Risk of Rain 2's structure, Ultrakill's game feel  
**Status**: Playable prototype with core gameplay loop

## 🚀 Quick Start

### Requirements
- .NET 9.0 SDK
- Windows (primary platform)
- OpenGL 3.3+ compatible graphics

### Build & Run
```bash
# Clone the repository
git clone [repository-url]
cd FPS-Roguelike

# Build the project
dotnet build FPSRoguelike.csproj

# Run the game
dotnet run --project FPSRoguelike.csproj
```

## 🎯 Game Controls

| Action | Control |
|--------|---------|
| **Move** | WASD |
| **Look** | Mouse |
| **Jump** | Space |
| **Shoot** | Left Mouse |
| **Respawn** | R (when dead) |
| **Debug Info** | F1 |
| **Spawn Enemies** | F2 |
| **Settings Menu** | ESC |

## 🏗️ Architecture

### Core Systems
- **Fixed Timestep Game Loop**: 60Hz physics with interpolated rendering
- **Modular Architecture**: Dependency injection with SOLID principles (post-refactor)
- **Thread-Safe Input**: Raw mouse input with comprehensive locking
- **Optimized Collision**: Spatial hash grid for O(n) collision detection
- **Custom Physics**: Character controller with gravity and jumping

### Project Structure
```
src/
├── Core/          # GameRefactored.cs (main coordinator), Settings, ILogger
├── Systems/       # Modular game systems with SOLID principles
│   ├── Core/      # System implementations (EntityManager, CollisionSystem, etc.)
│   └── Interfaces/# System contracts (IEntityManager, IWeaponSystem, etc.)
├── Input/         # Thread-safe raw input handling
├── Entities/      # Enemy AI, Boss, PlayerHealth
├── Physics/       # CharacterController, SpatialHashGrid (collision optimization)
├── Combat/        # Weapons (Revolver, SMG, Katana) and Projectiles
├── Rendering/     # Camera, Renderer, SlashEffect
├── UI/            # ImGuiHUD, ImGuiWrapper, SimpleUIManager
├── Environment/   # Obstacles and level elements
└── Editor/        # Level editor tools
```

## ✅ Current Features

### Movement System
- WASD movement with instant direction changes
- Jump mechanics with 2m height
- 30% air control for mid-air adjustments
- Gravity simulation (-20 m/s²)
- Ground collision detection

### Combat System
- Multiple weapons: Revolver (raycast), SMG (rapid fire), Katana (melee)
- Thread-safe projectile pooling (500 pre-allocated)
- Spatial hash grid collision detection (90% performance improvement)
- Visual hit feedback (hit markers, damage flash, screenshake)
- Game feel enhancements (hitstop on enemy hits)
- Environmental obstacles with destructible support

### Enemy AI
- State machine behavior (Idle, Patrol, Chase, Attack)
- Dynamic movement patterns
- Player detection and tracking
- Projectile-based attacks
- Health scaling per wave

### Wave System
- Progressive difficulty
- Enemy count scales with waves (3 + wave number)
- Enemy health scales with waves (100 + 50 per wave)
- 5-second delay between waves
- Automatic spawning on completion
- Score tracking (100 points per enemy, 150 bonus per wave)

## 🛠️ Technical Stack

- **Language**: C# (.NET 9.0)
- **Graphics**: Silk.NET v2.20.0 (OpenGL wrapper)
- **Math**: System.Numerics
- **Physics**: Custom implementation
- **Platform**: Windows (cross-platform possible)

## 📈 Performance

- Target: 60 FPS minimum
- Fixed timestep: 16.67ms
- Input latency: < 16ms
- Object pooling for projectiles

## 🔜 Roadmap

### Next Priority
- [x] Additional weapons (SMG and Katana implemented)
- [ ] Movement abilities (dash, double jump)
- [x] Score and UI overlay (ImGui HUD implemented)
- [x] Settings menu with adjustable FOV, sensitivity, volume
- [ ] Sound effects (volume sliders ready, audio system needed)
- [x] Performance optimization (spatial hashing, instanced rendering)
- [x] Thread safety fixes (comprehensive locking added)

### Future Plans
- [ ] Roguelike item system
- [ ] Boss enemies
- [ ] Arena variety
- [x] Visual effects (screenshake implemented, particles pending)
- [ ] Multiplayer support
- [ ] Settings persistence (save/load preferences)
- [ ] Key rebinding system

## 📚 Documentation

- **[CLAUDE.md](CLAUDE.md)** - AI assistant instructions and codebase guide (updated)
- **[CODE_STANDARDS.md](CODE_STANDARDS.md)** - Coding standards and best practices
- **[ISSUES.md](ISSUES.md)** - Known issues and technical debt tracking

## 🤝 Contributing

This is an active prototype. Key areas for contribution:
- Performance optimization
- Additional enemy types
- Weapon variety
- Visual effects
- Sound design

## 📝 License

[License information to be added]

## 🎮 Play Testing

The game is in active development. Current focus:
- Tuning movement feel
- Balancing combat difficulty
- Improving enemy AI behaviors
- Adding gameplay variety

---

*Built with passion for fast-paced FPS action and roguelike replayability*