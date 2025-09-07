# FPS Roguelike - Detailed Architecture Plan

## Project Structure

```
FPSRoguelike/
├── src/
│   ├── Core/                      # Core engine systems
│   │   ├── Game.cs                # Main game class and entry point
│   │   ├── GameLoop.cs            # Fixed timestep game loop
│   │   ├── Time.cs                # Time management and delta time
│   │   └── IUpdateable.cs         # Update interfaces
│   │
│   ├── Input/                     # Input handling
│   │   ├── InputSystem.cs         # Raw input processing
│   │   ├── InputActions.cs        # Action mapping
│   │   └── MouseState.cs          # Mouse delta tracking
│   │
│   ├── Entities/                  # Game objects
│   │   ├── Entity.cs              # Base entity class
│   │   ├── Components/            # Component definitions
│   │   │   ├── Transform.cs       # Position, rotation, scale
│   │   │   ├── Velocity.cs        # Movement velocity
│   │   │   ├── Collider.cs        # Collision bounds
│   │   │   └── Health.cs          # Damage tracking
│   │   ├── Player.cs              # Player-specific entity
│   │   └── Enemy.cs               # Enemy base class
│   │
│   ├── Physics/                   # Physics and collision
│   │   ├── PhysicsWorld.cs        # Physics simulation
│   │   ├── CharacterController.cs # FPS movement controller
│   │   ├── Collision.cs           # Collision detection
│   │   └── PhysicsConstants.cs    # Gravity, speeds, etc.
│   │
│   ├── Combat/                    # Combat systems
│   │   ├── Weapon.cs              # Weapon base class
│   │   ├── Projectile.cs          # Projectile physics
│   │   ├── HitDetection.cs        # Raycast/projectile hits
│   │   └── DamageSystem.cs        # Damage calculation
│   │
│   ├── Rendering/                 # Graphics and rendering
│   │   ├── Renderer.cs            # OpenGL renderer wrapper
│   │   ├── Camera.cs              # FPS camera
│   │   ├── Shader.cs              # Shader management
│   │   ├── Mesh.cs                # 3D mesh data
│   │   └── RenderInterpolation.cs # Smooth position interpolation
│   │
│   └── Program.cs                 # Application entry point
│
├── Content/                       # Game assets (later)
│   ├── Shaders/
│   ├── Models/
│   └── Textures/
│
├── FPSRoguelike.csproj           # Project file
└── README.md
```

## Core Architecture Components

### 1. Game Loop Architecture

```csharp
// Core/GameLoop.cs
public class GameLoop
{
    private const double FIXED_TIMESTEP = 1.0 / 60.0; // 60 Hz physics
    private const int MAX_UPDATES_PER_FRAME = 5;      // Prevent spiral of death
    
    private double accumulator = 0.0;
    private double currentTime;
    private double frameTime;
    
    public void Run()
    {
        double previousTime = GetTime();
        
        while (IsRunning)
        {
            double newTime = GetTime();
            frameTime = Math.Min(newTime - previousTime, 0.25); // Cap at 250ms
            previousTime = newTime;
            
            accumulator += frameTime;
            
            // Fixed timestep updates
            int updates = 0;
            while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES_PER_FRAME)
            {
                InputSystem.Poll();
                PhysicsWorld.Step(FIXED_TIMESTEP);
                GameState.Update(FIXED_TIMESTEP);
                
                accumulator -= FIXED_TIMESTEP;
                updates++;
            }
            
            // Interpolated rendering
            double interpolation = accumulator / FIXED_TIMESTEP;
            Renderer.Draw(interpolation);
        }
    }
}
```

### 2. Entity Component System (Simplified)

```csharp
// Entities/Entity.cs
public class Entity
{
    public int Id { get; }
    public Transform Transform { get; set; }
    public bool IsActive { get; set; } = true;
    
    private Dictionary<Type, IComponent> components = new();
    
    public T AddComponent<T>() where T : IComponent, new()
    {
        var component = new T();
        component.Entity = this;
        components[typeof(T)] = component;
        return component;
    }
    
    public T GetComponent<T>() where T : IComponent
    {
        return components.TryGetValue(typeof(T), out var component) 
            ? (T)component 
            : default;
    }
    
    public bool HasComponent<T>() where T : IComponent
    {
        return components.ContainsKey(typeof(T));
    }
}

// Components/IComponent.cs
public interface IComponent
{
    Entity Entity { get; set; }
}

// Components/Transform.cs
public class Transform : IComponent
{
    public Entity Entity { get; set; }
    
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;
    public Vector3 Scale { get; set; } = Vector3.One;
    
    // For interpolation
    public Vector3 PreviousPosition { get; set; }
    public Quaternion PreviousRotation { get; set; }
    
    public Matrix4x4 WorldMatrix => 
        Matrix4x4.CreateScale(Scale) *
        Matrix4x4.CreateFromQuaternion(Rotation) *
        Matrix4x4.CreateTranslation(Position);
}
```

