using System.Numerics;

namespace FPSRoguelike.Physics;

// Controls player movement physics, gravity, and jumping
public class CharacterController
{
    // Movement constants
    private const float DEFAULT_MOVE_SPEED = 10f;
    private const float DEFAULT_JUMP_HEIGHT = 2f;
    private const float DEFAULT_GRAVITY = -20f;
    private const float DEFAULT_AIR_CONTROL = 0.3f;
    
    // Movement parameters
    public float MoveSpeed { get; set; } = DEFAULT_MOVE_SPEED;
    public float JumpHeight { get; set; } = DEFAULT_JUMP_HEIGHT;
    public float Gravity { get; set; } = DEFAULT_GRAVITY;
    public float AirControl { get; set; } = DEFAULT_AIR_CONTROL;
    
    // State
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public bool IsGrounded { get; private set; }
    
    // Collision constants
    private const float DEFAULT_PLAYER_HEIGHT = 1.8f;
    private const float DEFAULT_GROUND_CHECK_DISTANCE = 0.1f;
    private const float DEFAULT_GROUND_LEVEL = 0f;
    private const float JUMP_VELOCITY_MULTIPLIER = 2f;
    
    // Collision parameters
    private float playerHeight = DEFAULT_PLAYER_HEIGHT;
    private float groundCheckDistance = DEFAULT_GROUND_CHECK_DISTANCE;
    private float groundLevel = DEFAULT_GROUND_LEVEL;
    
    public CharacterController(Vector3 startPosition)
    {
        Position = startPosition;
        Velocity = Vector3.Zero;
    }
    
    public void Update(Vector3 moveInput, bool jumpPressed, float deltaTime)
    {
        // Simple ground check - player is grounded if feet are near ground and falling
        float feetPosition = Position.Y - (playerHeight / 2f);
        IsGrounded = feetPosition <= groundLevel + groundCheckDistance && Velocity.Y <= 0;
        
        // Horizontal movement
        Vector3 horizontalVelocity = moveInput * MoveSpeed;
        
        if (IsGrounded)
        {
            // Full speed control on ground
            Velocity = new Vector3(horizontalVelocity.X, Velocity.Y, horizontalVelocity.Z);
            
            // Stop falling when we hit ground
            if (Velocity.Y < 0)
                Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
            
            // Jump physics - calculate initial velocity for desired height
            if (jumpPressed)
            {
                Velocity = new Vector3(Velocity.X, MathF.Sqrt(JUMP_VELOCITY_MULTIPLIER * JumpHeight * MathF.Abs(Gravity)), Velocity.Z);
                IsGrounded = false;
            }
        }
        else
        {
            // Limited air control - lerp towards desired velocity
            Vector3 targetVelocity = horizontalVelocity;
            Velocity = new Vector3(
                Lerp(Velocity.X, targetVelocity.X, AirControl * deltaTime),
                Velocity.Y,  // Don't affect vertical velocity
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