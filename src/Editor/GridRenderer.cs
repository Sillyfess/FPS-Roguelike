using System.Numerics;
using Silk.NET.OpenGL;

namespace FPSRoguelike.Editor;

/// <summary>
/// Renders a grid for spatial reference in the level editor
/// </summary>
public class GridRenderer : IDisposable
{
    private GL? gl;
    private uint vao;
    private uint vbo;
    private uint shaderProgram;
    
    // Grid settings
    private const int GRID_SIZE = 100;
    private const float GRID_SPACING = 1f;
    private const float MAJOR_LINE_INTERVAL = 10f;
    
    // Shader source
    private const string VERTEX_SHADER = @"
        #version 330 core
        layout(location = 0) in vec3 aPos;
        layout(location = 1) in vec4 aColor;
        
        uniform mat4 uView;
        uniform mat4 uProjection;
        
        out vec4 vertexColor;
        
        void main()
        {
            gl_Position = uProjection * uView * vec4(aPos, 1.0);
            vertexColor = aColor;
        }
    ";
    
    private const string FRAGMENT_SHADER = @"
        #version 330 core
        in vec4 vertexColor;
        out vec4 FragColor;
        
        uniform float uAlpha;
        
        void main()
        {
            FragColor = vec4(vertexColor.rgb, vertexColor.a * uAlpha);
        }
    ";
    
    private bool disposed = false;
    
    public GridRenderer(GL openGL)
    {
        gl = openGL;
        Initialize();
    }
    
    private void Initialize()
    {
        if (gl == null) return;
        
        // Create shader program
        uint vertexShader = CompileShader(ShaderType.VertexShader, VERTEX_SHADER);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, FRAGMENT_SHADER);
        
        shaderProgram = gl.CreateProgram();
        gl.AttachShader(shaderProgram, vertexShader);
        gl.AttachShader(shaderProgram, fragmentShader);
        gl.LinkProgram(shaderProgram);
        
        // Check for linking errors
        gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetProgramInfoLog(shaderProgram);
            throw new Exception($"Grid shader linking failed: {infoLog}");
        }
        
        // Cleanup shaders
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        // Generate grid vertices
        List<float> vertices = GenerateGridVertices();
        
        // Create VAO and VBO
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        
        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        
        unsafe
        {
            fixed (float* v = vertices.ToArray())
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, 
                    (nuint)(vertices.Count * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }
        
        // Position attribute
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 7 * sizeof(float), (void*)0);
        }
        gl.EnableVertexAttribArray(0);
        
        // Color attribute
        unsafe
        {
            gl.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 7 * sizeof(float), (void*)(3 * sizeof(float)));
        }
        gl.EnableVertexAttribArray(1);
        
        gl.BindVertexArray(0);
    }
    
    private List<float> GenerateGridVertices()
    {
        List<float> vertices = new List<float>();
        
        float halfSize = GRID_SIZE * GRID_SPACING / 2f;
        
        // Generate lines along X axis
        for (int z = -GRID_SIZE / 2; z <= GRID_SIZE / 2; z++)
        {
            float zPos = z * GRID_SPACING;
            bool isMajor = z % (int)MAJOR_LINE_INTERVAL == 0;
            
            // Determine color based on line type
            Vector4 color;
            if (z == 0)
                color = new Vector4(0f, 0f, 1f, 1f); // Blue for Z axis
            else if (isMajor)
                color = new Vector4(0.5f, 0.5f, 0.5f, 0.8f); // Brighter for major lines
            else
                color = new Vector4(0.3f, 0.3f, 0.3f, 0.5f); // Dimmer for minor lines
            
            // Start point
            vertices.Add(-halfSize);
            vertices.Add(0f);
            vertices.Add(zPos);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
            vertices.Add(color.W);
            
            // End point
            vertices.Add(halfSize);
            vertices.Add(0f);
            vertices.Add(zPos);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
            vertices.Add(color.W);
        }
        
        // Generate lines along Z axis
        for (int x = -GRID_SIZE / 2; x <= GRID_SIZE / 2; x++)
        {
            float xPos = x * GRID_SPACING;
            bool isMajor = x % (int)MAJOR_LINE_INTERVAL == 0;
            
            // Determine color based on line type
            Vector4 color;
            if (x == 0)
                color = new Vector4(1f, 0f, 0f, 1f); // Red for X axis
            else if (isMajor)
                color = new Vector4(0.5f, 0.5f, 0.5f, 0.8f); // Brighter for major lines
            else
                color = new Vector4(0.3f, 0.3f, 0.3f, 0.5f); // Dimmer for minor lines
            
            // Start point
            vertices.Add(xPos);
            vertices.Add(0f);
            vertices.Add(-halfSize);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
            vertices.Add(color.W);
            
            // End point
            vertices.Add(xPos);
            vertices.Add(0f);
            vertices.Add(halfSize);
            vertices.Add(color.X);
            vertices.Add(color.Y);
            vertices.Add(color.Z);
            vertices.Add(color.W);
        }
        
        // Add vertical axis line (Y axis)
        vertices.Add(0f);
        vertices.Add(0f);
        vertices.Add(0f);
        vertices.Add(0f);
        vertices.Add(1f);
        vertices.Add(0f);
        vertices.Add(1f);
        
        vertices.Add(0f);
        vertices.Add(20f);
        vertices.Add(0f);
        vertices.Add(0f);
        vertices.Add(1f);
        vertices.Add(0f);
        vertices.Add(1f);
        
        return vertices;
    }
    
    private uint CompileShader(ShaderType type, string source)
    {
        if (gl == null) throw new InvalidOperationException("OpenGL context is null");
        
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation failed: {infoLog}");
        }
        
        return shader;
    }
    
    /// <summary>
    /// Render the grid
    /// </summary>
    public void Render(Matrix4x4 view, Matrix4x4 projection, float alpha = 0.5f)
    {
        if (gl == null || disposed) return;
        
        // Use shader program
        gl.UseProgram(shaderProgram);
        
        // Set uniforms
        unsafe
        {
            int viewLoc = gl.GetUniformLocation(shaderProgram, "uView");
            if (viewLoc != -1)
            {
                float* viewPtr = (float*)&view;
                gl.UniformMatrix4(viewLoc, 1, false, viewPtr);
            }
            
            int projLoc = gl.GetUniformLocation(shaderProgram, "uProjection");
            if (projLoc != -1)
            {
                float* projPtr = (float*)&projection;
                gl.UniformMatrix4(projLoc, 1, false, projPtr);
            }
        }
        
        int alphaLoc = gl.GetUniformLocation(shaderProgram, "uAlpha");
        if (alphaLoc != -1)
        {
            gl.Uniform1(alphaLoc, alpha);
        }
        
        // Enable blending for transparency
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        // Disable depth writing for grid (so it doesn't occlude objects)
        gl.DepthMask(false);
        
        // Draw grid
        gl.BindVertexArray(vao);
        int lineCount = (GRID_SIZE + 1) * 2 * 2 + 1; // X lines + Z lines + Y axis
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)(lineCount * 2));
        gl.BindVertexArray(0);
        
        // Re-enable depth writing
        gl.DepthMask(true);
        
        // Reset shader
        gl.UseProgram(0);
    }
    
    public void Dispose()
    {
        if (disposed) return;
        
        if (gl != null)
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteProgram(shaderProgram);
        }
        
        disposed = true;
    }
}