### 3. Input System

```csharp
// Input/InputSystem.cs
public static class InputSystem
{
    private static MouseState currentMouse;
    private static MouseState previousMouse;
    private static KeyboardState currentKeyboard;
    private static KeyboardState previousKeyboard;
    
    private static Vector2 mouseDelta;
    private static bool rawMouseMode = true;
    
    public static void Initialize(IWindow window)
    {
        // Hook into Silk.NET input events
        window.Input.Mice[0].MouseMove += OnMouseMove;
        window.Input.Keyboards[0].KeyDown += OnKeyDown;
        window.Input.Keyboards[0].KeyUp += OnKeyUp;
        
        // Lock cursor for FPS controls
        window.Input.Mice[0].Cursor.CursorMode = CursorMode.Raw;
    }
    
    public static void Poll()
    {
        previousMouse = currentMouse;
        previousKeyboard = currentKeyboard;
        
        // Reset per-frame values
        mouseDelta = Vector2.Zero;
    }
    
    public static Vector2 GetMouseDelta() => mouseDelta;
    public static bool IsKeyPressed(Key key) => /* implementation */;
    public static bool IsKeyJustPressed(Key key) => /* implementation */;
    public static bool IsMouseButtonPressed(MouseButton button) => /* implementation */;
    
    // Input action mapping
    public static float GetAxis(string axisName)
    {
        return axisName switch
        {
            "Horizontal" => (IsKeyPressed(Key.D) ? 1f : 0f) - (IsKeyPressed(Key.A) ? 1f : 0f),
            "Vertical" => (IsKeyPressed(Key.W) ? 1f : 0f) - (IsKeyPressed(Key.S) ? 1f : 0f),
            "MouseX" => mouseDelta.X,
            "MouseY" => mouseDelta.Y,
            _ => 0f
        };
    }
}
```

### 4. Character Controller

```csharp
// Physics/CharacterController.cs
public class CharacterController : IComponent
{
    public Entity Entity { get; set; }
    
    // Movement parameters
    public float MoveSpeed { get; set; } = 10f;
    public float JumpHeight { get; set; } = 2f;
    public float Gravity { get; set; } = -20f;
    public float AirControl { get; set; } = 0.3f;
    
    // State
    private Vector3 velocity;
    private bool isGrounded;
    private float groundCheckDistance = 0.1f;
    
    public void Move(Vector3 moveInput, float deltaTime)
    {
        var transform = Entity.GetComponent<Transform>();
        
        // Ground check (simple for now)
        isGrounded = Physics.Raycast(
            transform.Position, 
            Vector3.Down, 
            groundCheckDistance + 0.5f // Half capsule height
        );
        
        // Horizontal movement
        Vector3 moveDirection = moveInput.Normalized();
        float currentSpeed = isGrounded ? MoveSpeed : MoveSpeed * AirControl;
        
        velocity.X = moveDirection.X * currentSpeed;
        velocity.Z = moveDirection.Z * currentSpeed;
        
        // Vertical movement
        if (isGrounded)
        {
            velocity.Y = 0;
            
            if (InputSystem.IsKeyJustPressed(Key.Space))
            {
                velocity.Y = MathF.Sqrt(2f * JumpHeight * -Gravity);
            }
        }
        else
        {
            velocity.Y += Gravity * deltaTime;
        }
        
        // Apply movement with collision
        Vector3 deltaPosition = velocity * deltaTime;
        transform.Position = PhysicsWorld.MoveAndSlide(
            transform.Position, 
            deltaPosition,
            Entity.GetComponent<Collider>()
        );
    }
}
```

### 5. Rendering Pipeline

```csharp
// Rendering/Renderer.cs
public class Renderer
{
    private GL gl;
    private Camera camera;
    private Shader basicShader;
    private List<RenderCommand> renderCommands = new();
    
    public void Initialize(GL glContext)
    {
        gl = glContext;
        
        // Setup OpenGL state
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);
        
        // Load basic shader
        basicShader = new Shader(gl, "basic.vert", "basic.frag");
        
        // Create camera
        camera = new Camera();
    }
    
    public void Draw(double interpolation)
    {
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        // Update camera matrices
        camera.UpdateMatrices();
        
        // Gather render commands from entities
        renderCommands.Clear();
        foreach (var entity in EntityManager.GetActiveEntities())
        {
            if (entity.HasComponent<MeshRenderer>())
            {
                var transform = entity.GetComponent<Transform>();
                var renderer = entity.GetComponent<MeshRenderer>();
                
                // Interpolate position for smooth rendering
                Vector3 interpolatedPos = Vector3.Lerp(
                    transform.PreviousPosition,
                    transform.Position,
                    (float)interpolation
                );
                
                renderCommands.Add(new RenderCommand
                {
                    Mesh = renderer.Mesh,
                    WorldMatrix = Matrix4x4.CreateTranslation(interpolatedPos),
                    Material = renderer.Material
                });
            }
        }
        
        // Sort by material/shader to minimize state changes
        renderCommands.Sort((a, b) => a.Material.Id.CompareTo(b.Material.Id));
        
        // Execute render commands
        foreach (var cmd in renderCommands)
        {
            DrawMesh(cmd);
        }
    }
}
```

