using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Numerics;
using FPSRoguelike.Input;
using FPSRoguelike.Rendering;
using FPSRoguelike.Physics;
using FPSRoguelike.Combat;
using FPSRoguelike.Entities;
using FPSRoguelike.UI;

namespace FPSRoguelike.Core;

public class Game
{
    private IWindow window;
    private GL gl;
    private InputSystem? inputSystem;
    private Renderer? renderer;
    private SimpleUIManager? uiManager;
    private bool showSettingsMenu = false;
    
    // Timing
    private double accumulator = 0.0;
    private const double FIXED_TIMESTEP = 1.0 / 60.0;
    private const int MAX_UPDATES = 5;
    
    // Hitstop constants
    private const float HITSTOP_DURATION = 0.05f; // Brief 50ms pause
    private const float HITSTOP_TIME_SCALE = 0.1f; // 10% speed for impact feel
    
    // Hitstop state
    private float hitstopTime = 0f;
    private bool isInHitstop = false;
    
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
    
    // Camera and movement constants
    private const float PLAYER_START_HEIGHT = 1.7f;
    private const float PLAYER_START_Z = 5f;
    
    // Camera and movement
    private Camera? camera;
    private CharacterController? characterController;
    private Vector3 playerStartPosition = new Vector3(0, PLAYER_START_HEIGHT, PLAYER_START_Z);
    
    // Combat constants
    private const float HIT_MARKER_DURATION = 2.0f;
    private const int MAX_PROJECTILES = 100;
    private const float WAVE_DELAY = 5f;
    private const float PLAYER_RADIUS = 0.5f;
    
    // Combat
    private Weapon? weapon;
    private List<(Vector3 position, float timeRemaining)> hitMarkers = new List<(Vector3, float)>();
    private HashSet<int> destroyedCubes = new HashSet<int>();
    
    // Enemies
    private List<Enemy> enemies = new List<Enemy>();
    private List<Projectile> projectiles = new List<Projectile>();
    private float gameTime = 0f; // Total game time for attack timing
    private int enemiesKilled = 0;
    private int currentWave = 0;
    private float waveTimer = 0f;
    
    // Player
    private PlayerHealth? playerHealth;
    private int score = 0;
    private double fps = 0;
    private double fpsTimer = 0;
    private int frameCount = 0;
    
    // UI
    private Crosshair? crosshair;
    private HUD? hud;
    
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
        
        // Initialize HUD
        hud = new HUD();
        hud.Initialize(gl);
        
        // Initialize player health
        playerHealth = new PlayerHealth(100f);
        
        // Initialize UI Manager
        uiManager = new SimpleUIManager();
        
        // Initialize projectile pool
        for (int i = 0; i < MAX_PROJECTILES; i++)
        {
            projectiles.Add(new Projectile());
        }
        
