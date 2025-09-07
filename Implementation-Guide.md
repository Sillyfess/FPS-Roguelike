# FPS Roguelike - Implementation Guide

## Quick Start Implementation

### Step 1: Create Initial Project Structure

```bash
# Create project
dotnet new console -n FPSRoguelike
cd FPSRoguelike

# Add packages
dotnet add package Silk.NET --version 2.20.0
dotnet add package Silk.NET.Input --version 2.20.0  
dotnet add package Silk.NET.OpenGL --version 2.20.0
dotnet add package Silk.NET.Maths --version 2.20.0

# Create folder structure
mkdir src src/Core src/Input src/Entities src/Physics src/Combat src/Rendering
```

### Step 2: Minimal Working Example

```csharp
// src/Program.cs
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using System.Numerics;

namespace FPSRoguelike;

class Program
{
    private static IWindow window;
    private static GL gl;
    private static Game game;
    
    static void Main(string[] args)
    {
        // Window configuration
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "FPS Roguelike Prototype",
            PreferredStencilBufferBits = 0,
            PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8),
            PreferredDepthBufferBits = 24,
            VSync = false  // We control timing
        };
        
        window = Window.Create(options);
        
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.Closing += OnClose;
        
        window.Run();
    }
    
    private static void OnLoad()
    {
        gl = GL.GetApi(window);
        game = new Game(window, gl);
        game.Initialize();
    }
    
    private static void OnUpdate(double deltaTime)
    {
        game.Update(deltaTime);
    }
    
    private static void OnRender(double deltaTime)
    {
        game.Render(deltaTime);
    }
    
    private static void OnClose()
    {
        game?.Cleanup();
    }
}
```

### Step 3: Core Game Class

```csharp
// src/Core/Game.cs
namespace FPSRoguelike.Core;

public class Game
{
    private IWindow window;
    private GL gl;
    private GameLoop gameLoop;
    private InputSystem inputSystem;
    private Renderer renderer;
    private PhysicsWorld physicsWorld;
    
    // Timing
    private double accumulator = 0.0;
    private const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const int MAX_UPDATES = 5;
    
    public Game(IWindow window, GL gl)
    {
        this.window = window;
        this.gl = gl;
    }
    
    public void Initialize()
    {
        // Initialize systems
        inputSystem = new InputSystem();
        inputSystem.Initialize(window);
        
        renderer = new Renderer();
        renderer.Initialize(gl, window.Size.X, window.Size.Y);
        
        physicsWorld = new PhysicsWorld();
        
        // Create player
        CreatePlayer();
        
        // Lock cursor for FPS
        var mouse = window.CreateInput().Mice[0];
        mouse.Cursor.CursorMode = CursorMode.Raw;
    }
    
    public void Update(double deltaTime)
    {
        accumulator += deltaTime;
        
        int updates = 0;
        while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES)
        {
            // Fixed update
            inputSystem.Poll();
            physicsWorld.Step(FIXED_TIMESTEP);
            UpdateGameLogic(FIXED_TIMESTEP);
            
            accumulator -= FIXED_TIMESTEP;
            updates++;
        }
    }
    
    public void Render(double deltaTime)
    {
        double interpolation = accumulator / FIXED_TIMESTEP;
        renderer.Draw(interpolation);
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        // Update entities
        foreach (var entity in EntityManager.GetActiveEntities())
        {
            entity.Update((float)fixedDeltaTime);
        }
    }
}
```

### Step 4: Basic FPS Camera

```csharp
// src/Rendering/Camera.cs
namespace FPSRoguelike.Rendering;

public class Camera
{
    public Vector3 Position { get; set; }
    public float Pitch { get; private set; }  // X rotation
    public float Yaw { get; private set; }    // Y rotation
    
    public float FieldOfView { get; set; } = 90f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;
    
    private float mouseSensitivity = 0.002f;
    
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }
    
    public void UpdateRotation(Vector2 mouseDelta)
    {
        Yaw += mouseDelta.X * mouseSensitivity;
        Pitch -= mouseDelta.Y * mouseSensitivity;
        
        // Clamp pitch to prevent flipping
        Pitch = Math.Clamp(Pitch, -89f * (MathF.PI / 180f), 89f * (MathF.PI / 180f));
    }
    
    public void UpdateMatrices(float aspectRatio)
    {
        // Calculate forward vector from rotation
        Vector3 forward = new Vector3(
            MathF.Cos(Pitch) * MathF.Sin(Yaw),
            MathF.Sin(Pitch),
            MathF.Cos(Pitch) * MathF.Cos(Yaw)
        );
        
        Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
        Vector3 up = Vector3.Cross(forward, right);
        
        ViewMatrix = Matrix4x4.CreateLookAt(Position, Position + forward, up);
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * (MathF.PI / 180f),
            aspectRatio,
            NearPlane,
            FarPlane
        );
    }
    
    public Vector3 GetForward()
    {
        return new Vector3(
            MathF.Cos(Pitch) * MathF.Sin(Yaw),
            0,  // No vertical movement from look direction
            MathF.Cos(Pitch) * MathF.Cos(Yaw)
        );
    }
    
    public Vector3 GetRight()
    {
        return new Vector3(
            MathF.Cos(Yaw),
            0,
            -MathF.Sin(Yaw)
        );
    }
}
```

### Step 5: Player Controller