## Implementation Phases

### Phase 1: Foundation (Week 1)
1. **Day 1-2**: Project setup, Silk.NET window, basic OpenGL context
2. **Day 3-4**: Game loop with fixed timestep, basic input system
3. **Day 5-7**: Simple camera, render a cube, WASD movement

### Phase 2: Movement (Week 2)
1. **Day 1-2**: Character controller with gravity and jumping
2. **Day 3-4**: Collision detection with ground plane
3. **Day 5-7**: Movement feel tuning, air control, coyote time

### Phase 3: Combat (Week 3-4)
1. **Week 3**: Basic weapon, projectile/hitscan shooting, hit detection
2. **Week 4**: Enemy AI, damage system, visual/audio feedback

### Phase 4: Polish & Iterate
- Movement refinement based on playtesting
- Additional enemy types and weapons
- Arena design and wave spawning
- Performance profiling and optimization

## Key Design Patterns

### 1. **Command Pattern** for Input
- Decouple input from actions
- Support rebinding and replay

### 2. **Observer Pattern** for Events
- Damage events, death events, etc.
- Loose coupling between systems

### 3. **Object Pool** for Projectiles
- Reuse projectile instances
- Reduce GC pressure

### 4. **State Machine** for AI
- Simple enemy behaviors
- Easy to extend and debug

## Performance Considerations

### Memory Management
- Pre-allocate pools for frequently created objects (projectiles, particles)
- Use structs for small data types (Vector3, Transform data)
- Minimize allocations in hot paths

### Rendering Optimization
- Frustum culling for off-screen entities
- Instanced rendering for multiple enemies
- Simple LOD system for distant objects

### Physics Optimization
- Spatial partitioning (simple grid initially)
- Broad phase collision detection
- Sleep inactive physics bodies

## Scaling Strategy

### From Prototype to Production

1. **Entity System Evolution**
   - Start: Simple composition as shown
   - Later: Full ECS with systems if needed
   - Benefit: Clean separation, easy parallelization

2. **Renderer Abstraction**
   - Start: Direct OpenGL calls
   - Later: Abstract render API
   - Benefit: Support Vulkan/DirectX if needed

3. **Asset Pipeline**
   - Start: Hardcoded test assets
   - Later: Asset loading system
   - Benefit: Faster iteration, modding support

4. **Networking Foundation**
   - Start: Deterministic simulation
   - Later: Add snapshot interpolation
   - Benefit: Multiplayer-ready architecture

## Code Style Guidelines

```csharp
// Use clear, descriptive names
public class CharacterController { } // Good
public class CC { } // Bad

// Prefer composition over inheritance
public class Player : Entity { } // Good
public class Player : Character : Moveable : Damageable { } // Bad

// Use properties with private setters for encapsulation
public float Health { get; private set; }

// Constants for magic numbers
private const float GRAVITY = -9.81f;

// Early returns for clarity
public void TakeDamage(float amount)
{
    if (IsDead) return;
    if (IsInvulnerable) return;
    
    Health -= amount;
}
```

## Testing Strategy

### Unit Tests
- Physics calculations
- Damage formulas
- Input mapping

### Integration Tests
- Game loop timing
- Entity spawning/destruction
- Save/load (when implemented)

### Playtesting Metrics
- Frame time consistency
- Input latency
- Movement responsiveness
- Combat feedback clarity

## Dependencies

```xml
<!-- FPSRoguelike.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Silk.NET" Version="2.20.0" />
    <PackageReference Include="Silk.NET.Input" Version="2.20.0" />
    <PackageReference Include="Silk.NET.OpenGL" Version="2.20.0" />
    <PackageReference Include="Silk.NET.Maths" Version="2.20.0" />
  </ItemGroup>
</Project>
```

## Next Steps

1. Create the project structure
2. Implement the basic game loop
3. Set up Silk.NET window and OpenGL context
4. Create input system with raw mouse input
5. Implement basic movement controller
6. Add collision detection
7. Test and iterate on movement feel

---

*This architecture is designed to start simple but scale with the project. Each system has clear boundaries and can be enhanced independently as needed.*