        // Spawn initial enemies for testing
        SpawnWave(1);
        
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
        Console.WriteLine("  F2 - Spawn more enemies");
        Console.WriteLine("  ESC - Exit");
        Console.WriteLine("\nObjective: Survive and destroy all enemies!");
    }
    
    public void Update(double deltaTime)
    {
        // Update FPS counter
        fpsTimer += deltaTime;
        frameCount++;
        if (fpsTimer >= 1.0)
        {
            fps = frameCount / fpsTimer;
            frameCount = 0;
            fpsTimer = 0;
        }
        
        // Handle UI inputs BEFORE polling (check previous frame's state)
        if (inputSystem?.IsKeyJustPressed(Key.Escape) == true)
        {
            if (uiManager?.IsPaused == true)
            {
                // If already paused and in menu, close menu or unpause
                if (showSettingsMenu)
                {
                    showSettingsMenu = false;
                }
                else
                {
                    uiManager?.TogglePause();
                }
            }
            else
            {
                // If not paused, pause and show menu
                uiManager?.TogglePause();
                showSettingsMenu = true;
            }
            Console.WriteLine($"ESC pressed - Paused: {uiManager?.IsPaused}, Menu: {showSettingsMenu}");
        }
        
        // Handle menu navigation when paused
        if (uiManager?.IsPaused == true && showSettingsMenu)
        {
            // Navigate menu with arrow keys
            if (inputSystem?.IsKeyJustPressed(Key.Up) == true || inputSystem?.IsKeyJustPressed(Key.W) == true)
            {
                hud?.NavigateMenu(-1);
            }
            if (inputSystem?.IsKeyJustPressed(Key.Down) == true || inputSystem?.IsKeyJustPressed(Key.S) == true)
            {
                hud?.NavigateMenu(1);
            }
            
            // Adjust settings with left/right arrows
            if (inputSystem?.IsKeyJustPressed(Key.Left) == true || inputSystem?.IsKeyJustPressed(Key.A) == true)
            {
                hud?.AdjustSelectedSetting(-1);
                // Apply settings if mouse sensitivity or FOV changed
                if (hud != null && camera != null)
                {
                    camera.SetFieldOfView(hud.FieldOfView);
                }
            }
            if (inputSystem?.IsKeyJustPressed(Key.Right) == true || inputSystem?.IsKeyJustPressed(Key.D) == true)
            {
                hud?.AdjustSelectedSetting(1);
                // Apply settings if mouse sensitivity or FOV changed
                if (hud != null && camera != null)
                {
                    camera.SetFieldOfView(hud.FieldOfView);
                }
            }
            
            // Select menu item with Enter
            if (inputSystem?.IsKeyJustPressed(Key.Enter) == true || inputSystem?.IsKeyJustPressed(Key.Space) == true)
            {
                string? action = hud?.GetSelectedAction();
                switch (action)
                {
                    case "Resume":
                        uiManager?.TogglePause();
                        showSettingsMenu = false;
                        break;
                    case "Exit Game":
                        window.Close();
                        break;
                }
            }
        }
        
        if (inputSystem?.IsKeyJustPressed(Key.F1) == true)
        {
            uiManager?.ToggleDebugInfo();
        }
        
        // Poll input for next frame
        inputSystem?.Poll();
        
        // Update UI
        uiManager?.Update(deltaTime);
        
        // Don't update game logic if paused
        if (uiManager?.IsPaused == true) return;
        
        accumulator += deltaTime;
        
        // Handle mouse look every frame (not in fixed timestep) - but disable during hitstop
        if (inputSystem != null && camera != null && !isInHitstop)
        {
            Vector2 mouseDelta = inputSystem.GetMouseDelta();
            
            // Apply mouse sensitivity from HUD settings or UI settings
            float sensitivity = hud?.MouseSensitivity ?? uiManager?.MouseSensitivity ?? 0.3f;
            mouseDelta *= sensitivity;
            
            camera.UpdateRotation(mouseDelta);
        }
        
        int updates = 0;
        while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES)
        {
            // Fixed update
            UpdateGameLogic(FIXED_TIMESTEP);
            
            accumulator -= FIXED_TIMESTEP;
            updates++;
        }
        
        // Exit handled via Alt+F4 or window close button
    }
    
    public void Render(double deltaTime)
    {
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        double interpolation = accumulator / FIXED_TIMESTEP;
        
        // Render 3D scene
        RenderTestCube(interpolation, deltaTime);
        
        // Render UI elements
        if (uiManager?.IsPaused != true)
        {
            // Crosshair is now part of HUD
        }
        
        // Render HUD
        int aliveEnemies = enemies.Count(e => e.IsAlive);
        hud?.Render(playerHealth, weapon, score, currentWave, aliveEnemies, uiManager?.IsPaused ?? false, showSettingsMenu);
        uiManager?.PrintHUD(playerHealth, weapon, score, currentWave, aliveEnemies, (float)fps);
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        if (inputSystem == null || camera == null || characterController == null || playerHealth == null) return;
        
        float dt = (float)fixedDeltaTime;
        
        // Update hitstop timer (always at normal speed)
        UpdateHitstop(dt);
        
        // Apply time scale during hitstop
        float scaledDt = isInHitstop ? dt * HITSTOP_TIME_SCALE : dt;
        
        gameTime += scaledDt; // Track total time for enemy attack cooldowns
        
        // Update player health
        playerHealth.Update(dt);
        
        // Check if player is dead - block input but allow respawn
        if (!playerHealth.IsAlive)
        {
            // Respawn player after a delay
            if (inputSystem.IsKeyJustPressed(Key.R))
            {
                RespawnPlayer();
            }
            return; // Don't process input when dead
        }
        
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
        
        // Handle jump input (use IsKeyPressed for fixed timestep)
        bool jumpPressed = inputSystem.IsKeyPressed(Key.Space);
        
        // Update character controller with physics (apply hitstop to movement too)
        characterController.Update(moveInput, jumpPressed, scaledDt);
        
        // Update camera position to follow player (with eye height offset)
        camera.Position = characterController.Position;
        
        // Update weapon (use scaled time)
        weapon?.Update(scaledDt);
        
        // Handle shooting (disable during hitstop)
        if (inputSystem.IsMouseButtonPressed(MouseButton.Left) && !isInHitstop)
        {
            FireWeapon();
        }
        
        // Update enemies (use scaled time)
        UpdateEnemies(scaledDt);
        
        // Update projectiles (use scaled time)
        UpdateProjectiles(scaledDt);
        
        // Update hit markers (use scaled time)
        UpdateHitMarkers(scaledDt);
        
        // Check for wave completion
        CheckWaveCompletion();
        
        // Update cube rotation for visual interest (use scaled time)
        rotation += scaledDt * 50.0f;
        
        // Debug output
        if (inputSystem.IsKeyJustPressed(Key.F1))
        {
            Console.WriteLine($"Position: {characterController.Position:F2}");
            Console.WriteLine($"Velocity: {characterController.Velocity:F2}");
            Console.WriteLine($"Grounded: {characterController.IsGrounded}");
            Console.WriteLine($"Health: {playerHealth.Health}/{playerHealth.MaxHealth}");
            Console.WriteLine($"Enemies: {enemies.Count(e => e.IsAlive)} | Killed: {enemiesKilled}");
            Console.WriteLine($"Wave: {currentWave}");
        }
        
        // Spawn enemies manually for testing
        if (inputSystem.IsKeyJustPressed(Key.F2))
        {
            SpawnWave(currentWave + 1);
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
    
    private double lastRenderTime = 0;
    
    private void RenderTestCube(double interpolation, double renderDeltaTime)
    {
        if (camera == null) return;
        
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Update camera matrices with FOV from UI settings
        float aspectRatio = (float)window.Size.X / window.Size.Y;
        float fov = uiManager?.FieldOfView ?? 90f;
        camera.UpdateScreenshake((float)renderDeltaTime);
        camera.UpdateMatrices(aspectRatio, fov);
        
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
        
        // Render enemies as tall red cubes
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive) continue;
            
            Matrix4x4 enemyModel = 
                Matrix4x4.CreateScale(1.5f, 2f, 1.5f) * // Taller than wide for visibility
                Matrix4x4.CreateRotationY(enemy.GetYRotation()) * // Face player when attacking
                Matrix4x4.CreateTranslation(enemy.Position);
            
            RenderCube(enemyModel, view, projection, enemy.Color); // Red normally, white when hit
        }
        
        // Render projectiles
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;
            
            Matrix4x4 projectileModel = 
                Matrix4x4.CreateScale(0.3f) *
                Matrix4x4.CreateTranslation(projectile.Position);
            
            // Enemy projectiles are orange, player projectiles are yellow
            Vector3 projectileColor = projectile.IsEnemyProjectile ? 
                new Vector3(1.0f, 0.5f, 0.0f) : new Vector3(1.0f, 1.0f, 0.0f);
            
            RenderCube(projectileModel, view, projection, projectileColor);
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
    
    private void TriggerHitstop()
    {
        hitstopTime = HITSTOP_DURATION;
        isInHitstop = true;
    }
    
    private void UpdateHitstop(float deltaTime)
    {
        if (hitstopTime > 0)
        {
            hitstopTime -= deltaTime;
            if (hitstopTime <= 0)
            {
                hitstopTime = 0;
                isInHitstop = false;
            }
        }
    }
    
    private void SpawnWave(int wave)
    {
        currentWave = wave;
        waveTimer = 0f;
        
        int enemiesToSpawn = 3 + (wave * 2); // Scale difficulty: 5, 7, 9, 11...
        Console.WriteLine($"\n=== WAVE {wave} === Spawning {enemiesToSpawn} enemies!");
        
        Random rand = new Random();
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // Spawn enemies in a circle around the player at random distances
            float angle = (float)(rand.NextDouble() * Math.PI * 2);
            float distance = 10f + (float)(rand.NextDouble() * 10f); // 10-20 units away
            
            Vector3 spawnPos = new Vector3(
                characterController!.Position.X + MathF.Sin(angle) * distance,
                1f,
                characterController.Position.Z + MathF.Cos(angle) * distance
            );
            
            var enemy = new Enemy(spawnPos, 30f + (wave * 10f)); // Health scales: 40, 50, 60...
            enemies.Add(enemy);
        }
    }
    
    private void UpdateEnemies(float deltaTime)
    {
        if (characterController == null || camera == null) return;
        
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive) continue;
            
            // Update AI state machine and movement
            enemy.Update(deltaTime, characterController.Position);
            
            // Check if enemy is ready to fire (in attack state + cooldown expired)
            if (enemy.CanAttack(gameTime))
            {
                FireEnemyProjectile(enemy);
            }
        }
    }
    
    private void UpdateProjectiles(float deltaTime)
    {
        if (characterController == null || playerHealth == null) return;
        
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;
            
            projectile.Update(deltaTime); // Move projectile and check lifetime
            
            if (projectile.IsEnemyProjectile)
            {
                // Check collision with player hitbox
                if (projectile.CheckCollision(characterController.Position, PLAYER_RADIUS))
                {
                    playerHealth.TakeDamage(10f);
                    projectile.Deactivate(); // Return to pool
                }
            }
            else
            {
                // Check collision with enemies
                foreach (var enemy in enemies)
                {
                    if (!enemy.IsActive) continue;
                    
                    if (projectile.CheckCollision(enemy.Position, 1f))
                    {
                        enemy.TakeDamage(weapon?.Damage ?? 10f);
                        projectile.Deactivate();
                        
                        // Trigger hitstop on enemy hit
                        TriggerHitstop();
                        
                        if (!enemy.IsAlive)
                        {
                            enemiesKilled++;
                            score += 100;
                            Console.WriteLine($"Enemy destroyed! Total kills: {enemiesKilled}");
                        }
                        break;
                    }
                }
            }
        }
    }
    
    private void FireWeapon()
    {
        if (weapon == null || camera == null || !weapon.CanFire()) return;
        
        // Trigger screenshake when firing
        camera.TriggerScreenshake();
        
        // Spawn a projectile for the player's shot (projectile-based, not hitscan)
        var projectile = projectiles.FirstOrDefault(p => !p.IsActive);
        if (projectile != null)
        {
            Vector3 origin = camera.Position;
            Vector3 direction = camera.GetForwardVector();
            projectile.Fire(origin, direction, 50f, weapon.Damage, fromEnemy: false);
        }
        
        Console.WriteLine($"[{weapon.Name}] Fired!");
    }
    
    private void FireEnemyProjectile(Enemy enemy)
    {
        // Find an inactive projectile to use
        var projectile = projectiles.FirstOrDefault(p => !p.IsActive);
        if (projectile != null)
        {
            Vector3 origin = enemy.Position + new Vector3(0, 0.5f, 0); // Shoot from middle of enemy
            Vector3 direction = enemy.GetAttackDirection();
            
            projectile.Fire(origin, direction, enemy.GetProjectileSpeed(), enemy.GetDamage(), true);
            Console.WriteLine($"Enemy {enemy.Id} fired projectile!");
        }
    }
    
    private void CheckWaveCompletion()
    {
        // Check if all enemies are dead
        if (enemies.Count == 0 || enemies.All(e => !e.IsAlive))
        {
            waveTimer += (float)FIXED_TIMESTEP;
            
            // Wait for delay before spawning next wave
            if (waveTimer >= WAVE_DELAY && enemies.Count > 0)
            {
                // Clear dead enemies from list
                enemies.RemoveAll(e => !e.IsAlive);
                
                // Spawn next wave with increased difficulty
                SpawnWave(currentWave + 1);
            }
        }
    }
    
    private void RespawnPlayer()
    {
        if (characterController == null || playerHealth == null) return;
        
        characterController.Position = playerStartPosition;
        characterController.Velocity = Vector3.Zero;
        playerHealth.Respawn();
        
        // Clear all projectiles
        foreach (var projectile in projectiles)
        {
            projectile.Deactivate();
        }
        
        Console.WriteLine("Player respawned! Press R to respawn when dead.");
    }
}