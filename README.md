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
| **Exit** | ESC |

## 🏗️ Architecture

### Core Systems
- **Fixed Timestep Game Loop**: 60Hz physics with interpolated rendering
- **Raw Mouse Input**: Precise FPS aiming without OS acceleration  
- **Custom Physics**: Character controller with gravity and jumping
- **Component System**: Simple entity composition (not full ECS)

### Project Structure
```
src/
├── Core/          # Main game loop and systems
├── Input/         # Raw input handling
├── Entities/      # Enemy AI, player health
├── Physics/       # Character controller
├── Combat/        # Weapons and projectiles
└── Rendering/     # Camera, renderer, UI
```

## ✅ Current Features

### Movement System
- WASD movement with instant direction changes
- Jump mechanics with 2m height
- 30% air control for mid-air adjustments
- Gravity simulation (-20 m/s²)
- Ground collision detection

### Combat System
- Raycast-based pistol weapon
- Enemy projectile attacks
- Health and damage systems
- Visual hit feedback (hit markers, damage flash)
- Destructible environment targets

### Enemy AI
- State machine behavior (Idle, Patrol, Chase, Attack)
- Dynamic movement patterns
- Player detection and tracking
- Projectile-based attacks
- Health scaling per wave

### Wave System
- Progressive difficulty
- Enemy count scales with waves
- 5-second delay between waves
- Automatic spawning on completion

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
- [ ] Additional weapons (shotgun, machine gun, rocket launcher)
- [ ] Movement abilities (dash, double jump)
- [ ] Score and UI overlay
- [ ] Sound effects

### Future Plans
- [ ] Roguelike item system
- [ ] Boss enemies
- [ ] Arena variety
- [ ] Visual effects (particles, screen shake)
- [ ] Multiplayer support

## 📚 Documentation

- **[CLAUDE.md](CLAUDE.md)** - AI assistant instructions and codebase guide
- **[Technical-Document.md](Technical-Document.md)** - Core vision and technical decisions
- **[Architecture-Plan.md](Architecture-Plan.md)** - Detailed system architecture
- **[Implementation-Guide.md](Implementation-Guide.md)** - Implementation reference
- **[PROGRESS.md](PROGRESS.md)** - Development timeline and completed features
- **[ISSUES.md](ISSUES.md)** - Known issues and technical debt

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