using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Numerics;
using FPSRoguelike.Input;
using FPSRoguelike.Rendering;
using FPSRoguelike.Physics;
using FPSRoguelike.Combat;

namespace FPSRoguelike.Core;

public class Game
{
    private IWindow window;
    private GL gl;
    private InputSystem? inputSystem;
    private Renderer? renderer;
    
    // Timing
    private double accumulator = 0.0;
    private const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const int MAX_UPDATES = 5;
    
    // Test cube vertices - need 24 vertices (4 per face) for proper normals
    private readonly float[] vertices = 
    {
        // Position           // Normal
        // Front face (z = 0.5)
        -0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f, -0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        -0.5f,  0.5f,  0.5f,  0.0f,  0.0f,  1.0f,
        
        // Back face (z = -0.5)
        -0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f, -0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  0.0f, -1.0f,
        
        // Top face (y = 0.5)
        -0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  0.0f,  1.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
        -0.5f,  0.5f, -0.5f,  0.0f,  1.0f,  0.0f,
        
        // Bottom face (y = -0.5)
        -0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f,  0.5f,  0.0f, -1.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
        -0.5f, -0.5f, -0.5f,  0.0f, -1.0f,  0.0f,
        
        // Right face (x = 0.5)
         0.5f, -0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
         0.5f, -0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f, -0.5f,  1.0f,  0.0f,  0.0f,
         0.5f,  0.5f,  0.5f,  1.0f,  0.0f,  0.0f,
        
        // Left face (x = -0.5)
        -0.5f, -0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f, -0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f, -0.5f, -1.0f,  0.0f,  0.0f,
        -0.5f,  0.5f,  0.5f, -1.0f,  0.0f,  0.0f,
    };
    
    private readonly uint[] indices = 
    {
        // Front face
        0, 1, 2,    0, 2, 3,
        
        // Back face
        4, 6, 5,    4, 7, 6,
        
        // Top face
        8, 9, 10,   8, 10, 11,
        
        // Bottom face
        12, 14, 13, 12, 15, 14,
        
        // Right face
        16, 17, 18, 16, 18, 19,
        
        // Left face
        20, 22, 21, 20, 23, 22
    };
    
    private uint vao, vbo, ebo;
    private uint shaderProgram;
    
    // Camera and movement
    private Camera? camera;
    private CharacterController? characterController;
    private Vector3 playerStartPosition = new Vector3(0, 1.7f, 5f);
    
    // Combat
    private Weapon? weapon;
    private List<(Vector3 position, float timeRemaining)> hitMarkers = new List<(Vector3, float)>();
    private const float HIT_MARKER_DURATION = 2.0f; // Longer duration
    private HashSet<int> destroyedCubes = new HashSet<int>();
    
    // UI
    private Crosshair? crosshair;
    
    public Game(IWindow window, GL gl)
    {
        this.window = window;
        this.gl = gl;
    }
    
    public void Initialize()
    {
        // Initialize OpenGL settings
        gl.Enable(EnableCap.DepthTest);
        gl.ClearColor(0.1f, 0.1f, 0.2f, 1.0f);
        
        // Initialize systems
        inputSystem = new InputSystem();
        inputSystem.Initialize(window);
        
        renderer = new Renderer();
        renderer.Initialize(gl, window.Size.X, window.Size.Y);
        
        // Setup test geometry
        SetupTestCube();
        
        // Initialize camera and character controller
        characterController = new CharacterController(playerStartPosition);
        camera = new Camera(characterController.Position);
        
        // Initialize weapon
        weapon = new Weapon();
        
        // Initialize crosshair
        crosshair = new Crosshair(gl);
        
        // Lock cursor for FPS
        var input = window.CreateInput();
        if (input.Mice.Count > 0)
        {
            var mouse = input.Mice[0];
            mouse.Cursor.CursorMode = CursorMode.Raw;
        }
        
        Console.WriteLine("Game initialized!");
        Console.WriteLine("Controls:");
        Console.WriteLine("  WASD - Move");
        Console.WriteLine("  Mouse - Look around");
        Console.WriteLine("  Space - Jump");
        Console.WriteLine("  Left Mouse - Shoot");
        Console.WriteLine("  F1 - Debug info");
        Console.WriteLine("  ESC - Exit");
    }
    
    public void Update(double deltaTime)
    {
        accumulator += deltaTime;
        
        int updates = 0;
        while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES)
        {
            // Fixed update
            inputSystem?.Poll();
            UpdateGameLogic(FIXED_TIMESTEP);
            
            accumulator -= FIXED_TIMESTEP;
            updates++;
        }
        
