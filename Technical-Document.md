# FPS Roguelike - Technical Architecture Document

## Core Vision
**Goal**: The best possible action combat FPS with roguelike systems to extend replayability.  
**Not**: A story game, exploration game, or tactical shooter.

**Core Loop**: Arena-based FPS combat where movement replaces cover, inspired by Risk of Rain 2's structure and Ultrakill's game feel.

## Technology Stack
- **Language**: C# (.NET 8+)
- **Graphics**: Silk.NET for OpenGL
- **Math**: System.Numerics 
- **Physics**: Custom character controller first, consider BEPUphysics v2 later
- **Networking**: Later addition - LiteNetLib or Mirror when needed
- **Target**: Windows first, Steam release

## Minimal Starting Architecture

### 1. Core Game Loop
```csharp
// Fixed timestep for deterministic physics
// Interpolated rendering for smooth visuals
while (running)
{
    Input.Process();
    
    accumulator += deltaTime;
    while (accumulator >= FIXED_TIMESTEP)
    {
        Game.FixedUpdate(FIXED_TIMESTEP);
        accumulator -= FIXED_TIMESTEP;
    }
    
    float interpolation = accumulator / FIXED_TIMESTEP;
    Render.Draw(interpolation);
}
```

### 2. Simple Entity Structure
```csharp
// Start simple, expand as needed
// NOT a full ECS yet - just composition
class Entity 
{
    public Transform Transform;
    public Velocity Velocity;  
    public Collider Collider;
    // Add components as gameplay demands
}
```

### 3. Separation of Concerns
- **Input System**: Separate from game logic, polls at fixed rate
- **Game State**: Updates at fixed timestep
- **Renderer**: Interpolates positions, separate from game logic
- **Physics**: Own coordinate space and update cycle

*Note: Can start in single files with clean interfaces, split later*

### 4. Foundation Systems

**Input**
- Raw mouse input (bypass OS acceleration)
- Input buffering for frame-independent processing
- Action mapping (separate bindings from game logic)

**Movement**
- Velocity-based, no acceleration curves initially
- Direct control in air and on ground
- Capsule collider for player
- Start with move speed, air control, jump height, gravity constants

**Combat**
- Hitscan or projectile (test both)
- No ammo management
- Visual and audio feedback on hit
- Simple damage numbers

## Implementation Order

### Phase 1: Basic Movement
1. Window + OpenGL context
2. Render a cube
3. Camera with mouse look
4. WASD movement in 3D space
5. Gravity and jumping
6. Collision with floor plane

### Phase 2: Combat Feel
1. One weapon that shoots
2. One enemy that moves and shoots projectiles
3. Hit feedback (visual + audio + screenshake)
4. Enemy death state
5. Simple arena box

### Phase 3: Core Polish
1. Movement refinement based on feel
2. Wall running IF it adds to combat
3. Second enemy type
4. Second weapon
5. Arena iteration

### Phase 4: Expand When Proven Fun
- Roguelike items
- Arena progression
- Co-op networking
- Visual style

## What NOT to Build Yet
- ❌ Generic asset loading system
- ❌ Scene management
- ❌ Abstract rendering backends  
- ❌ Memory pooling/optimization
- ❌ Full ECS
- ❌ Level editor
- ❌ Save system
- ❌ Configuration/settings

## Key Decisions

**Movement**
- Fast, arcade style (not momentum-based)
- Instant direction changes
- Movement is defense (no cover system)
- Player is tanky but mobile

**Combat**  
- No reload or unlimited fast reload (TBD based on feel)
- No ammo management
- Weapons work at full movement speed
- Discrete arenas, not exploration

**Technical**
- Fixed timestep physics (for networking later)
- Start monolithic, refactor when patterns emerge
- Optimize based on profiling, not speculation
- Build the game, not an engine

## Success Metrics
- Can I dodge projectiles through movement?
- Does shooting feel responsive and impactful?
- Can I add new enemy types easily?
- Is the code structure holding up as I add features?

## Open Questions (To Be Answered Through Prototyping)
- Hitscan vs projectile weapons?
- Should there be headshots/weak points?
- Reload mechanics or pure weapon swapping?
- How much air control feels right?
- Wall running automatic or manual?

---
*This is a living document. Update as decisions are made through prototyping.*