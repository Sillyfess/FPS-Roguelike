using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System.Drawing;
using System.Numerics;
using FPSRoguelike.Input;
using FPSRoguelike.Rendering;

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
        
        // Lock cursor for FPS
        var input = window.CreateInput();
        if (input.Mice.Count > 0)
        {
            var mouse = input.Mice[0];
            mouse.Cursor.CursorMode = CursorMode.Raw;
        }
        
        Console.WriteLine("Game initialized!");
        Console.WriteLine("Press ESC to exit");
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
        
        // For now, just render the test cube
        RenderTestCube(interpolation);
    }
    
    private void UpdateGameLogic(double fixedDeltaTime)
    {
        // Game logic will go here
        // Update rotation during fixed timestep
        rotation += (float)(fixedDeltaTime * 50.0f);
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

void main()
{
    // Directional light from above-right
    vec3 lightDir = normalize(vec3(-0.5, -1.0, -0.3));
    vec3 lightColor = vec3(1.0, 1.0, 1.0);
    vec3 objectColor = vec3(0.5, 0.6, 0.7);
    
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
    
    private void RenderTestCube(double interpolation)
    {
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Create transformation matrices
        Matrix4x4 model = Matrix4x4.CreateRotationY(rotation * (float)(Math.PI / 180.0)) *
                         Matrix4x4.CreateRotationX(rotation * 0.5f * (float)(Math.PI / 180.0));
        
        Matrix4x4 view = Matrix4x4.CreateLookAt(
            new Vector3(0.0f, 0.0f, 3.0f),
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f)
        );
        
        float aspectRatio = (float)window.Size.X / window.Size.Y;
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            45.0f * (float)(Math.PI / 180.0),
            aspectRatio,
            0.1f,
            100.0f
        );
        
        // Set uniforms
        unsafe
        {
            int modelLoc = gl.GetUniformLocation(shaderProgram, "model");
            int viewLoc = gl.GetUniformLocation(shaderProgram, "view");
            int projLoc = gl.GetUniformLocation(shaderProgram, "projection");
            
            gl.UniformMatrix4(modelLoc, 1, false, (float*)&model);
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
            
            // Draw
            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
        }
    }
    
    public void Cleanup()
    {
        gl.DeleteVertexArray(vao);
        gl.DeleteBuffer(vbo);
        gl.DeleteBuffer(ebo);
        gl.DeleteProgram(shaderProgram);
        
        Console.WriteLine("Game cleaned up!");
    }
}