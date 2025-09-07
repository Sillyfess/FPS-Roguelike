using Silk.NET.OpenGL;
using System;
using System.Numerics;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;

namespace FPSRoguelike.UI;

public class HUD : IDisposable
{
    private GL gl = null!;
    private uint hudShaderProgram;
    private uint quadVAO, quadVBO, quadEBO;
    
    // Settings menu state
    private int selectedMenuItem = 0;
    private readonly string[] menuItems = { "Resume", "Mouse Sensitivity", "Field of View", "Exit Game" };
    private readonly string[] menuDescriptions = { 
        "Press ENTER to return to game",
        "Use ← → arrows to adjust look speed", 
        "Use ← → arrows to change camera FOV",
        "Press ENTER to quit to desktop"
    };
    private float mouseSensitivity = 0.3f;
    private float fieldOfView = 90f;
    
    // IDisposable pattern fields
    private bool disposed = false;
    
    public float MouseSensitivity => mouseSensitivity;
    public float FieldOfView => fieldOfView;
    public int SelectedMenuItem => selectedMenuItem;
    
    // HUD element positions and sizes
    private const float HEALTH_BAR_WIDTH = 0.3f;
    private const float HEALTH_BAR_HEIGHT = 0.03f;
    private const float HEALTH_BAR_X = -0.9f;
    private const float HEALTH_BAR_Y = -0.9f;
    
    private const float CROSSHAIR_SIZE = 0.02f;
    
    // Simple quad vertices for UI elements
    private readonly float[] quadVertices = 
    {
        // Position (2D)  // UV
        -1f, -1f,  0f, 0f,
         1f, -1f,  1f, 0f,
         1f,  1f,  1f, 1f,
        -1f,  1f,  0f, 1f,
    };
    
    private readonly uint[] quadIndices = { 0, 1, 2, 0, 2, 3 };
    
    // HUD shader source
    private const string HUD_VERTEX_SHADER = @"
        #version 330 core
        layout (location = 0) in vec2 aPos;
        layout (location = 1) in vec2 aTexCoord;
        
        uniform mat4 transform;
        out vec2 TexCoord;
        
        void main()
        {
            gl_Position = transform * vec4(aPos, 0.0, 1.0);
            TexCoord = aTexCoord;
        }
    ";
    
    private const string HUD_FRAGMENT_SHADER = @"
        #version 330 core
        out vec4 FragColor;
        in vec2 TexCoord;
        
        uniform vec4 color;
        
        void main()
        {
            FragColor = color;
        }
    ";
    