        // Check for exit
        if (inputSystem?.IsKeyPressed(Key.Escape) == true)
        {
            window.Close();
        }
    }
    
    public void Render(double deltaTime)
    {
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        double interpolation = accumulator / FIXED_TIMESTEP;
        
        // Render 3D scene
        RenderTestCube(interpolation);
        
        // Render UI elements (crosshair)
        crosshair?.Render(window.Size.X, window.Size.Y);
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        if (inputSystem == null || camera == null || characterController == null) return;
        
        float dt = (float)fixedDeltaTime;
        
        // Handle mouse look
        Vector2 mouseDelta = inputSystem.GetMouseDelta();
        camera.UpdateRotation(mouseDelta);
        
        // Handle WASD movement input
        Vector3 moveInput = Vector3.Zero;
        
        if (inputSystem.IsKeyPressed(Key.W))
            moveInput += camera.GetForwardMovement();
        if (inputSystem.IsKeyPressed(Key.S))
            moveInput -= camera.GetForwardMovement();
        if (inputSystem.IsKeyPressed(Key.D))
            moveInput += camera.GetRightMovement();
        if (inputSystem.IsKeyPressed(Key.A))
            moveInput -= camera.GetRightMovement();
        
        // Normalize diagonal movement
        if (moveInput.LengthSquared() > 0)
            moveInput = Vector3.Normalize(moveInput);
        
        // Handle jump input
        bool jumpPressed = inputSystem.IsKeyJustPressed(Key.Space);
        
        // Update character controller with physics
        characterController.Update(moveInput, jumpPressed, dt);
        
        // Update camera position to follow player (with eye height offset)
        camera.Position = characterController.Position;
        
        // Update weapon
        weapon?.Update(dt);
        
        // Handle shooting
        if (inputSystem.IsMouseButtonPressed(MouseButton.Left))
        {
            weapon?.Fire(
                camera.Position,
                camera.GetForwardVector(),
                hit => OnWeaponHit(hit),
                destroyedCubes
            );
        }
        
        // Update hit markers
        UpdateHitMarkers(dt);
        
        // Update cube rotation for visual interest
        rotation += dt * 50.0f;
        
        // Debug output
        if (inputSystem.IsKeyJustPressed(Key.F1))
        {
            Console.WriteLine($"Position: {characterController.Position:F2}");
            Console.WriteLine($"Velocity: {characterController.Velocity:F2}");
            Console.WriteLine($"Grounded: {characterController.IsGrounded}");
        }
    }
    
    private void SetupTestCube()
    {
        // Create and bind VAO
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
        // Create and bind VBO
        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        
        unsafe
        {
            fixed (float* v = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, 
                    (nuint)(vertices.Length * sizeof(float)), 
                    v, 
                    BufferUsageARB.StaticDraw);
            }
        }
        
        // Create and bind EBO
        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        
        unsafe
        {
            fixed (uint* i = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, 
                    (nuint)(indices.Length * sizeof(uint)), 
                    i, 
                    BufferUsageARB.StaticDraw);
            }
        }
        
        // Setup vertex attributes
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), null);
            gl.EnableVertexAttribArray(0);
            
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
        }
        
        // Create simple shaders
        CreateSimpleShader();
    }
    
    private void CreateSimpleShader()
    {
        // Simple vertex shader with lighting
        string vertexShaderSource = @"
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
}";

        // Fragment shader with improved lighting for better edge definition
        string fragmentShaderSource = @"
#version 330 core
in vec3 FragPos;
in vec3 Normal;
out vec4 FragColor;

uniform vec3 objectColor = vec3(0.5, 0.6, 0.7);

