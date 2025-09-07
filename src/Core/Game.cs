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
using FPSRoguelike.Environment;

namespace FPSRoguelike.Core;

public class Game : IDisposable
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
    private const int MAX_UPDATES = 10; // Increased to prevent spiral of death
    
    // Removed global hitstop - now handled per-enemy
    
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
    
    // GPU Instancing
    private uint instancedShaderProgram;
    private uint enemyInstanceVBO;
    private uint projectileInstanceVBO;
    private const int MAX_ENEMY_INSTANCES = 30;  // Support up to 30 enemies
    private Matrix4x4[] enemyInstanceData = new Matrix4x4[MAX_ENEMY_INSTANCES];
    private Vector3[] enemyInstanceColors = new Vector3[MAX_ENEMY_INSTANCES];
    private Matrix4x4[] projectileInstanceData = new Matrix4x4[MAX_PROJECTILES];
    private Vector3[] projectileInstanceColors = new Vector3[MAX_PROJECTILES];
    
    // Camera and movement constants
    private const float PLAYER_START_HEIGHT = 1.7f;
    private const float PLAYER_START_Z = -10f;
    
    // Camera and movement
    private Camera? camera;
    private CharacterController? characterController;
    private Vector3 playerStartPosition = new Vector3(0, PLAYER_START_HEIGHT, PLAYER_START_Z);
    
    // Combat constants
    private const int MAX_PROJECTILES = 500;
    private const float PLAYER_RADIUS = 0.5f;
    private const float BOSS_HEALTH = 500f;  // Single powerful boss
    private const float BOSS_SIZE = 3f;  // Boss is much larger
    
    // Combat
    private Revolver? revolver;
    private Katana? katana;
    private SMG? smg;
    private SlashEffect? slashEffect;
    private int currentWeaponIndex = 0; // 0 = revolver, 1 = katana, 2 = SMG
    
    // Boss and combat
    private List<Enemy> enemies = new List<Enemy>();  // Using list for compatibility, but only one boss
    private List<Projectile> projectiles = new List<Projectile>();
    private List<Obstacle> obstacles = new List<Obstacle>();
    private float gameTime = 0f; // Total game time for attack timing
    private int enemiesKilled = 0;
    
    // Player
    private PlayerHealth? playerHealth;
    private int score = 0;
    private double fps = 0;
    private double fpsTimer = 0;
    private int frameCount = 0;
    private float respawnCooldown = 0f;
    
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
        
        // Initialize weapons
        revolver = new Revolver();
        katana = new Katana();
        smg = new SMG();
        slashEffect = new SlashEffect(gl);
        
        // Create initial obstacles for the level
        GenerateObstacles();
        
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
        
        // Spawn the boss
        SpawnBoss();
        
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
        Console.WriteLine("  Left Mouse - Shoot/Slash");
        Console.WriteLine("  1 - Katana");
        Console.WriteLine("  2 - Revolver");
        Console.WriteLine("  R - Reload revolver");
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
            fps = fpsTimer > 0 ? frameCount / fpsTimer : 0;
            frameCount = 0;
            fpsTimer = 0;
        }
        
        // Handle UI inputs
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
        
        // F1 - Toggle debug info and print to console
        if (inputSystem?.IsKeyJustPressed(Key.F1) == true)
        {
            uiManager?.ToggleDebugInfo();
            
            // Also print debug info to console
            if (characterController != null && playerHealth != null)
            {
                Console.WriteLine($"\n=== DEBUG INFO ===");
                Console.WriteLine($"Position: {characterController.Position:F2}");
                Console.WriteLine($"Velocity: {characterController.Velocity:F2}");
                Console.WriteLine($"Grounded: {characterController.IsGrounded}");
                Console.WriteLine($"Health: {playerHealth.Health}/{playerHealth.MaxHealth}");
                Console.WriteLine($"Boss Active: {(enemies.Count(e => e.IsAlive) > 0 ? "Yes" : "No")} | Total Kills: {enemiesKilled}");
                Console.WriteLine($"==================\n");
            }
        }
        
        // Update UI
        uiManager?.Update(deltaTime);
        
        // Update respawn cooldown
        if (respawnCooldown > 0)
            respawnCooldown -= (float)deltaTime;
        
        // Check for respawn input (works even when dead)
        if (playerHealth?.IsAlive == false && respawnCooldown <= 0)
        {
            // Debug: Check both pressed and just pressed states
            bool rPressed = inputSystem?.IsKeyPressed(Key.R) == true;
            bool rJustPressed = inputSystem?.IsKeyJustPressed(Key.R) == true;
            
            if (rJustPressed || rPressed)  // Accept either state for respawn
            {
                RespawnPlayer();
                respawnCooldown = 0.5f; // Half second cooldown to prevent double-respawn
                Console.WriteLine($"Player respawned! (JustPressed: {rJustPressed}, Pressed: {rPressed})");
            }
        }
        
        // Quick respawn - press F5 to instantly respawn even when alive
        if (inputSystem?.IsKeyJustPressed(Key.F5) == true)
        {
            RespawnPlayer();
            Console.WriteLine("Player respawned!");
        }
        
        // Respawn boss for testing - F2
        if (inputSystem?.IsKeyJustPressed(Key.F2) == true)
        {
            SpawnBoss();
            Console.WriteLine("Boss spawned!");
        }
        
        // Only update game when not paused
        if (uiManager?.IsPaused != true)
        {
            accumulator += deltaTime;
            
            // Handle mouse look every frame (not in fixed timestep)
            if (inputSystem != null && camera != null)
            {
                Vector2 mouseDelta = inputSystem.GetMouseDelta();
                
                // Apply mouse sensitivity from HUD settings or UI settings
                float sensitivity = hud?.MouseSensitivity ?? uiManager?.MouseSensitivity ?? 0.3f;
                mouseDelta *= sensitivity;
                
                camera.UpdateRotation(mouseDelta);
            }
            
            // Removed respawn check from here - now only handled in fixed timestep to prevent double-triggering
            
            int updates = 0;
            while (accumulator >= FIXED_TIMESTEP && updates < MAX_UPDATES)
            {
                // Fixed update
                UpdateGameLogic(FIXED_TIMESTEP);
                
                accumulator -= FIXED_TIMESTEP;
                updates++;
            }
        }
        
        // Poll input at end of frame to capture state changes for next frame's edge detection
        inputSystem?.Poll();
        
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
        // Pass 0 for wave since we don't use waves anymore - just boss fights
        Weapon? currentWeapon = currentWeaponIndex switch
        {
            0 => revolver,
            1 => katana,
            2 => smg,
            _ => revolver
        };
        hud?.Render(playerHealth, currentWeapon, score, 0, aliveEnemies, uiManager?.IsPaused ?? false, showSettingsMenu);
        uiManager?.PrintHUD(playerHealth, currentWeapon, score, 0, aliveEnemies, (float)fps);
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        if (inputSystem == null || camera == null || characterController == null || playerHealth == null) return;
        
        float dt = (float)fixedDeltaTime;
        
        gameTime += dt; // Track total time for enemy attack cooldowns
        
        // Update player health
        playerHealth.Update(dt);
        
        // Check if player is dead - block input
        if (!playerHealth.IsAlive)
        {
            return; // Don't process input when dead (respawn is handled in main Update)
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
        
        // Update character controller with physics and collision
        characterController.Update(moveInput, jumpPressed, dt, obstacles);
        
        // Update camera position to follow player (with eye height offset)
        camera.Position = characterController.Position;
        
        // Update weapons (use scaled time)
        revolver?.Update(dt);
        katana?.Update(dt);
        smg?.Update(dt);
        
        // Handle weapon switching with number keys
        if (inputSystem.IsKeyPressed(Key.Number1))
        {
            SelectWeapon(1); // Katana
        }
        else if (inputSystem.IsKeyPressed(Key.Number2))
        {
            SelectWeapon(0); // Revolver
        }
        else if (inputSystem.IsKeyPressed(Key.Number3))
        {
            SelectWeapon(2); // SMG
        }
        
        // Handle manual reload
        if (inputSystem.IsKeyPressed(Key.R))
        {
            if (currentWeaponIndex == 0)
        {
                revolver?.StartReload();
            }
            else if (currentWeaponIndex == 2)
            {
                smg?.StartReload();
            }
        }
        
        // Handle shooting/slashing
        if (inputSystem.IsMouseButtonPressed(MouseButton.Left))
        {
            FireWeapon();
        }
        
        // Update enemies (use normal time - enemies handle their own hitstop)
        UpdateEnemies(dt);
        
        // Update projectiles (use normal time)
        UpdateProjectiles(dt);
        
        // Check if boss is defeated
        CheckBossDefeat(dt);
        
        
        // Debug output moved to main Update for responsiveness
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
        
        // Create instanced shader for enemies and projectiles
        CreateInstancedShader();
        
        // Setup instance buffers
        SetupInstanceBuffers();
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
        
        // Check vertex shader compilation
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexSuccess);
        if (vertexSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Vertex shader compilation failed: {infoLog}");
        }
        
        // Compile fragment shader
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentShaderSource);
        gl.CompileShader(fragmentShader);
        
        // Check fragment shader compilation
        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentSuccess);
        if (fragmentSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Fragment shader compilation failed: {infoLog}");
        }
        
        // Link shaders
        shaderProgram = gl.CreateProgram();
        gl.AttachShader(shaderProgram, vertexShader);
        gl.AttachShader(shaderProgram, fragmentShader);
        gl.LinkProgram(shaderProgram);
        
        // Check shader program linking
        gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out int linkSuccess);
        if (linkSuccess == 0)
        {
            string infoLog = gl.GetProgramInfoLog(shaderProgram);
            throw new Exception($"Shader linking failed: {infoLog}");
        }
        
        // Clean up
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
    
    private void CreateInstancedShader()
    {
        // Instanced vertex shader
        string instancedVertexShader = @"
#version 330 core
layout (location = 0) in vec3 aPos;
layout (location = 1) in vec3 aNormal;
layout (location = 2) in mat4 instanceMatrix;  // Takes locations 2,3,4,5
layout (location = 6) in vec3 instanceColor;

out vec3 FragPos;
out vec3 Normal;
out vec3 InstanceColor;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    FragPos = vec3(instanceMatrix * vec4(aPos, 1.0));
    Normal = mat3(transpose(inverse(instanceMatrix))) * aNormal;
    InstanceColor = instanceColor;
    gl_Position = projection * view * vec4(FragPos, 1.0);
}";

        // Instanced fragment shader
        string instancedFragmentShader = @"