```csharp
// src/Entities/Player.cs
namespace FPSRoguelike.Entities;

public class Player : Entity
{
    private CharacterController controller;
    private Camera camera;
    
    public Player()
    {
        // Add components
        Transform = new Transform();
        controller = AddComponent<CharacterController>();
        
        // Setup controller parameters
        controller.MoveSpeed = 10f;
        controller.JumpHeight = 2f;
        controller.Gravity = -20f;
        
        // Create camera
        camera = new Camera();
    }
    
    public override void Update(float deltaTime)
    {
        // Handle mouse look
        Vector2 mouseDelta = InputSystem.GetMouseDelta();
        camera.UpdateRotation(mouseDelta);
        
        // Handle movement input
        Vector3 moveInput = Vector3.Zero;
        
        if (InputSystem.IsKeyPressed(Key.W))
            moveInput += camera.GetForward();
        if (InputSystem.IsKeyPressed(Key.S))
            moveInput -= camera.GetForward();
        if (InputSystem.IsKeyPressed(Key.D))
            moveInput += camera.GetRight();
        if (InputSystem.IsKeyPressed(Key.A))
            moveInput -= camera.GetRight();
            
        // Normalize diagonal movement
        if (moveInput.LengthSquared() > 0)
            moveInput = Vector3.Normalize(moveInput);
            
        // Apply movement
        controller.Move(moveInput, deltaTime);
        
        // Update camera position to follow player
        camera.Position = Transform.Position + new Vector3(0, 1.7f, 0); // Eye height
    }
}
```

## Core Interfaces

```csharp
// src/Core/IUpdateable.cs
public interface IUpdateable
{
    void Update(float deltaTime);
}

// src/Core/IRenderable.cs  
public interface IRenderable
{
    void Render(GL gl, Matrix4x4 viewMatrix, Matrix4x4 projMatrix);
}

// src/Entities/IComponent.cs
public interface IComponent
{
    Entity Entity { get; set; }
    void Initialize();
    void Update(float deltaTime);
}

// src/Physics/ICollider.cs
public interface ICollider
{
    Vector3 GetMin();
    Vector3 GetMax();
    bool Intersects(ICollider other);
    Vector3 GetClosestPoint(Vector3 point);
}
```

## Shader Setup

```glsl
// Content/Shaders/basic.vert
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;

out vec3 FragPos;
out vec3 Normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    FragPos = vec3(model * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(model))) * aNormal;
    gl_Position = projection * view * vec4(FragPos, 1.0);
}

// Content/Shaders/basic.frag
#version 330 core
in vec3 FragPos;
in vec3 Normal;

out vec4 FragColor;

uniform vec3 lightDir = vec3(0.5, -1.0, 0.3);
uniform vec3 lightColor = vec3(1.0, 1.0, 1.0);
uniform vec3 objectColor = vec3(0.5, 0.5, 0.5);

void main()
{
    // Simple directional lighting
    vec3 norm = normalize(Normal);
    vec3 lightDirNorm = normalize(-lightDir);
    
    float diff = max(dot(norm, lightDirNorm), 0.0);
    vec3 diffuse = diff * lightColor;
    
    vec3 ambient = 0.3 * lightColor;
    vec3 result = (ambient + diffuse) * objectColor;
    
    FragColor = vec4(result, 1.0);
}
```

## Testing Your Implementation

### Movement Test Checklist
- [ ] Window opens at correct resolution
- [ ] Mouse look works smoothly
- [ ] WASD movement in all directions
- [ ] Diagonal movement is normalized
- [ ] Jump works and feels responsive
- [ ] Gravity applies correctly
- [ ] No stuttering or frame pacing issues

### Performance Targets
- Consistent 60 FPS minimum
- Input latency < 16ms
- No frame time spikes > 33ms
- Memory usage stable (no leaks)

### Debug Overlays

```csharp
// Add debug info rendering
public class DebugOverlay
{
    public static void Draw()
    {
        DrawText($"FPS: {1.0 / deltaTime:F0}", 10, 10);
        DrawText($"Position: {player.Position}", 10, 30);
        DrawText($"Velocity: {player.Velocity}", 10, 50);
        DrawText($"Grounded: {player.IsGrounded}", 10, 70);
    }
}
```

## Common Issues and Solutions

### Issue: Mouse Input Feels Laggy
**Solution**: Ensure raw mouse mode is enabled and poll input before physics update

### Issue: Movement Stutters
**Solution**: Check fixed timestep implementation and interpolation

### Issue: Collision Jitter
**Solution**: Use swept collision detection, not discrete position updates

### Issue: High Memory Usage
**Solution**: Profile allocations, implement object pooling for projectiles

## Optimization Opportunities

### Early (During Prototype)
- Simple frustum culling
- Batch similar draw calls
- Reuse vertex buffers

### Later (When Needed)
- Spatial partitioning for physics
- GPU instancing for enemies
- Texture atlasing
- LOD system

## Extension Points

### Adding Weapons
1. Create `IWeapon` interface
2. Implement specific weapon classes
3. Add to player inventory system
4. Handle weapon switching input

### Adding Enemies
1. Extend Enemy base class
2. Implement AI state machine
3. Add to spawn system
4. Create enemy-specific behaviors

### Adding Roguelike Elements
1. Item system with effects
2. Procedural arena generation
3. Wave-based progression
4. Persistent upgrades

## Resources and References

- [Silk.NET Documentation](https://dotnet.github.io/Silk.NET/)
- [Game Programming Patterns](https://gameprogrammingpatterns.com/)
- [Fix Your Timestep!](https://gafferongames.com/post/fix_your_timestep/)
- [FPS Movement Controllers](https://www.youtube.com/watch?v=oFZOFkkVBgo)

---

*Start with Phase 1, test frequently, and iterate based on feel. The architecture supports gradual enhancement without major refactoring.*