    public void Initialize(GL glContext)
    {
        gl = glContext;
        
        // Create HUD shader
        hudShaderProgram = CreateShaderProgram(HUD_VERTEX_SHADER, HUD_FRAGMENT_SHADER);
        
        // Create quad VAO/VBO for UI rendering
        quadVAO = gl.GenVertexArray();
        quadVBO = gl.GenBuffer();
        quadEBO = gl.GenBuffer();
        
        gl.BindVertexArray(quadVAO);
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, quadVBO);
        unsafe
        {
            fixed (float* v = quadVertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(quadVertices.Length * sizeof(float)), v, BufferUsageARB.StaticDraw);
            }
        }
        
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, quadEBO);
        unsafe
        {
            fixed (uint* i = quadIndices)
            {
                gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)(quadIndices.Length * sizeof(uint)), i, BufferUsageARB.StaticDraw);
            }
        }
        
        // Position attribute
        unsafe
        {
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)0);
            gl.EnableVertexAttribArray(0);
            
            // Texture coord attribute
            gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), (void*)(2 * sizeof(float)));
            gl.EnableVertexAttribArray(1);
        }
        
        gl.BindVertexArray(0);
    }
    
    public void Render(PlayerHealth? playerHealth, Weapon? weapon, int score, int waveNumber, int enemiesRemaining, bool isPaused = false, bool showSettings = false)
    {
        // Save current OpenGL state
        gl.Disable(EnableCap.DepthTest);
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        gl.UseProgram(hudShaderProgram);
        gl.BindVertexArray(quadVAO);
        
        // Draw pause overlay if paused
        if (isPaused)
        {
            // Dark overlay
            DrawQuad(-1f, -1f, 2f, 2f, new Vector4(0f, 0f, 0f, 0.7f));
            
            if (showSettings)
            {
                DrawSettingsMenu();
            }
            else
            {
                // Main pause menu
                DrawQuad(-0.3f, -0.05f, 0.6f, 0.35f, new Vector4(0.1f, 0.1f, 0.1f, 0.9f));
                DrawQuad(-0.3f, -0.05f, 0.6f, 0.01f, new Vector4(1f, 1f, 1f, 0.5f));
                DrawQuad(-0.3f, 0.29f, 0.6f, 0.01f, new Vector4(1f, 1f, 1f, 0.5f));
                DrawQuad(-0.3f, -0.05f, 0.01f, 0.35f, new Vector4(1f, 1f, 1f, 0.5f));
                DrawQuad(0.29f, -0.05f, 0.01f, 0.35f, new Vector4(1f, 1f, 1f, 0.5f));
                
                // "PAUSED" title
                DrawInfoBox(-0.15f, 0.2f, 0.3f, 0.06f, "PAUSED", new Vector4(0.8f, 0.2f, 0.2f, 0.8f));
                
                // Controls info
                DrawInfoBox(-0.25f, 0.1f, 0.5f, 0.04f, "Press TAB for Settings", new Vector4(0.2f, 0.2f, 0.3f, 0.6f));
                DrawInfoBox(-0.25f, 0.03f, 0.5f, 0.04f, "Press ESC to Resume", new Vector4(0.2f, 0.2f, 0.3f, 0.6f));
                
                // Game controls reminder
                DrawInfoBox(-0.25f, -0.08f, 0.5f, 0.03f, "Controls: WASD Move | Mouse Look | Space Jump", new Vector4(0.1f, 0.1f, 0.2f, 0.5f));
                DrawInfoBox(-0.25f, -0.13f, 0.5f, 0.03f, "LMB Shoot | R Respawn | F1 Debug | F2 Spawn Enemies", new Vector4(0.1f, 0.1f, 0.2f, 0.5f));
            }
            
            return; // Don't draw HUD when paused
        }
        
        // Draw health bar background
        DrawQuad(-0.9f, -0.9f, HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT, new Vector4(0.2f, 0.2f, 0.2f, 0.8f));
        
        // Draw health bar fill
        if (playerHealth != null)
        {
            float healthPercent = playerHealth.Health / playerHealth.MaxHealth;
            Vector4 healthColor = healthPercent > 0.5f ? new Vector4(0.2f, 0.8f, 0.2f, 1.0f) :
                                 healthPercent > 0.25f ? new Vector4(0.8f, 0.8f, 0.2f, 1.0f) :
                                 new Vector4(0.8f, 0.2f, 0.2f, 1.0f);
            
            DrawQuad(-0.9f, -0.9f, HEALTH_BAR_WIDTH * healthPercent, HEALTH_BAR_HEIGHT, healthColor);
            
            // Draw health numbers
            string healthText = $"{(int)playerHealth.Health}/{(int)playerHealth.MaxHealth}";
            DrawInfoBox(-0.65f, -0.93f, 0.15f, 0.06f, healthText, new Vector4(0.0f, 0.0f, 0.0f, 0.7f));
        }
        
        // Draw crosshair
        DrawCrosshair();
        
        // Draw score/wave info boxes
        DrawInfoBox(0.7f, 0.85f, 0.25f, 0.1f, $"Score: {score}", new Vector4(0.1f, 0.1f, 0.3f, 0.8f));
        DrawInfoBox(0.7f, 0.7f, 0.25f, 0.1f, $"Wave: {waveNumber}", new Vector4(0.3f, 0.1f, 0.1f, 0.8f));
        
        if (enemiesRemaining > 0)
        {
            DrawInfoBox(0.7f, 0.55f, 0.25f, 0.1f, $"Enemies: {enemiesRemaining}", new Vector4(0.5f, 0.1f, 0.1f, 0.8f));
        }
        
        // Draw weapon info
        if (weapon != null)
        {
            DrawInfoBox(-0.9f, 0.85f, 0.25f, 0.08f, weapon.Name, new Vector4(0.1f, 0.2f, 0.3f, 0.8f));
        }
        
        gl.BindVertexArray(0);
        gl.UseProgram(0);
        
        // Restore OpenGL state
        gl.Enable(EnableCap.DepthTest);
        gl.Disable(EnableCap.Blend);
    }
    
    private void DrawCrosshair()
    {
        Vector4 crosshairColor = new Vector4(1.0f, 1.0f, 1.0f, 0.8f);
        
        // Horizontal line
        DrawQuad(-CROSSHAIR_SIZE, -0.002f, CROSSHAIR_SIZE * 2, 0.004f, crosshairColor);
        
        // Vertical line
        DrawQuad(-0.002f, -CROSSHAIR_SIZE, 0.004f, CROSSHAIR_SIZE * 2, crosshairColor);
        
        // Center dot
        DrawQuad(-0.003f, -0.003f, 0.006f, 0.006f, new Vector4(1.0f, 0.2f, 0.2f, 1.0f));
    }
    
    private void DrawInfoBox(float x, float y, float width, float height, string text, Vector4 color)
    {
        // Draw background box
        DrawQuad(x, y, width, height, color);
        
        // Draw border
        float borderThickness = 0.005f;
        Vector4 borderColor = new Vector4(1.0f, 1.0f, 1.0f, 0.3f);
        
        // Top border
        DrawQuad(x, y + height - borderThickness, width, borderThickness, borderColor);
        // Bottom border
        DrawQuad(x, y, width, borderThickness, borderColor);
        // Left border
        DrawQuad(x, y, borderThickness, height, borderColor);
        // Right border
        DrawQuad(x + width - borderThickness, y, borderThickness, height, borderColor);
    }
    
    private void DrawQuad(float x, float y, float width, float height, Vector4 color)
    {
        Matrix4x4 transform = Matrix4x4.CreateScale(width / 2f, height / 2f, 1f) *
                             Matrix4x4.CreateTranslation(x + width / 2f, y + height / 2f, 0f);
        
        int transformLoc = gl.GetUniformLocation(hudShaderProgram, "transform");
        int colorLoc = gl.GetUniformLocation(hudShaderProgram, "color");
        
        unsafe
        {
            gl.UniformMatrix4(transformLoc, 1, false, (float*)&transform);
            gl.Uniform4(colorLoc, color.X, color.Y, color.Z, color.W);
        }
        
        unsafe
        {
            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }
    }
    
    private uint CreateShaderProgram(string vertexSource, string fragmentSource)
    {
        uint vertexShader = gl.CreateShader(ShaderType.VertexShader);
        gl.ShaderSource(vertexShader, vertexSource);
        gl.CompileShader(vertexShader);
        
        // Check vertex shader compilation
        gl.GetShader(vertexShader, ShaderParameterName.CompileStatus, out int vertexSuccess);
        if (vertexSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(vertexShader);
            throw new Exception($"HUD vertex shader compilation failed: {infoLog}");
        }
        
        uint fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        gl.ShaderSource(fragmentShader, fragmentSource);
        gl.CompileShader(fragmentShader);
        
        // Check fragment shader compilation
        gl.GetShader(fragmentShader, ShaderParameterName.CompileStatus, out int fragmentSuccess);
        if (fragmentSuccess == 0)
        {
            string infoLog = gl.GetShaderInfoLog(fragmentShader);
            throw new Exception($"HUD fragment shader compilation failed: {infoLog}");
        }
        
        uint program = gl.CreateProgram();
        gl.AttachShader(program, vertexShader);
        gl.AttachShader(program, fragmentShader);
        gl.LinkProgram(program);
        
        // Check shader program linking
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int linkSuccess);
        if (linkSuccess == 0)
        {
            string infoLog = gl.GetProgramInfoLog(program);
            throw new Exception($"HUD shader linking failed: {infoLog}");
        }
        
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        return program;
    }
    
    private void DrawSettingsMenu()
    {
        // Menu background
        float menuWidth = 0.6f;
        float menuHeight = 0.7f;
        float menuX = -menuWidth / 2f;
        float menuY = -menuHeight / 2f;
        
        DrawQuad(menuX, menuY, menuWidth, menuHeight, new Vector4(0.1f, 0.1f, 0.15f, 0.95f));
        
        // Menu border
        float borderThickness = 0.01f;
        Vector4 borderColor = new Vector4(0.3f, 0.5f, 0.8f, 1f);
        DrawQuad(menuX, menuY + menuHeight - borderThickness, menuWidth, borderThickness, borderColor);
        DrawQuad(menuX, menuY, menuWidth, borderThickness, borderColor);
        DrawQuad(menuX, menuY, borderThickness, menuHeight, borderColor);
        DrawQuad(menuX + menuWidth - borderThickness, menuY, borderThickness, menuHeight, borderColor);
        
        // Title
        float titleY = menuY + menuHeight - 0.1f;
        DrawInfoBox(menuX + 0.1f, titleY, menuWidth - 0.2f, 0.08f, "SETTINGS", new Vector4(0.2f, 0.3f, 0.5f, 0.8f));
        
        // Menu items
        float itemHeight = 0.08f;
        float itemSpacing = 0.02f;
        float startY = titleY - 0.15f;
        
        for (int i = 0; i < menuItems.Length; i++)
        {
            float itemY = startY - (i * (itemHeight + itemSpacing));
            Vector4 itemColor = i == selectedMenuItem ? 
                new Vector4(0.3f, 0.5f, 0.8f, 0.8f) : 
                new Vector4(0.15f, 0.15f, 0.2f, 0.6f);
            
            // Draw menu item box with label
            DrawInfoBox(menuX + 0.05f, itemY, menuWidth - 0.1f, itemHeight, menuItems[i], itemColor);
            
            // Draw description text below each item
            DrawInfoBox(menuX + 0.05f, itemY - 0.025f, menuWidth - 0.1f, 0.02f, menuDescriptions[i], new Vector4(0.3f, 0.3f, 0.4f, 0.4f));
            
            // Draw value indicators for settings
            if (i == 1) // Mouse Sensitivity
            {
                float barWidth = 0.2f;
                float barX = menuX + menuWidth - 0.3f;
                DrawQuad(barX, itemY + 0.02f, barWidth, 0.04f, new Vector4(0.1f, 0.1f, 0.1f, 0.8f));
                DrawQuad(barX, itemY + 0.02f, barWidth * (mouseSensitivity / 2f), 0.04f, new Vector4(0.5f, 0.8f, 0.3f, 1f));
            }
            else if (i == 2) // FOV
            {
                float barWidth = 0.2f;
                float barX = menuX + menuWidth - 0.3f;
                DrawQuad(barX, itemY + 0.02f, barWidth, 0.04f, new Vector4(0.1f, 0.1f, 0.1f, 0.8f));
                DrawQuad(barX, itemY + 0.02f, barWidth * ((fieldOfView - 60f) / 60f), 0.04f, new Vector4(0.5f, 0.8f, 0.3f, 1f));
            }
        }
        
        // Instructions
        float instructY = menuY + 0.05f;
        DrawInfoBox(menuX + 0.1f, instructY, menuWidth - 0.2f, 0.04f, "↑↓ Navigate | ←→ Adjust | ENTER Select | ESC Back", new Vector4(0.05f, 0.05f, 0.1f, 0.5f));
    }
    
    public void NavigateMenu(int direction)
    {
        selectedMenuItem = Math.Clamp(selectedMenuItem + direction, 0, menuItems.Length - 1);
    }
    
    public void AdjustSelectedSetting(float delta)
    {
        switch (selectedMenuItem)
        {
            case 1: // Mouse Sensitivity
                mouseSensitivity = Math.Clamp(mouseSensitivity + delta * 0.1f, 0.1f, 2f);
                break;
            case 2: // FOV
                fieldOfView = Math.Clamp(fieldOfView + delta * 5f, 60f, 120f);
                break;
        }
    }
    
    public string GetSelectedAction()
    {
        return menuItems[selectedMenuItem];
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
                if (quadVAO != 0)
                {
                    gl.DeleteVertexArray(quadVAO);
                    quadVAO = 0;
                }
                
                if (quadVBO != 0)
                {
                    gl.DeleteBuffer(quadVBO);
                    quadVBO = 0;
                }
                
                if (quadEBO != 0)
                {
                    gl.DeleteBuffer(quadEBO);
                    quadEBO = 0;
                }
                
                if (hudShaderProgram != 0)
                {
                    gl.DeleteProgram(hudShaderProgram);
                    hudShaderProgram = 0;
                }
            }
            
            disposed = true;
        }
    }
    
    ~HUD()
    {
        Dispose(false);
    }
}