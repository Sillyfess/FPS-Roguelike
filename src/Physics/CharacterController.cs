using System.Numerics;

namespace FPSRoguelike.Physics;

public class CharacterController
{
    // Movement parameters
    public float MoveSpeed { get; set; } = 10f;
    public float JumpHeight { get; set; } = 2f;
    public float Gravity { get; set; } = -20f;
    public float AirControl { get; set; } = 0.3f;
    
    // State
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public bool IsGrounded { get; private set; }
    
    // Collision parameters
    private float playerHeight = 1.8f;
    private float groundCheckDistance = 0.1f;
    private float groundLevel = 0f;  // Simple ground plane at y=0
    
    public CharacterController(Vector3 startPosition)
    {
        Position = startPosition;
        Velocity = Vector3.Zero;
    }
    
    public void Update(Vector3 moveInput, bool jumpPressed, float deltaTime)
    {
        // Ground check (simple for now - just check if we're near ground level)
        float feetPosition = Position.Y - (playerHeight / 2f);
        IsGrounded = feetPosition <= groundLevel + groundCheckDistance && Velocity.Y <= 0;
        
        // Horizontal movement
        Vector3 horizontalVelocity = moveInput * MoveSpeed;
        
        if (IsGrounded)
        {
            // Full control on ground
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);
            
            // Reset vertical velocity when grounded
            if (Velocity.Y < 0)
                Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
            
            // Jump
            if (jumpPressed)
            {
                Velocity = new Vector3(Velocity.X, MathF.Sqrt(2f * JumpHeight * -Gravity), Velocity.Z);
                IsGrounded = false;
            }
        }
        else
        {
            // Limited air control
            Vector3 targetVelocity = horizontalVelocity;
            Velocity = new Vector3(
                Lerp(Velocity.X, targetVelocity.X, AirControl * deltaTime),
                Velocity.Y,
                Lerp(Velocity.Z, targetVelocity.Z, AirControl * deltaTime)
            );
        }
        
        // Apply gravity
        if (!IsGrounded)
        {
            Velocity += new Vector3(0, Gravity * deltaTime, 0);
        }
        
        // Apply movement
        Vector3 deltaPosition = Velocity * deltaTime;
        Position += deltaPosition;
        
        // Prevent falling through ground
        feetPosition = Position.Y - (playerHeight / 2f);
        if (feetPosition < groundLevel)
        {
            Position = new Vector3(Position.X, groundLevel + (playerHeight / 2f), Position.Z);
            if (Velocity.Y < 0)
                Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }
    }
    
    private float Lerp(float a, float b, float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return a + (b - a) * t;
    }
    
    public void SetGroundLevel(float level)
    {
        groundLevel = level;
    }
}