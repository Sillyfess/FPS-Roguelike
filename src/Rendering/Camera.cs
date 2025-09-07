using System.Numerics;

namespace FPSRoguelike.Rendering;

public class Camera
{
    public Vector3 Position { get; set; }
    public float Pitch { get; private set; }
    public float Yaw { get; private set; }
    
    public float FieldOfView { get; set; } = 90f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 1000f;
    
    private float mouseSensitivity = 0.002f;
    private float maxPitch = 89f * (MathF.PI / 180f);
    
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
        if (Yaw > MathF.PI * 2f)
            Yaw -= MathF.PI * 2f;
        else if (Yaw < -MathF.PI * 2f)
            Yaw += MathF.PI * 2f;
    }
    
    public void UpdateMatrices(float aspectRatio)
    {
        // Calculate forward vector from rotation
        Vector3 forward = GetForwardVector();
        
        Vector3 worldUp = Vector3.UnitY;
        Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        Vector3 up = Vector3.Cross(forward, right);
        
        ViewMatrix = Matrix4x4.CreateLookAt(Position, Position + forward, up);
        ProjectionMatrix = Matrix4x4.CreatePerspectiveFieldOfView(
            FieldOfView * (MathF.PI / 180f),
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
        mouseSensitivity = Math.Clamp(sensitivity, 0.0001f, 0.01f);
    }
}