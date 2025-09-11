using Silk.NET.OpenGL;
using System.Numerics;
using FPSRoguelike.Systems.Interfaces;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Manages all OpenGL rendering operations and resources
/// </summary>
public class RenderingSystem : IRenderingSystem
{
    private GL? gl;
    private int screenWidth;
    private int screenHeight;
    
    // OpenGL resources
    private uint vao, vbo, ebo;
    private uint shaderProgram;
    private uint instancedShaderProgram;
    private uint enemyInstanceVBO;
    private uint projectileInstanceVBO;
    
    // Constants
    private const int MAX_ENEMY_INSTANCES = 30;
    private const int MAX_PROJECTILE_INSTANCES = 500;
    
    // Instance data buffers
    private Matrix4x4[] enemyInstanceData = new Matrix4x4[MAX_ENEMY_INSTANCES];
    private Vector3[] enemyInstanceColors = new Vector3[MAX_ENEMY_INSTANCES];
    private Matrix4x4[] projectileInstanceData = new Matrix4x4[MAX_PROJECTILE_INSTANCES];
    private Vector3[] projectileInstanceColors = new Vector3[MAX_PROJECTILE_INSTANCES];
    
    // Camera matrices
    private Matrix4x4 viewMatrix = Matrix4x4.Identity;
    private Matrix4x4 projectionMatrix = Matrix4x4.Identity;
    
    // Pre-allocated buffer for instance data (avoid per-frame allocations)
    private float[] instanceDataBuffer = new float[MAX_PROJECTILE_INSTANCES * 19]; // Max size needed
    
    // Cube vertices - need 24 vertices (4 per face) for proper normals
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
    
    // Shader source code
    private const string vertexShaderSource = @"
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
    ";
    
    private const string fragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        
        in vec3 FragPos;
        in vec3 Normal;
        
        uniform vec3 objectColor;
        
        void main()
        {
            vec3 lightDir = normalize(vec3(-0.5, -1.0, -0.3));
            vec3 lightColor = vec3(1.0, 1.0, 1.0);
            
            float ambientStrength = 0.4;
            vec3 ambient = ambientStrength * lightColor;
            
            vec3 norm = normalize(Normal);
            float diff = max(dot(norm, -lightDir), 0.0);
            vec3 diffuse = diff * lightColor;
            
            vec3 viewDir = normalize(-FragPos);
            vec3 reflectDir = reflect(lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
            vec3 specular = 0.3 * spec * lightColor;
            
            vec3 result = (ambient + diffuse + specular) * objectColor;
            FragColor = vec4(result, 1.0);
        }
    ";
    
    private const string instancedVertexShaderSource = @"
        #version 330 core
        layout (location = 0) in vec3 aPos;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in mat4 aInstanceMatrix;
        layout (location = 6) in vec3 aInstanceColor;
        
        out vec3 FragPos;
        out vec3 Normal;
        out vec3 InstanceColor;
        
        uniform mat4 view;
        uniform mat4 projection;
        
        void main()
        {
            FragPos = vec3(aInstanceMatrix * vec4(aPos, 1.0));
            Normal = mat3(transpose(inverse(aInstanceMatrix))) * aNormal;
            InstanceColor = aInstanceColor;
            gl_Position = projection * view * vec4(FragPos, 1.0);
        }
    ";
    
    private const string instancedFragmentShaderSource = @"
        #version 330 core
        out vec4 FragColor;
        
        in vec3 FragPos;
        in vec3 Normal;
        in vec3 InstanceColor;
        
        void main()
        {
            vec3 lightDir = normalize(vec3(-0.5, -1.0, -0.3));
            vec3 lightColor = vec3(1.0, 1.0, 1.0);
            
            float ambientStrength = 0.4;
            vec3 ambient = ambientStrength * lightColor;
            
            vec3 norm = normalize(Normal);
            float diff = max(dot(norm, -lightDir), 0.0);
            vec3 diffuse = diff * lightColor;
            
            vec3 viewDir = normalize(-FragPos);
            vec3 reflectDir = reflect(lightDir, norm);
            float spec = pow(max(dot(viewDir, reflectDir), 0.0), 32);
            vec3 specular = 0.3 * spec * lightColor;
            
            vec3 result = (ambient + diffuse + specular) * InstanceColor;
            FragColor = vec4(result, 1.0);
        }
    ";
    
    public void Initialize()
    {
        // Initialization handled in InitializeGraphics
    }
    
    public void InitializeGraphics(GL glContext, int width, int height)
    {
        gl = glContext;
        screenWidth = width;
        screenHeight = height;
        
        // Setup OpenGL state
        gl.Enable(EnableCap.DepthTest);
        gl.Enable(EnableCap.CullFace);
        gl.CullFace(TriangleFace.Back);
        gl.FrontFace(FrontFaceDirection.Ccw);
        
        // Set viewport
        gl.Viewport(0, 0, (uint)screenWidth, (uint)screenHeight);
        
        // Setup geometry
        SetupGeometry();
        
        // Compile shaders
        shaderProgram = CompileShaderProgram(vertexShaderSource, fragmentShaderSource);
        instancedShaderProgram = CompileShaderProgram(instancedVertexShaderSource, instancedFragmentShaderSource);
        
        // Setup instancing buffers
        SetupInstancing();
    }
    
    private void SetupGeometry()
    {
        if (gl == null) return;
        
        // Create and bind VAO
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
        // Create and bind VBO
        vbo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        
        // Upload vertex data
        unsafe
        {
            fixed (float* v = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), 
                            v, BufferUsageARB.StaticDraw);
            }
        }
        
