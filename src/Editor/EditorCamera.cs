using System.Numerics;

namespace FPSRoguelike.Editor;

/// <summary>
/// Free-flying camera for level editor navigation
/// </summary>
public class EditorCamera
{
    // Camera properties
    public Vector3 Position { get; set; }
    public float Yaw { get; private set; }
    public float Pitch { get; private set; }
    public float FieldOfView { get; set; } = 90f;
    
    // Movement settings
    private const float DEFAULT_MOVE_SPEED = 10f;
    private const float FAST_MOVE_MULTIPLIER = 3f;
    private const float SLOW_MOVE_MULTIPLIER = 0.3f;
    private const float MOUSE_SENSITIVITY = 0.3f;
    private const float MAX_PITCH = 89f;
    private const float MIN_PITCH = -89f;
    private const float SCROLL_SPEED_MULTIPLIER = 1.1f;
    
    // Current movement speed
    private float currentMoveSpeed = DEFAULT_MOVE_SPEED;
    
    // Camera vectors
    public Vector3 Forward { get; private set; }
    public Vector3 Right { get; private set; }
    public Vector3 Up { get; private set; }
    
    public EditorCamera(Vector3 position, float yaw = 0f, float pitch = 0f)
    {
        Position = position;
        Yaw = yaw;
        Pitch = pitch;
        UpdateVectors();
    }
    
    /// <summary>
    /// Update camera based on input
    /// </summary>
    public void Update(float deltaTime, Vector3 moveInput, bool fastMove, bool slowMove, float scrollDelta)
    {
        // Adjust move speed based on modifiers
        float moveSpeed = DEFAULT_MOVE_SPEED;
        if (fastMove)
            moveSpeed *= FAST_MOVE_MULTIPLIER;
        else if (slowMove)
            moveSpeed *= SLOW_MOVE_MULTIPLIER;
            
        // Adjust speed with scroll wheel
        if (Math.Abs(scrollDelta) > 0.01f)
        {
            currentMoveSpeed *= MathF.Pow(SCROLL_SPEED_MULTIPLIER, scrollDelta);
            currentMoveSpeed = Math.Clamp(currentMoveSpeed, 1f, 100f);
        }
        else
        {
            currentMoveSpeed = moveSpeed;
        }
        
        // Calculate movement
        Vector3 movement = Vector3.Zero;
        
        // Forward/backward (W/S)
        movement += Forward * moveInput.Z * currentMoveSpeed * deltaTime;
        
        // Right/left (A/D)
        movement += Right * moveInput.X * currentMoveSpeed * deltaTime;
        
        // Up/down (Q/E or Space/Ctrl)
        movement += Vector3.UnitY * moveInput.Y * currentMoveSpeed * deltaTime;
        
        Position += movement;
    }
    
    /// <summary>
    /// Rotate camera based on mouse movement
    /// </summary>
    public void Rotate(float deltaX, float deltaY, float sensitivity = MOUSE_SENSITIVITY)
    {
        Yaw += deltaX * sensitivity;
        Pitch -= deltaY * sensitivity; // Inverted
        
        // Clamp pitch to prevent flipping
        Pitch = Math.Clamp(Pitch, MIN_PITCH, MAX_PITCH);
        
        // Wrap yaw
        while (Yaw > 360f) Yaw -= 360f;
        while (Yaw < 0f) Yaw += 360f;
        
        UpdateVectors();
    }
    
    /// <summary>
    /// Update camera direction vectors based on yaw and pitch
    /// </summary>
    private void UpdateVectors()
    {
        float yawRad = MathF.PI * Yaw / 180f;
        float pitchRad = MathF.PI * Pitch / 180f;
        
        // Calculate forward vector
        Forward = new Vector3(
            MathF.Sin(yawRad) * MathF.Cos(pitchRad),
            MathF.Sin(pitchRad),
            -MathF.Cos(yawRad) * MathF.Cos(pitchRad)
        );
        Forward = Vector3.Normalize(Forward);
        
        // Calculate right vector
        Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        
        // Calculate up vector
        Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
    }
    
    /// <summary>
    /// Get view matrix for rendering
    /// </summary>
    public Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);
    }
    
    /// <summary>
    /// Get projection matrix for rendering
    /// </summary>
    public Matrix4x4 GetProjectionMatrix(float aspectRatio, float nearPlane = 0.1f, float farPlane = 1000f)
    {
        return Matrix4x4.CreatePerspectiveFieldOfView(
            MathF.PI * FieldOfView / 180f,
            aspectRatio,
            nearPlane,
            farPlane
        );
    }
    
    /// <summary>
    /// Get a ray from camera position in view direction
    /// </summary>
    public (Vector3 origin, Vector3 direction) GetViewRay()
    {
        return (Position, Forward);
    }
    
    /// <summary>
    /// Get a ray from camera through screen coordinates
    /// </summary>
    public (Vector3 origin, Vector3 direction) GetScreenRay(float screenX, float screenY, float screenWidth, float screenHeight)
    {
        // Convert screen coords to NDC (-1 to 1)
        float ndcX = (2f * screenX / screenWidth) - 1f;
        float ndcY = 1f - (2f * screenY / screenHeight); // Inverted Y
        
        // Create ray in view space
        float tanFov = MathF.Tan(MathF.PI * FieldOfView / 360f);
        float aspectRatio = screenWidth / screenHeight;
        
        Vector3 rayViewSpace = new Vector3(
            ndcX * tanFov * aspectRatio,
            ndcY * tanFov,
            -1f
        );
        
        // Transform to world space
        Vector3 rayWorld = Right * rayViewSpace.X + Up * rayViewSpace.Y + Forward * (-rayViewSpace.Z);
        rayWorld = Vector3.Normalize(rayWorld);
        
        return (Position, rayWorld);
    }
    
    /// <summary>
    /// Focus camera on a specific point
    /// </summary>
    public void LookAt(Vector3 target)
    {
        Vector3 direction = Vector3.Normalize(target - Position);
        
        // Calculate yaw and pitch from direction
        Yaw = MathF.Atan2(direction.X, -direction.Z) * 180f / MathF.PI;
        Pitch = MathF.Asin(direction.Y) * 180f / MathF.PI;
        
        UpdateVectors();
    }
    
    /// <summary>
    /// Move camera to look at target from a specific distance
    /// </summary>
    public void FocusOn(Vector3 target, float distance = 10f)
    {
        // Position camera at distance from target, looking down at 45 degrees
        Position = target + new Vector3(0, distance * 0.7f, -distance * 0.7f);
        LookAt(target);
    }
}