void main()
{
    // Directional light from above-right
    vec3 lightDir = normalize(vec3(-0.5, -1.0, -0.3));
    vec3 lightColor = vec3(1.0, 1.0, 1.0);
    
    // Ambient lighting
    float ambientStrength = 0.4;
    vec3 ambient = ambientStrength * lightColor * objectColor;
    
    // Diffuse lighting
    vec3 norm = normalize(Normal);
    float diff = max(dot(norm, -lightDir), 0.0);
    vec3 diffuse = diff * lightColor * objectColor;
    
    // Simple specular for slight shine
    vec3 viewDir = normalize(-FragPos);
    vec3 reflectDir = reflect(lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = 0.3 * spec * lightColor;
    
    vec3 result = ambient + diffuse + specular;
    FragColor = vec4(result, 1.0);
}";

        // Compile vertex shader
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexShaderSource);
        gl.CompileShader(vertexShader);
        
        // Check compilation
        string infoLog = gl.GetShaderInfoLog(vertexShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Vertex shader compilation: {infoLog}");
        }
        
        // Compile fragment shader
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentShaderSource);
        gl.CompileShader(fragmentShader);
        
        infoLog = gl.GetShaderInfoLog(fragmentShader);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Fragment shader compilation: {infoLog}");
        }
        
        // Link shaders
        shaderProgram = gl.CreateProgram();
        gl.AttachShader(shaderProgram, vertexShader);
        gl.AttachShader(shaderProgram, fragmentShader);
        gl.LinkProgram(shaderProgram);
        
        infoLog = gl.GetProgramInfoLog(shaderProgram);
        if (!string.IsNullOrWhiteSpace(infoLog))
        {
            Console.WriteLine($"Shader linking: {infoLog}");
        }
        
        // Clean up
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
    
    private float rotation = 0.0f;
    
    // Cube positions for the scene
    private readonly Vector3[] cubePositions = new Vector3[]
    {
        new Vector3(0, 1, 0),      // Center floating cube
        new Vector3(5, 1, 5),      // Corner cubes
        new Vector3(-5, 1, 5),
        new Vector3(5, 1, -5),
        new Vector3(-5, 1, -5),
        new Vector3(10, 2, 0),     // Distant cubes
        new Vector3(-10, 2, 0),
        new Vector3(0, 2, 10),
        new Vector3(0, 2, -10),
    };
    
    private void RenderTestCube(double interpolation)
    {
        if (camera == null) return;
        
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Update camera matrices
        float aspectRatio = (float)window.Size.X / window.Size.Y;
        camera.UpdateMatrices(aspectRatio);
        
        Matrix4x4 view = camera.ViewMatrix;
        Matrix4x4 projection = camera.ProjectionMatrix;
        
        // Render ground plane (large flat cube)
        RenderCube(
            Matrix4x4.CreateScale(50f, 0.1f, 50f) * 
            Matrix4x4.CreateTranslation(0, -0.05f, 0),
            view, projection
        );
        
        // Render multiple cubes at different positions
        for (int i = 0; i < cubePositions.Length; i++)
        {
            // Skip destroyed cubes
            if (destroyedCubes.Contains(i)) continue;
            
            // Animate only the first cube
            float currentRotation = i == 0 ? rotation : 45f * i;
            
            Matrix4x4 model = 
                Matrix4x4.CreateRotationY(currentRotation * (float)(Math.PI / 180.0)) *
                Matrix4x4.CreateRotationX(currentRotation * 0.5f * (float)(Math.PI / 180.0)) *
                Matrix4x4.CreateTranslation(cubePositions[i]);
            
            RenderCube(model, view, projection);
        }
        
        // Render hit markers (red and pulsing)
        foreach (var (hitPos, timeRemaining) in hitMarkers)
        {
            // Pulse effect based on time remaining
            float scale = 0.3f + (0.1f * MathF.Sin(timeRemaining * 10f));
            Matrix4x4 hitModel = 
                Matrix4x4.CreateScale(scale) *
                Matrix4x4.CreateTranslation(hitPos);
            // Render in red
            RenderCube(hitModel, view, projection, new Vector3(1.0f, 0.2f, 0.2f));
        }
    }
    
    private void RenderCube(Matrix4x4 model, Matrix4x4 view, Matrix4x4 projection, Vector3? color = null)
    {
        unsafe
        {
            int modelLoc = gl.GetUniformLocation(shaderProgram, "model");
            int viewLoc = gl.GetUniformLocation(shaderProgram, "view");
            int projLoc = gl.GetUniformLocation(shaderProgram, "projection");
            int colorLoc = gl.GetUniformLocation(shaderProgram, "objectColor");
            
            gl.UniformMatrix4(modelLoc, 1, false, (float*)&model);
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
            
            // Set color (default blue-gray or custom color)
            Vector3 cubeColor = color ?? new Vector3(0.5f, 0.6f, 0.7f);
            gl.Uniform3(colorLoc, cubeColor.X, cubeColor.Y, cubeColor.Z);
            
            // Draw
            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
    
    public void Cleanup()
    {
        crosshair?.Cleanup();
        gl.DeleteVertexArray(vao);
        gl.DeleteBuffer(vbo);
        gl.DeleteBuffer(ebo);
        gl.DeleteProgram(shaderProgram);
        
        Console.WriteLine("Game cleaned up!");
    }
    
    private void OnWeaponHit(RaycastHit hit)
    {
        Console.WriteLine($"HIT! Target at {hit.Target:F2}, Distance: {hit.Distance:F1}");
        
        // Add visual hit marker with duration
        hitMarkers.Add((hit.Point, HIT_MARKER_DURATION));
        
        // Mark the cube as destroyed
        var targetIndex = Array.FindIndex(cubePositions, pos => Vector3.Distance(pos, hit.Target) < 0.1f);
        if (targetIndex >= 0 && targetIndex < cubePositions.Length)
        {
            destroyedCubes.Add(targetIndex);
            Console.WriteLine($"Destroyed cube {targetIndex}!");
        }
    }
    
    private void UpdateHitMarkers(float deltaTime)
    {
        // Update hit marker timers and remove expired ones
        for (int i = hitMarkers.Count - 1; i >= 0; i--)
        {
            var (pos, timeRemaining) = hitMarkers[i];
            timeRemaining -= deltaTime;
            
            if (timeRemaining <= 0)
            {
                hitMarkers.RemoveAt(i);
            }
            else
            {
                hitMarkers[i] = (pos, timeRemaining);
            }
        }
    }
}