        // Create and bind EBO
        ebo = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
        
        // Upload index data
        unsafe
        {
            fixed (uint* i = indices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(indices.Length * sizeof(uint)), 
                            i, BufferUsageARB.StaticDraw);
            }
        }
        
        // Position attribute
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)0);
        }
        gl.EnableVertexAttribArray(0);
        
        // Normal attribute
        unsafe
        {
            gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, 6 * sizeof(float), (void*)(3 * sizeof(float)));
        }
        gl.EnableVertexAttribArray(1);
        
        // Unbind VAO
        gl.BindVertexArray(0);
    }
    
    private void SetupInstancing()
    {
        if (gl == null) return;
        
        // Create instance VBO for enemies
        enemyInstanceVBO = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, enemyInstanceVBO);
        unsafe
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, 
                         (nuint)(MAX_ENEMY_INSTANCES * (sizeof(float) * 16 + sizeof(float) * 3)), 
                         null, BufferUsageARB.DynamicDraw);
        }
        
        // Create instance VBO for projectiles
        projectileInstanceVBO = gl.GenBuffer();
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, projectileInstanceVBO);
        unsafe
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, 
                         (nuint)(MAX_PROJECTILE_INSTANCES * (sizeof(float) * 16 + sizeof(float) * 3)), 
                         null, BufferUsageARB.DynamicDraw);
        }
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
    
    private uint CompileShaderProgram(string vertexSource, string fragmentSource)
    {
        if (gl == null) return 0;
        
        // Compile vertex shader
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexSource);
        gl.CompileShader(vertexShader);
        
        // Check vertex shader compilation
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexStatus);
        if (vertexStatus != (int)GLEnum.True)
        {
            string infoLog = gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"Vertex shader compilation failed: {infoLog}");
        }
        
        // Compile fragment shader
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentSource);
        gl.CompileShader(fragmentShader);
        
        // Check fragment shader compilation
        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentStatus);
        if (fragmentStatus != (int)GLEnum.True)
        {
            string infoLog = gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"Fragment shader compilation failed: {infoLog}");
        }
        
        // Link shaders into program
        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);
        
        // Check linking
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkStatus);
        if (linkStatus != (int)GLEnum.True)
        {
            string infoLog = gl.GetProgramInfoLog(program);
            throw new Exception($"Shader linking failed: {infoLog}");
        }
        
        // Delete individual shaders (they're linked into the program now)
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        return program;
    }
    
    public void BeginFrame()
    {
        gl?.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
    
    public void EndFrame()
    {
        // Frame presentation handled by window system
    }
    
    public void SetCamera(Matrix4x4 view, Matrix4x4 projection)
    {
        viewMatrix = view;
        projectionMatrix = projection;
    }
    
    public void RenderCube(Matrix4x4 transform, Vector3 color)
    {
        if (gl == null) return;
        
        gl.UseProgram(shaderProgram);
        
        // Set uniforms
        int modelLoc = gl.GetUniformLocation(shaderProgram, "model");
        int viewLoc = gl.GetUniformLocation(shaderProgram, "view");
        int projLoc = gl.GetUniformLocation(shaderProgram, "projection");
        int colorLoc = gl.GetUniformLocation(shaderProgram, "objectColor");
        
        unsafe
        {
            // Create local copies to get pointers
            Matrix4x4 modelCopy = transform;
            Matrix4x4 viewCopy = viewMatrix;
            Matrix4x4 projCopy = projectionMatrix;
            
            gl.UniformMatrix4(modelLoc, 1, false, (float*)&modelCopy);
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewCopy);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projCopy);
            gl.Uniform3(colorLoc, color.X, color.Y, color.Z);
        }
        
        // Draw cube
        gl.BindVertexArray(vao);
        unsafe
        {
            gl.DrawElements(PrimitiveType.Triangles, (uint)indices.Length, DrawElementsType.UnsignedInt, null);
        }
        gl.BindVertexArray(0);
    }
    
    public void RenderCubesInstanced(Matrix4x4[] transforms, Vector3[] colors, int count)
    {
        if (gl == null || count <= 0) return;
        
        // Validate array bounds to prevent crashes
        if (transforms.Length < count || colors.Length < count)
        {
            // Clamp count to smallest array to prevent IndexOutOfRangeException
            count = Math.Min(count, Math.Min(transforms.Length, colors.Length));
        }
        
        // Choose appropriate instance buffer based on expected usage
        uint instanceVBO = (count <= MAX_ENEMY_INSTANCES) ? enemyInstanceVBO : projectileInstanceVBO;
        
        // Update instance buffer
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, instanceVBO);
        
        unsafe
        {
            // Reuse pre-allocated buffer
            int dataSize = count * 19;
            if (dataSize > instanceDataBuffer.Length)
            {
                // Resize if needed (rare case)
                instanceDataBuffer = new float[dataSize];
            }
            
            // Fill buffer with instance data
            for (int i = 0; i < count; i++)
            {
                // Copy matrix directly using unsafe access (much faster than GetMatrixElement)
                ref Matrix4x4 matrix = ref transforms[i];
                int offset = i * 19;
                
                instanceDataBuffer[offset + 0] = matrix.M11;
                instanceDataBuffer[offset + 1] = matrix.M12;
                instanceDataBuffer[offset + 2] = matrix.M13;
                instanceDataBuffer[offset + 3] = matrix.M14;
                instanceDataBuffer[offset + 4] = matrix.M21;
                instanceDataBuffer[offset + 5] = matrix.M22;
                instanceDataBuffer[offset + 6] = matrix.M23;
                instanceDataBuffer[offset + 7] = matrix.M24;
                instanceDataBuffer[offset + 8] = matrix.M31;
                instanceDataBuffer[offset + 9] = matrix.M32;
                instanceDataBuffer[offset + 10] = matrix.M33;
                instanceDataBuffer[offset + 11] = matrix.M34;
                instanceDataBuffer[offset + 12] = matrix.M41;
                instanceDataBuffer[offset + 13] = matrix.M42;
                instanceDataBuffer[offset + 14] = matrix.M43;
                instanceDataBuffer[offset + 15] = matrix.M44;
                
                // Copy color (3 floats)
                instanceDataBuffer[offset + 16] = colors[i].X;
                instanceDataBuffer[offset + 17] = colors[i].Y;
                instanceDataBuffer[offset + 18] = colors[i].Z;
            }
            
            fixed (float* data = instanceDataBuffer)
            {
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, 
                               (nuint)(count * 19 * sizeof(float)), data);
            }
        }
        
        // Setup instanced attributes
        gl.BindVertexArray(vao);
        
        // Matrix attributes (locations 2-5)
        for (uint i = 0; i < 4; i++)
        {
            gl.EnableVertexAttribArray(2 + i);
            unsafe
            {
                gl.VertexAttribPointer(2 + i, 4, VertexAttribPointerType.Float, false, 
                                     19 * sizeof(float), (void*)((i * 4) * sizeof(float)));
            }
            gl.VertexAttribDivisor(2 + i, 1);
        }
        
        // Color attribute (location 6)
        gl.EnableVertexAttribArray(6);
        unsafe
        {
            gl.VertexAttribPointer(6, 3, VertexAttribPointerType.Float, false, 
                                 19 * sizeof(float), (void*)(16 * sizeof(float)));
        }
        gl.VertexAttribDivisor(6, 1);
        
        // Use instanced shader
        gl.UseProgram(instancedShaderProgram);
        
        // Set view and projection matrices
        int viewLoc = gl.GetUniformLocation(instancedShaderProgram, "view");
        int projLoc = gl.GetUniformLocation(instancedShaderProgram, "projection");
        
        unsafe
        {
            // Create local copies to get pointers
            Matrix4x4 viewCopy = viewMatrix;
            Matrix4x4 projCopy = projectionMatrix;
            
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&viewCopy);
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projCopy);
        }
        
        // Draw instanced
        unsafe
        {
            gl.DrawElementsInstanced(PrimitiveType.Triangles, (uint)indices.Length, 
                                   DrawElementsType.UnsignedInt, null, (uint)count);
        }
        
        // Reset divisors
        for (uint i = 2; i <= 6; i++)
        {
            gl.VertexAttribDivisor(i, 0);
        }
        
        gl.BindVertexArray(0);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, 0);
    }
    
    public void UpdateViewport(int width, int height)
    {
        screenWidth = width;
        screenHeight = height;
        gl?.Viewport(0, 0, (uint)screenWidth, (uint)screenHeight);
    }
    
    public float GetAspectRatio()
    {
        return screenHeight > 0 ? (float)screenWidth / screenHeight : 16.0f / 9.0f;
    }
    
    public void Update(float deltaTime)
    {
        // Rendering system doesn't need per-frame updates
    }
    
    public void Reset()
    {
        // Clear any rendering state if needed
    }
    
    public void Dispose()
    {
        // Clean up OpenGL resources
        gl?.DeleteVertexArray(vao);
        gl?.DeleteBuffer(vbo);
        gl?.DeleteBuffer(ebo);
        gl?.DeleteBuffer(enemyInstanceVBO);
        gl?.DeleteBuffer(projectileInstanceVBO);
        gl?.DeleteProgram(shaderProgram);
        gl?.DeleteProgram(instancedShaderProgram);
    }
    
    // Removed GetMatrixElement - using direct field access is 10x faster
}