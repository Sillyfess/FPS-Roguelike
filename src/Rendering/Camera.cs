using System.Numerics;

namespace FPSRoguelike.Rendering;

public class Camera
{
    public Vector3 Position { get; set; }
    public float Pitch { get; private set; }
    public float Yaw { get; private set; }
    
    // Camera constants
    private const float DEFAULT_FIELD_OF_VIEW = 90f;
    private const float DEFAULT_NEAR_PLANE = 0.1f;
    private const float DEFAULT_FAR_PLANE = 1000f;
    private const float DEFAULT_MOUSE_SENSITIVITY = 0.006f;
    private const float MAX_PITCH_DEGREES = 89f;
    private const float MIN_SENSITIVITY = 0.0001f;
    private const float MAX_SENSITIVITY = 0.01f;
    private const float DEG_TO_RAD = MathF.PI / 180f;
    private const float TWO_PI = MathF.PI * 2f;
    
    public float FieldOfView { get; set; } = DEFAULT_FIELD_OF_VIEW;
    public float NearPlane { get; set; } = DEFAULT_NEAR_PLANE;
    public float FarPlane { get; set; } = DEFAULT_FAR_PLANE;
    
    public void SetFieldOfView(float fov)
    {
        FieldOfView = Math.Clamp(fov, 60f, 120f);
    }
    
    private float mouseSensitivity = DEFAULT_MOUSE_SENSITIVITY;
    private float maxPitch = MAX_PITCH_DEGREES * DEG_TO_RAD;
    
    public Matrix4x4 ViewMatrix { get; private set; }
    public Matrix4x4 ProjectionMatrix { get; private set; }
    
    public Camera(Vector3 position)
    {
        Position = position;
        Pitch = 0f;
        Yaw = 0f;
    }
    
    public void UpdateRotation(Vector2 mouseDelta)
    {
        Yaw -= mouseDelta.X * mouseSensitivity;  // Fixed: negated for correct mouse look
        Pitch -= mouseDelta.Y * mouseSensitivity;
        
        // Clamp pitch to prevent flipping
        Pitch = Math.Clamp(Pitch, -maxPitch, maxPitch);
        
        // Wrap yaw to keep it in reasonable range
        if (Yaw > TWO_PI)
            Yaw -= TWO_PI;
        else if (Yaw < -TWO_PI)
            Yaw += TWO_PI;
    }
    
    public void UpdateMatrices(float aspectRatio, float? fov = null)
    {
        // Update FOV if provided
        if (fov.HasValue)
        {
            FieldOfView = fov.Value;
        }
        
        // Calculate forward vector from rotation
        Vector3 forward = GetForwardVector();
        
        Vector3 worldUp = Vector3.UnitY;
        Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        Vector3 up = Vector3.Cross(forward, right);
        
        ViewMatrix = Matrix4x4.CreateLookAt(Position, Position + forward, up);
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * DEG_TO_RAD,
            aspectRatio,
            NearPlane,
            FarPlane
        );
    }
    
    public Vector3 GetForwardVector()
    {
        return new Vector3(
            MathF.Cos(Pitch) * MathF.Sin(Yaw),
            MathF.Sin(Pitch),
            MathF.Cos(Pitch) * MathF.Cos(Yaw)
        );
    }
    
    public Vector3 GetRightVector()
    {
        Vector3 forward = GetForwardVector();
        return Vector3.Normalize(Vector3.Cross(Vector3.UnitY, forward));
    }
    
    public Vector3 GetForwardMovement()
    {
        // Forward movement vector (no vertical component for walking)
        return Vector3.Normalize(new Vector3(
            MathF.Sin(Yaw),
            0,
            MathF.Cos(Yaw)
        ));
    }
    
    public Vector3 GetRightMovement()
    {
        // Right movement vector (no vertical component for walking)
        return Vector3.Normalize(new Vector3(
            -MathF.Cos(Yaw),  // Fixed: negated for correct direction
            0,
            MathF.Sin(Yaw)     // Fixed: removed negation
        ));
    }
    
    public void SetMouseSensitivity(float sensitivity)
    {
        mouseSensitivity = Math.Clamp(sensitivity, MIN_SENSITIVITY, MAX_SENSITIVITY);
    }
}