using Silk.NET.OpenGL;
using System;
using System.Numerics;

namespace FPSRoguelike.Rendering;

public class Crosshair : IDisposable
{
    private GL gl;
    private uint vao, vbo;
    private uint shaderProgram;
    
    // Crosshair parameters (classic CS style)
    private float thickness = 2.0f;  // Line thickness in pixels
    private float length = 15.0f;    // Length of each line
    private float gap = 5.0f;        // Gap from center
    private Vector3 color = new Vector3(0.0f, 1.0f, 0.0f); // Classic green
    
    // IDisposable pattern fields
    private bool disposed = false;
    
    public Crosshair(GL glContext)
    {
        gl = glContext;
        Initialize();
    }
    
    private void Initialize()
    {
        // Create crosshair geometry (4 lines from center)
        float[] vertices = new float[]
        {
            // Horizontal lines (left and right)
            -1.0f, 0.0f,  // Left start
            -0.5f, 0.0f,  // Left end
            0.5f,  0.0f,  // Right start
            1.0f,  0.0f,  // Right end
            
            // Vertical lines (top and bottom)
            0.0f, -1.0f,  // Bottom start
            0.0f, -0.5f,  // Bottom end
            0.0f,  0.5f,  // Top start
            0.0f,  1.0f,  // Top end
        };
        
        // Create VAO and VBO
        vao = gl.GenVertexArray();
        gl.BindVertexArray(vao);
        
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
            
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), null);
            gl.EnableVertexAttribArray(0);
        }
        
        // Create simple 2D shader for crosshair
        CreateCrosshairShader();
    }
    
    private void CreateCrosshairShader()
    {
        string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec2 aPos;

uniform vec2 screenSize;
uniform float gap;
uniform float length;

out vec2 screenPos;

void main()
{
    vec2 pos = aPos;
    
    // Apply gap and length
    if (abs(pos.x) > 0.1) // Horizontal line
    {
        float sign = sign(pos.x);
        pos.x = sign * (gap + (abs(pos.x) - 0.5) * length) / (screenSize.x * 0.5);
    }
    else // Vertical line
    {
        float sign = sign(pos.y);
        pos.y = sign * (gap + (abs(pos.y) - 0.5) * length) / (screenSize.y * 0.5);
    }
    
    gl_Position = vec4(pos, 0.0, 1.0);
    screenPos = pos;
}";

        string fragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

uniform vec3 crosshairColor;

void main()
{
    FragColor = vec4(crosshairColor, 1.0);
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
            throw new Exception($"Crosshair vertex shader compilation failed: {infoLog}");
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
            throw new Exception($"Crosshair fragment shader compilation failed: {infoLog}");
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
            throw new Exception($"Crosshair shader linking failed: {infoLog}");
        }
        
        // Clean up
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
    
    public void Render(int screenWidth, int screenHeight)
    {
        // Save current state
        bool depthTest = gl.IsEnabled(EnableCap.DepthTest);
        
        // Setup for 2D rendering
        gl.Disable(EnableCap.DepthTest);
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Set uniforms
        int screenSizeLoc = gl.GetUniformLocation(shaderProgram, "screenSize");
        int colorLoc = gl.GetUniformLocation(shaderProgram, "crosshairColor");
        int gapLoc = gl.GetUniformLocation(shaderProgram, "gap");
        int lengthLoc = gl.GetUniformLocation(shaderProgram, "length");
        
        gl.Uniform2(screenSizeLoc, (float)screenWidth, (float)screenHeight);
        gl.Uniform3(colorLoc, color.X, color.Y, color.Z);
        gl.Uniform1(gapLoc, gap);
        gl.Uniform1(lengthLoc, length);
        
        // Set line width
        gl.LineWidth(thickness);
        
        // Draw crosshair lines
        gl.DrawArrays(PrimitiveType.Lines, 0, 8);
        
        // Restore state
        if (depthTest)
            gl.Enable(EnableCap.DepthTest);
            
        // Reset line width
        gl.LineWidth(1.0f);
    }
    
    public void SetColor(Vector3 newColor)
    {
        color = newColor;
    }
    
    public void SetSize(float newLength, float newGap, float newThickness)
    {
        length = newLength;
        gap = newGap;
        thickness = newThickness;
    }
    
    public void Cleanup()
    {
        Dispose();
    }
    
    // IDisposable implementation
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
                // Dispose managed resources (none in this case)
            }
            
            // Clean up unmanaged OpenGL resources
            if (gl != null)
            {
                if (vao != 0)
                {
                    gl.DeleteVertexArray(vao);
                    vao = 0;
                }
                
                if (vbo != 0)
                {
                    gl.DeleteBuffer(vbo);
                    vbo = 0;
                }
                
                if (shaderProgram != 0)
                {
                    gl.DeleteProgram(shaderProgram);
                    shaderProgram = 0;
                }
            }
            
            disposed = true;
        }
    }
    
    ~Crosshair()
    {
        Dispose(false);
    }
}