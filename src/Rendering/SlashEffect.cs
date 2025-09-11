using Silk.NET.OpenGL;
using System.Numerics;
using System.Text;

namespace FPSRoguelike.Rendering;

public class SlashEffect : IDisposable
{
    private GL gl;
    private uint vao, vbo;
    private uint shaderProgram;
    private const int MAX_POINTS = 32;
    private float[] vertices = new float[MAX_POINTS * 3];
    
    private bool isActive = false;
    private float effectTimer = 0f;
    private const float EFFECT_DURATION = 0.2f;
    
    public SlashEffect(GL glContext)
    {
        gl = glContext;
        SetupSlashEffect();
    }
    
    public void Update(float deltaTime)
    {
        if (isActive)
        {
            effectTimer += deltaTime;
            if (effectTimer >= EFFECT_DURATION)
            {
                isActive = false;
                effectTimer = 0f;
            }
        }
    }
    
    public void Trigger()
    {
        isActive = true;
        effectTimer = 0f;
    }
    
    private void SetupSlashEffect()
    {
        // Create VAO and VBO
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        
        gl.BindVertexArray(vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        
        // Allocate buffer for maximum points
        unsafe
        {
            gl.BufferData(BufferTargetARB.ArrayBuffer, (uint)(MAX_POINTS * 3 * sizeof(float)), 
                null, BufferUsageARB.DynamicDraw);
        }
        
        // Position attribute
        unsafe
        {
            gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        }
        gl.EnableVertexAttribArray(0);
        
        // Create shader program
        CreateShaderProgram();
        
        gl.BindVertexArray(0);
    }
    
    private void CreateShaderProgram()
    {
        string vertexShaderSource = @"
#version 330 core
layout (location = 0) in vec3 aPos;

uniform mat4 view;
uniform mat4 projection;

void main()
{
    gl_Position = projection * view * vec4(aPos, 1.0);
}";

        string fragmentShaderSource = @"
#version 330 core
out vec4 FragColor;

uniform float progress;

void main()
{
    // Cyan-blue slash effect that fades out
    float alpha = (1.0 - progress) * 0.8;
    FragColor = vec4(0.2, 0.8, 1.0, alpha);
}";

        // Compile shaders
        uint vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        uint fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);
        
        // Link program
        shaderProgram = gl.CreateProgram();
        gl.AttachShader(shaderProgram, vertexShader);
        gl.AttachShader(shaderProgram, fragmentShader);
        gl.LinkProgram(shaderProgram);
        
        // Check for linking errors
        gl.GetProgram(shaderProgram, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetProgramInfoLog(shaderProgram);
            throw new Exception($"Shader program linking failed: {infoLog}");
        }
        
        // Clean up
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
    }
    
    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = gl.CreateShader(type);
        gl.ShaderSource(shader, source);
        gl.CompileShader(shader);
        
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = gl.GetShaderInfoLog(shader);
            throw new Exception($"Shader compilation failed ({type}): {infoLog}");
        }
        
        return shader;
    }
    
    public void Render(Vector3[] points, float progress, Matrix4x4 view, Matrix4x4 projection)
    {
        if (points == null || points.Length < 2) return;
        
        // Update vertex data
        int pointCount = Math.Min(points.Length, MAX_POINTS);
        for (int i = 0; i < pointCount; i++)
        {
            vertices[i * 3] = points[i].X;
            vertices[i * 3 + 1] = points[i].Y;
            vertices[i * 3 + 2] = points[i].Z;
        }
        
        // Upload data to GPU
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* ptr = vertices)
            {
                gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, 
                    (uint)(pointCount * 3 * sizeof(float)), ptr);
            }
        }
        
        // Setup render state
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        gl.Disable(EnableCap.DepthTest);
        gl.LineWidth(3.0f);
        
        // Use shader
        gl.UseProgram(shaderProgram);
        
        // Set uniforms
        unsafe
        {
            int viewLoc = gl.GetUniformLocation(shaderProgram, "view");
            gl.UniformMatrix4(viewLoc, 1, false, (float*)&view);
            
            int projLoc = gl.GetUniformLocation(shaderProgram, "projection");
            gl.UniformMatrix4(projLoc, 1, false, (float*)&projection);
            
            int progressLoc = gl.GetUniformLocation(shaderProgram, "progress");
            gl.Uniform1(progressLoc, progress);
        }
        
        // Draw the slash arc
        gl.BindVertexArray(vao);
        gl.DrawArrays(PrimitiveType.LineStrip, 0, (uint)pointCount);
        
        // Also draw a fan to fill the area
        gl.DrawArrays(PrimitiveType.TriangleFan, 0, (uint)pointCount);
        
        // Restore state
        gl.Enable(EnableCap.DepthTest);
        gl.Disable(EnableCap.Blend);
        gl.LineWidth(1.0f);
        gl.UseProgram(0);
        gl.BindVertexArray(0);
    }
    
    public void Dispose()
    {
        gl.DeleteVertexArray(vao);
        gl.DeleteBuffer(vbo);
        gl.DeleteProgram(shaderProgram);
    }
}