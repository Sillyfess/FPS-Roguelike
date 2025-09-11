using System.Numerics;
using Silk.NET.OpenGL;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Editor;

/// <summary>
/// UI renderer for the level editor
/// </summary>
public class EditorUI : IDisposable
{
    private GL? gl;
    private uint vao;
    private uint vbo;
    private uint ebo;
    private uint shaderProgram;
    
    // UI colors
    private static readonly Vector3 BACKGROUND_COLOR = new Vector3(0.1f, 0.1f, 0.1f);
    private static readonly Vector3 TEXT_COLOR = new Vector3(1f, 1f, 1f);
    private static readonly Vector3 HIGHLIGHT_COLOR = new Vector3(0.2f, 0.8f, 0.2f);
    private static readonly Vector3 WARNING_COLOR = new Vector3(1f, 0.8f, 0.1f);
    private static readonly Vector3 ERROR_COLOR = new Vector3(1f, 0.2f, 0.2f);
    
    // Shader source (reusing simple UI shader)
    private const string VERTEX_SHADER = @"
        #version 330 core
        layout(location = 0) in vec2 aPos;
        
        uniform vec2 uPosition;
        uniform vec2 uSize;
        
        void main()
        {
            vec2 pos = aPos * uSize + uPosition;
            gl_Position = vec4(pos * 2.0 - 1.0, 0.0, 1.0);
        }
    ";
    
    private const string FRAGMENT_SHADER = @"
        #version 330 core
        out vec4 FragColor;
        
        uniform vec3 uColor;
        uniform float uAlpha;
        
        void main()
        {
            FragColor = vec4(uColor, uAlpha);
        }
    ";
    
    private bool disposed = false;
    
    // Editor state info to display
    public class EditorState
    {
        public bool IsActive { get; set; }
        public string CurrentTool { get; set; } = "Place";
        public ObstacleType CurrentObstacleType { get; set; } = ObstacleType.Crate;
        public int SelectedObjectCount { get; set; } = 0;
        public bool GridSnapping { get; set; } = true;
        public float GridSize { get; set; } = 1f;
        public string? StatusMessage { get; set; }
        public DateTime StatusMessageTime { get; set; }
        public bool ShowHelp { get; set; } = false;
        public int UndoStackSize { get; set; } = 0;
        public int RedoStackSize { get; set; } = 0;
    }
    
