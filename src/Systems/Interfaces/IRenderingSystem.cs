using Silk.NET.OpenGL;
using System.Numerics;

namespace FPSRoguelike.Systems.Interfaces;

/// <summary>
/// Handles all rendering operations and OpenGL resource management
/// </summary>
public interface IRenderingSystem : IGameSystem
{
    /// <summary>
    /// Initialize OpenGL resources and shaders
    /// </summary>
    void InitializeGraphics(GL gl, int screenWidth, int screenHeight);
    
    /// <summary>
    /// Begin a new frame
    /// </summary>
    void BeginFrame();
    
    /// <summary>
    /// End current frame
    /// </summary>
    void EndFrame();
    
    /// <summary>
    /// Render a cube at the specified transform
    /// </summary>
    void RenderCube(Matrix4x4 transform, Vector3 color);
    
    /// <summary>
    /// Render multiple cubes using instancing
    /// </summary>
    void RenderCubesInstanced(Matrix4x4[] transforms, Vector3[] colors, int count);
    
    /// <summary>
    /// Set the view and projection matrices for rendering
    /// </summary>
    void SetCamera(Matrix4x4 view, Matrix4x4 projection);
    
    /// <summary>
    /// Update viewport when window resizes
    /// </summary>
    void UpdateViewport(int width, int height);
    
    /// <summary>
    /// Get current aspect ratio
    /// </summary>
    float GetAspectRatio();
}