#version 330 core
in vec3 FragPos;
in vec3 Normal;
in vec3 InstanceColor;
out vec4 FragColor;

void main()
{
    // Same lighting as regular shader
    vec3 lightDir = normalize(vec3(-0.5, -1.0, -0.3));
    vec3 lightColor = vec3(1.0, 1.0, 1.0);
    
    float ambientStrength = 0.4;
    vec3 ambient = ambientStrength * lightColor * InstanceColor;
    
    vec3 norm = normalize(Normal);
    float diff = max(dot(norm, -lightDir), 0.0);
    vec3 diffuse = diff * lightColor * InstanceColor;
    
    vec3 viewDir = normalize(-FragPos);
    vec3 reflectDir = reflect(lightDir, norm);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
    vec3 specular = 0.3 * spec * lightColor;
    
    vec3 result = ambient + diffuse + specular;
    FragColor = vec4(result, 1.0);
}";

        // Compile instanced vertex shader
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, instancedVertexShader);
        gl.CompileShader(vertexShader);
        
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexSuccess);
        if (vertexSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Instanced vertex shader compilation failed: {infoLog}");
        }
        
        // Compile instanced fragment shader
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, instancedFragmentShader);
        gl.CompileShader(fragmentShader);
        
        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentSuccess);
        if (fragmentSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Instanced fragment shader compilation failed: {infoLog}");
        }
        
        // Link instanced shader program
        instancedShaderProgram = gl.CreateProgram();
        gl.AttachShader(instancedShaderProgram, vertexShader);
        gl.AttachShader(instancedShaderProgram, fragmentShader);
        gl.LinkProgram(instancedShaderProgram);
        
        gl.GetProgram(instancedShaderProgram, ProgramPropertyARB.LinkStatus, out int linkSuccess);
        if (linkSuccess == 0)
        {
            string infoLog = gl.GetProgramInfoLog(instancedShaderProgram);
            throw new Exception($"Instanced shader linking failed: {infoLog}");
        }
        
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
    
    private void SetupInstanceBuffers()
    {
        // Create enemy instance VBO
        enemyInstanceVBO = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, enemyInstanceVBO);
        // Allocate buffer for matrices + colors (16 floats for matrix + 3 for color = 19 floats per instance)
        unsafe
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, 
                (nuint)(MAX_ENEMY_INSTANCES * (16 + 3) * sizeof(float)), 
                null, 
                BufferUsageARB.DynamicDraw);
        }
        
        // Create projectile instance VBO
        projectileInstanceVBO = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, projectileInstanceVBO);
        unsafe
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, 
                (nuint)(MAX_PROJECTILES * (16 + 3) * sizeof(float)), 
                null, 
                BufferUsageARB.DynamicDraw);
        }
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
    
    
    
    private void RenderTestCube(double interpolation, double renderDeltaTime)
    {
        if (camera == null) return;
        
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Update camera matrices with FOV from UI settings
        float aspectRatio = window.Size.Y > 0 ? (float)window.Size.X / window.Size.Y : 16.0f / 9.0f;
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
        
        // Render obstacles
        foreach (var obstacle in obstacles)
        {
            if (obstacle.IsDestroyed) continue;
            
            Matrix4x4 obstacleModel = Matrix4x4.CreateScale(obstacle.Size) *
                                      Matrix4x4.CreateRotationY(obstacle.Rotation) *
                                      Matrix4x4.CreateTranslation(obstacle.Position);
            RenderCube(obstacleModel, view, projection, obstacle.Color);
        }
        
        // Render all enemies in a single draw call using GPU instancing
        RenderEnemiesInstanced(view, projection);
        
        // Render all projectiles in a single draw call using GPU instancing
        RenderProjectilesInstanced(view, projection);
        
        // Render katana slash effect
        if (katana?.IsSlashing == true && slashEffect != null)
        {
            var slashPoints = katana.GetSlashVisualizationPoints();
            slashEffect.Render(slashPoints, katana.SlashProgress, view, projection);
        }
    }
    
    private void RenderEnemiesInstanced(Matrix4x4 view, Matrix4x4 projection)
    {
        // Prepare instance data for all active enemies
        int instanceCount = 0;
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive || instanceCount >= MAX_ENEMY_INSTANCES) continue;
            
            // Build transformation matrix for this enemy
            // Boss enemies are larger
            float scaleX = enemy is Boss ? 3f : 1.5f;
            float scaleY = enemy is Boss ? 4f : 2f;
            float scaleZ = enemy is Boss ? 3f : 1.5f;
            
            enemyInstanceData[instanceCount] = 
                Matrix4x4.CreateScale(scaleX, scaleY, scaleZ) *
                Matrix4x4.CreateRotationY(enemy.GetYRotation()) *
                Matrix4x4.CreateTranslation(enemy.Position);
            
            // Set color (red normally, white when hit)
            enemyInstanceColors[instanceCount] = enemy.Color;
            instanceCount++;
        }
        
        if (instanceCount == 0) return; // No enemies to render
        
        // Update instance buffer with enemy data
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, enemyInstanceVBO);
        unsafe
        {
            // Create combined data array (matrix + color for each instance)
            float[] combinedData = new float[instanceCount * 19];
            for (int i = 0; i < instanceCount; i++)
            {
                // Copy matrix (16 floats)
                for (int j = 0; j < 16; j++)
                {
                    combinedData[i * 19 + j] = GetMatrixValue(enemyInstanceData[i], j);
                }
                // Copy color (3 floats)
                combinedData[i * 19 + 16] = enemyInstanceColors[i].X;
                combinedData[i * 19 + 17] = enemyInstanceColors[i].Y;
                combinedData[i * 19 + 18] = enemyInstanceColors[i].Z;
            }
            
            fixed (float* dataPtr = combinedData)
            {
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, 
                    (nuint)(instanceCount * 19 * sizeof(float)), dataPtr);
            }
        }
        
        // Setup instanced rendering
        gl.UseProgram(instancedShaderProgram);
        gl.BindVertexArray(vao);
        
        // Setup instance attributes
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, enemyInstanceVBO);
        
        unsafe
        {
            // Instance matrix (locations 2,3,4,5)
            for (uint i = 0; i < 4; i++)
            {
                gl.EnableVertexAttribArray(2 + i);
                gl.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, 
                    19 * sizeof(float), (void*)((i * 4) * sizeof(float)));
                gl.VertexAttribDivisor(2 + i, 1);
            }
            
            // Instance color (location 6)
            gl.EnableVertexAttribArray(6);
            gl.VertexAttribPointer(6, 3, VertexAttribPointerType.Float, false, 
                19 * sizeof(float), (void*)(16 * sizeof(float)));
            gl.VertexAttribDivisor(6, 1);
        }
        
        // Set uniforms
        unsafe
        {
            int viewLoc = gl.GetUniformLocation(instancedShaderProgram, "view");
            int projLoc = gl.GetUniformLocation(instancedShaderProgram, "projection");
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
        }
        
        // Draw all enemies in one call!
        unsafe
        {
            gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)indices.Length, 
                DrawElementsType.UnsignedInt, null, (uint)instanceCount);
        }
        
        // Clean up divisors
        for (uint i = 2; i <= 6; i++)
        {
            gl.VertexAttribDivisor(i, 0);
            gl.DisableVertexAttribArray(i);
        }
    }
    
    private void RenderProjectilesInstanced(Matrix4x4 view, Matrix4x4 projection)
    {
        // Prepare instance data for all active projectiles
        int instanceCount = 0;
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive || instanceCount >= MAX_PROJECTILES) continue;
            
            // Build transformation matrix for this projectile
            projectileInstanceData[instanceCount] = 
                Matrix4x4.CreateScale(0.3f) *
                Matrix4x4.CreateTranslation(projectile.Position);
            
            // Set color (orange for enemy, yellow for player)
            projectileInstanceColors[instanceCount] = projectile.IsEnemyProjectile ? 
                new Vector3(1.0f, 0.5f, 0.0f) : new Vector3(1.0f, 1.0f, 0.0f);
            instanceCount++;
        }
        
        if (instanceCount == 0) return; // No projectiles to render
        
        // Update instance buffer with projectile data
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, projectileInstanceVBO);
        unsafe
        {
            // Create combined data array (matrix + color for each instance)
            float[] combinedData = new float[instanceCount * 19];
            for (int i = 0; i < instanceCount; i++)
            {
                // Copy matrix (16 floats)
                for (int j = 0; j < 16; j++)
                {
                    combinedData[i * 19 + j] = GetMatrixValue(projectileInstanceData[i], j);
                }
                // Copy color (3 floats)
                combinedData[i * 19 + 16] = projectileInstanceColors[i].X;
                combinedData[i * 19 + 17] = projectileInstanceColors[i].Y;
                combinedData[i * 19 + 18] = projectileInstanceColors[i].Z;
            }
            
            fixed (float* dataPtr = combinedData)
            {
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, 
                    (nuint)(instanceCount * 19 * sizeof(float)), dataPtr);
            }
        }
        
        // Setup instanced rendering
        gl.UseProgram(instancedShaderProgram);
        gl.BindVertexArray(vao);
        
        // Setup instance attributes
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, projectileInstanceVBO);
        
        unsafe
        {
            // Instance matrix (locations 2,3,4,5)
            for (uint i = 0; i < 4; i++)
            {
                gl.EnableVertexAttribArray(2 + i);
                gl.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, 
                    19 * sizeof(float), (void*)((i * 4) * sizeof(float)));
                gl.VertexAttribDivisor(2 + i, 1);
            }
            
            // Instance color (location 6)
            gl.EnableVertexAttribArray(6);
            gl.VertexAttribPointer(6, 3, VertexAttribPointerType.Float, false, 
                19 * sizeof(float), (void*)(16 * sizeof(float)));
            gl.VertexAttribDivisor(6, 1);
        }
        
        // Set uniforms
        unsafe
        {
            int viewLoc = gl.GetUniformLocation(instancedShaderProgram, "view");
            int projLoc = gl.GetUniformLocation(instancedShaderProgram, "projection");
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
        }
        
        // Draw all projectiles in one call!
        unsafe
        {
            gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)indices.Length, 
                DrawElementsType.UnsignedInt, null, (uint)instanceCount);
        }
        
        // Clean up divisors
        for (uint i = 2; i <= 6; i++)
        {
            gl.VertexAttribDivisor(i, 0);
            gl.DisableVertexAttribArray(i);
        }
    }
    
    private float GetMatrixValue(Matrix4x4 matrix, int index)
    {
        // Helper to get matrix values by index (row-major order)
        return index switch
        {
            0 => matrix.M11, 1 => matrix.M12, 2 => matrix.M13, 3 => matrix.M14,
            4 => matrix.M21, 5 => matrix.M22, 6 => matrix.M23, 7 => matrix.M24,
            8 => matrix.M31, 9 => matrix.M32, 10 => matrix.M33, 11 => matrix.M34,
            12 => matrix.M41, 13 => matrix.M42, 14 => matrix.M43, 15 => matrix.M44,
            _ => 0f
        };
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
    
    private bool disposed = false;
    
    public void Cleanup()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                // Dispose managed resources
                inputSystem?.Dispose();
                renderer?.Cleanup();
                crosshair?.Dispose();
                hud?.Dispose();
                slashEffect?.Dispose();
            }
            
            // Clean up unmanaged resources (OpenGL)
            if (gl != null)
            {
                // Clean up each resource individually to prevent one failure from blocking others
                try
                {
                    gl.DeleteVertexArray(vao);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error deleting VAO: {ex.Message}");
                }
                
                try
                {
                    gl.DeleteBuffer(vbo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error deleting VBO: {ex.Message}");
                }
                
                try
                {
                    gl.DeleteBuffer(ebo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error deleting EBO: {ex.Message}");
                }
                
                try
                {
                    gl.DeleteProgram(shaderProgram);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error deleting shader program: {ex.Message}");
                }
            }
            
            disposed = true;
        }
    }
    
    
    // Hitstop methods removed - now handled per-enemy
    
    private void SpawnBoss()
    {
        // Clear any existing enemies
        enemies.Clear();
        
        // Spawn a single powerful boss enemy
        Vector3 bossSpawnPos = new Vector3(0, 1f, 20f); // Spawn in front of player at distance
        var boss = new Boss(bossSpawnPos, BOSS_HEALTH);
        enemies.Add(boss);
        
        Console.WriteLine($"\n=== BOSS FIGHT ===");
        Console.WriteLine($"Boss Health: {BOSS_HEALTH} HP");
        Console.WriteLine("Defeat the boss to win!");
    }
    
    private void UpdateEnemies(float deltaTime)
    {
        if (characterController == null || camera == null) return;
        
        // Track dead enemies for removal
        List<Enemy> deadEnemies = new List<Enemy>();
        
        foreach (var enemy in enemies)
        {
            if (!enemy.IsActive) continue;
            
            // Update AI state machine and movement
            enemy.Update(deltaTime, characterController.Position, obstacles);
            
            // Check if enemy is ready to fire (in attack state + cooldown expired)
            if (enemy.CanAttack(gameTime))
            {
                FireEnemyProjectile(enemy);
                // Only consume cooldown if projectile was actually fired
                enemy.ConsumeAttackCooldown(gameTime);
            }
            
            // Check if enemy is charging and close enough to deal melee damage
            if (enemy.HasChargeDamageReady)
            {
                float distanceToPlayer = Vector3.Distance(enemy.Position, characterController.Position);
                if (distanceToPlayer <= 2f) // Within melee range
                {
                    playerHealth?.TakeDamage(enemy.GetChargeDamage());
                    enemy.ConsumeChargeDamage();
                    
                    // Apply knockback to player
                    Vector3 knockback = Vector3.Normalize(characterController.Position - enemy.Position);
                    knockback.Y = 0.3f; // Add slight upward force
                    characterController.Velocity += knockback * 10f;
                    
                    // Screenshake for impact
                    camera.TriggerScreenshake();
                    
                    Console.WriteLine($"[Enemy {enemy.Id}] Charge hit player for {enemy.GetChargeDamage()} damage!");
                }
            }
            
            // Check if boss has melee damage ready
            if (enemy is Boss boss && boss.HasMeleeDamageReady)
            {
                float distanceToPlayer = Vector3.Distance(enemy.Position, characterController.Position);
                if (distanceToPlayer <= 3f) // Boss melee range
                {
                    playerHealth?.TakeDamage(boss.GetMeleeDamage());
                    boss.ConsumeMeleeDamage();
                    
                    // Apply knockback to player
                    Vector3 knockback = Vector3.Normalize(characterController.Position - enemy.Position);
                    knockback.Y = 0.2f; // Slight upward force
                    characterController.Velocity += knockback * 8f;
                    
                    // Screenshake for melee hit
                    camera.TriggerScreenshake();
                    
                    Console.WriteLine($"[BOSS] Melee attack hit player for {boss.GetMeleeDamage()} damage!");
                }
            }
            
            // Mark dead enemies for removal
            if (!enemy.IsAlive)
            {
                deadEnemies.Add(enemy);
            }
        }
        
        // Remove dead enemies from the list to free memory
        foreach (var deadEnemy in deadEnemies)
        {
            enemies.Remove(deadEnemy);
        }
    }
    
    private void UpdateProjectiles(float deltaTime)
    {
        if (characterController == null || playerHealth == null) return;
        
        foreach (var projectile in projectiles)
        {
            if (!projectile.IsActive) continue;
            
            projectile.Update(deltaTime); // Move projectile and check lifetime
            
            // Check collision with obstacles
            bool hitObstacle = false;
            foreach (var obstacle in obstacles)
            {
                if (obstacle.IsDestroyed) continue;
                
                // Check if projectile hits obstacle
                if (obstacle.CheckCollision(projectile.Position, 0.2f))
                {
                    // Damage destructible obstacles
                    if (obstacle.IsDestructible)
                    {
                        obstacle.TakeDamage(projectile.Damage);
                    }
                    
                    // Deactivate projectile on impact
                    projectile.Deactivate();
                    hitObstacle = true;
                    break;
                }
            }
            
            if (hitObstacle) continue;
            
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
                        enemy.TakeDamage(10f);
                        projectile.Deactivate();
                        
                        // Hitstop now handled per-enemy in TakeDamage
                        
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
        if (camera == null) return;
        
        if (currentWeaponIndex == 0)
        {
            // Revolver
            if (revolver == null || !revolver.CanShoot()) return;
            
            // Trigger recoil when firing revolver (strong recoil)
            camera.TriggerRecoil(6.0f);
            
            // Spawn a projectile for the revolver shot
            var projectile = projectiles.FirstOrDefault(p => !p.IsActive);
            if (projectile != null)
            {
                Vector3 origin = camera.Position;
                Vector3 direction = camera.GetForwardVector();
                projectile.Fire(origin, direction, revolver.ProjectileSpeed, revolver.Damage, fromEnemy: false);
                revolver.Shoot(); // Consume ammo and handle auto-reload
            }
            else
            {
                // Projectile pool exhausted - provide feedback
                Console.WriteLine("[WARNING] Projectile pool exhausted! Cannot fire.");
            }
            
            Console.WriteLine($"[{revolver.Name}] Fired! ({revolver.CurrentAmmo}/{revolver.MaxAmmo})");
        }
        else if (currentWeaponIndex == 1)
        {
            // Katana
            if (katana == null) return;
            
            Vector3 origin = camera.Position;
            Vector3 direction = camera.GetForwardVector();
            
            katana.Slash(origin, direction, enemies, (enemy) =>
            {
                enemy.TakeDamage(katana.Damage);
                Console.WriteLine($"[Katana] Hit enemy for {katana.Damage} damage!");
            });
            
            Console.WriteLine($"[{katana.Name}] Slashed!");
        }
        else if (currentWeaponIndex == 2)
        {
            // SMG
            if (smg == null || !smg.CanShoot()) return;
            
            // Light recoil for SMG
            camera.TriggerRecoil(0.5f);
            
            // Spawn a projectile with spread
            var projectile = projectiles.FirstOrDefault(p => !p.IsActive);
            if (projectile != null)
            {
                Vector3 origin = camera.Position;
                Vector3 baseDirection = camera.GetForwardVector();
                Vector3 direction = smg.GetSpreadDirection(baseDirection);
                projectile.Fire(origin, direction, smg.ProjectileSpeed, smg.Damage, fromEnemy: false);
                smg.Shoot(); // Consume ammo and handle auto-reload
            }
            else
            {
                Console.WriteLine("[WARNING] Projectile pool exhausted! Cannot fire.");
            }
            
            Console.WriteLine($"[{smg.Name}] Fired! ({smg.CurrentAmmo}/{smg.MaxAmmo})");
        }
    }
    
    private void SelectWeapon(int index)
    {
        if (index != currentWeaponIndex)
        {
            currentWeaponIndex = index;
            string weaponName = currentWeaponIndex switch
            {
                0 => revolver?.Name ?? "Revolver",
                1 => katana?.Name ?? "Katana",
                2 => smg?.Name ?? "SMG",
                _ => "Unknown"
            };
            Console.WriteLine($"Switched to {weaponName}");
        }
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
        else
        {
            // Pool exhausted - enemy can't fire
            Console.WriteLine($"Enemy {enemy.Id} couldn't fire - projectile pool exhausted!");
        }
    }
    
    private void CheckBossDefeat(float deltaTime)
    {
        // Check if boss is defeated
        if (enemies.Count > 0 && enemies.All(e => !e.IsAlive))
        {
            // Boss defeated!
            Console.WriteLine("\n=== VICTORY! ===");
            Console.WriteLine("You have defeated the boss!");
            Console.WriteLine("Press F2 to spawn a new boss");
            
            // Clear the boss
            enemies.Clear();
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
    
    private void GenerateObstacles()
    {
        obstacles.Clear();
        
        // One central pillar
        obstacles.Add(new Obstacle(new Vector3(0f, 6f, 0f), ObstacleType.Pillar));
    }
}