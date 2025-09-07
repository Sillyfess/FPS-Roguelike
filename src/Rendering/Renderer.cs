using Silk.NET.OpenGL;
using System.Numerics;

namespace FPSRoguelike.Rendering;

public class Renderer
{
    private GL? gl;
    private int screenWidth;
    private int screenHeight;
    
    public void Initialize(GL glContext, int width, int height)
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
    }
    
    public void UpdateViewport(int width, int height)
    {
        screenWidth = width;
        screenHeight = height;
        gl?.Viewport(0, 0, (uint)screenWidth, (uint)screenHeight);
    }
    
    public float GetAspectRatio()
    {
        // Prevent division by zero when window is minimized
        // Store values locally to prevent race conditions
        int height = screenHeight;
        int width = screenWidth;
        
        if (height <= 0)
        {
            return 16.0f / 9.0f; // Default aspect ratio
        }
        return (float)width / height;
    }
    
    public void Clear(Vector3 clearColor)
    {
        gl?.ClearColor(clearColor.X, clearColor.Y, clearColor.Z, 1.0f);
        gl?.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }
    
    public void Cleanup()
    {
        // Cleanup resources if needed
    }
}