    public EditorUI(GL openGL)
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
            throw new Exception($"EditorUI shader linking failed: {infoLog}");
        }
        
        // Cleanup shaders
        gl.DeleteShader(vertexShader);
        gl.DeleteShader(fragmentShader);
        
        // Create quad for UI elements
        float[] vertices = 
        {
            0.0f, 0.0f,
            1.0f, 0.0f,
            1.0f, 1.0f,
            0.0f, 1.0f
        };
        
        uint[] indices = { 0, 1, 2, 2, 3, 0 };
        
        // Create VAO, VBO, EBO
        vao = gl.GenVertexArray();
        vbo = gl.GenBuffer();
        ebo = gl.GenBuffer();
        
        gl.BindVertexArray(vao);
        
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        unsafe
        {
            fixed (float* v = vertices)
            {
                gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(vertices.Length * sizeof(float)), 
                    v, BufferUsageARB.StaticDraw);
            }
        }
        
        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, ebo);
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
            gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), (void*)0);
        }
        gl.EnableVertexAttribArray(0);
        
        gl.BindVertexArray(0);
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
    /// Render the editor UI
    /// </summary>
    public void Render(EditorState state, float screenWidth, float screenHeight)
    {
        if (gl == null || disposed || !state.IsActive) return;
        
        // Enable blending
        gl.Enable(EnableCap.Blend);
        gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        
        // Disable depth test for UI
        gl.Disable(EnableCap.DepthTest);
        
        gl.UseProgram(shaderProgram);
        gl.BindVertexArray(vao);
        
        // Top bar background
        DrawQuad(0f, 0f, 1f, 0.04f, BACKGROUND_COLOR, 0.8f);
        
        // Status bar at bottom
        DrawQuad(0f, 0.96f, 1f, 0.04f, BACKGROUND_COLOR, 0.8f);
        
        // Tool info panel (top-left)
        DrawQuad(0.01f, 0.05f, 0.2f, 0.15f, BACKGROUND_COLOR, 0.6f);
        
        // If help is shown, draw help panel
        if (state.ShowHelp)
        {
            DrawQuad(0.25f, 0.2f, 0.5f, 0.6f, BACKGROUND_COLOR, 0.9f);
        }
        
        // Draw status message if recent
        if (!string.IsNullOrEmpty(state.StatusMessage))
        {
            var elapsed = DateTime.Now - state.StatusMessageTime;
            if (elapsed.TotalSeconds < 3)
            {
                float alpha = 1f - (float)(elapsed.TotalSeconds / 3.0);
                DrawQuad(0.35f, 0.45f, 0.3f, 0.05f, WARNING_COLOR, alpha);
            }
        }
        
        gl.BindVertexArray(0);
        gl.UseProgram(0);
        
        // Re-enable depth test
        gl.Enable(EnableCap.DepthTest);
        
        // Note: In a real implementation, we'd render text here using a text rendering library
        // For now, we're just rendering colored rectangles as placeholders
        RenderText(state, screenWidth, screenHeight);
    }
    
    private void DrawQuad(float x, float y, float width, float height, Vector3 color, float alpha)
    {
        if (gl == null) return;
        
        int posLoc = gl.GetUniformLocation(shaderProgram, "uPosition");
        int sizeLoc = gl.GetUniformLocation(shaderProgram, "uSize");
        int colorLoc = gl.GetUniformLocation(shaderProgram, "uColor");
        int alphaLoc = gl.GetUniformLocation(shaderProgram, "uAlpha");
        
        if (posLoc != -1) gl.Uniform2(posLoc, x, y);
        if (sizeLoc != -1) gl.Uniform2(sizeLoc, width, height);
        if (colorLoc != -1) gl.Uniform3(colorLoc, color.X, color.Y, color.Z);
        if (alphaLoc != -1) gl.Uniform1(alphaLoc, alpha);
        
        unsafe
        {
            gl.DrawElements(PrimitiveType.Triangles, 6, DrawElementsType.UnsignedInt, (void*)0);
        }
    }
    
    private void RenderText(EditorState state, float screenWidth, float screenHeight)
    {
        // This is a placeholder for text rendering
        // In a real implementation, we would use a text rendering library like FreeType
        // or render text to a texture
        
        // For now, let's just output the state to console when it changes
        // This helps with debugging
    }
    
    /// <summary>
    /// Get formatted help text
    /// </summary>
    public static string GetHelpText()
    {
        return @"
LEVEL EDITOR CONTROLS
=====================
F3          - Toggle Editor Mode
Tab         - Cycle Object Types
LMB         - Place/Select Object
RMB         - Delete Object
MMB Drag    - Pan Camera
Scroll      - Adjust Distance

CAMERA:
WASD        - Move Camera
Q/E         - Move Up/Down
Shift       - Fast Move
Ctrl        - Slow Move
Mouse       - Look Around

EDITING:
G           - Move Selected
R           - Rotate Selected
Ctrl+D      - Duplicate
Delete      - Delete Selected
Ctrl+A      - Select All
Ctrl+Z      - Undo
Ctrl+Y      - Redo

FILES:
Ctrl+S      - Save Level
Ctrl+O      - Open Level
Ctrl+N      - New Level

OPTIONS:
H           - Toggle Help
Grid        - Toggle Grid Snap (1)
[/]         - Adjust Grid Size
";
    }
    
    public void Dispose()
    {
        if (disposed) return;
        
        if (gl != null)
        {
            gl.DeleteVertexArray(vao);
            gl.DeleteBuffer(vbo);
            gl.DeleteBuffer(ebo);
            gl.DeleteProgram(shaderProgram);
        }
        
        disposed = true;
    }
}