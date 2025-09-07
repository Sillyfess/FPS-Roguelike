using System.Numerics;
using FPSRoguelike.Environment;

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
    private const float PLAYER_RADIUS = 0.5f;
    
    // Collision parameters
    private float playerHeight = DEFAULT_PLAYER_HEIGHT;
    private float groundCheckDistance = DEFAULT_GROUND_CHECK_DISTANCE;
    private float groundLevel = DEFAULT_GROUND_LEVEL;
    
    public CharacterController(Vector3 startPosition)
    {
        Position = startPosition;
        Velocity = Vector3.Zero;
    }
    
    public void Update(Vector3 moveInput, bool jumpPressed, float deltaTime, List<Obstacle>? obstacles = null)
    {
        // Validate inputs to prevent physics corruption
        if (float.IsNaN(moveInput.X) || float.IsNaN(moveInput.Y) || float.IsNaN(moveInput.Z) ||
            float.IsInfinity(moveInput.X) || float.IsInfinity(moveInput.Y) || float.IsInfinity(moveInput.Z))
        {
            moveInput = Vector3.Zero;
        }
        
        if (deltaTime <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
        {
            return; // Skip invalid frame
        }
        
        // Ground check - player is grounded if feet are near ground or on an obstacle
        float feetPosition = Position.Y - (playerHeight / 2f);
        const float EPSILON = 0.001f;
        bool onGround = feetPosition <= groundLevel + groundCheckDistance + EPSILON && Velocity.Y <= 0;
        
        // Also check if standing on any obstacle
        bool onObstacle = false;
        if (obstacles != null && Velocity.Y <= 0)
        {
                // Check slightly below player for obstacle tops
            Vector3 checkPos = new Vector3(Position.X, Position.Y - (playerHeight / 2f) - groundCheckDistance, Position.Z);
            
            foreach (var obstacle in obstacles)
            {
                if (obstacle.IsDestroyed) continue;
                
                // Check if we're above the obstacle and within its horizontal bounds
                if (Position.Y > obstacle.MaxBounds.Y && 
                    Position.Y <= obstacle.MaxBounds.Y + playerHeight / 2f + 0.1f &&
                    Position.X >= obstacle.MinBounds.X - PLAYER_RADIUS && 
                    Position.X <= obstacle.MaxBounds.X + PLAYER_RADIUS &&
                    Position.Z >= obstacle.MinBounds.Z - PLAYER_RADIUS && 
                    Position.Z <= obstacle.MaxBounds.Z + PLAYER_RADIUS)
                {
                    onObstacle = true;
                    break;
                }
            }
        }
        
        IsGrounded = onGround || onObstacle;
        
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
        
        // Apply movement with collision detection - handle each axis separately
        Vector3 deltaPosition = Velocity * deltaTime;
        
        if (obstacles != null && obstacles.Count > 0)
        {
            // Handle X movement
            if (MathF.Abs(deltaPosition.X) > 0.0001f)
            {
                Vector3 xStep = new Vector3(Position.X + deltaPosition.X, Position.Y, Position.Z);
                bool xCollision = false;
                
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IsDestroyed) continue;
                    if (obstacle.CheckCollision(xStep, PLAYER_RADIUS))
                    {
                        xCollision = true;
                        // Stop X velocity on collision
                        Velocity = new Vector3(0, Velocity.Y, Velocity.Z);
                        break;
                    }
                }
                
                if (!xCollision)
                {
                    Position = new Vector3(xStep.X, Position.Y, Position.Z);
                }
            }
            
            // Handle Y movement (jumping/falling)
            if (MathF.Abs(deltaPosition.Y) > 0.0001f)
            {
                Vector3 yStep = new Vector3(Position.X, Position.Y + deltaPosition.Y, Position.Z);
                bool yCollision = false;
                
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IsDestroyed) continue;
                    if (obstacle.CheckCollision(yStep, PLAYER_RADIUS))
                    {
                        yCollision = true;
                        
                        // Check if we're landing on top of the obstacle
                        if (Velocity.Y < 0 && Position.Y > obstacle.MaxBounds.Y)
                        {
                            // Land on top of the obstacle
                            Position = new Vector3(Position.X, obstacle.MaxBounds.Y + playerHeight / 2f + 0.01f, Position.Z);
                            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
                            IsGrounded = true;
                        }
                        // Check if we hit our head on the bottom
                        else if (Velocity.Y > 0 && Position.Y < obstacle.MinBounds.Y)
                        {
                            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
                        }
                        break;
                    }
                }
                
                if (!yCollision)
                {
                    Position = new Vector3(Position.X, yStep.Y, Position.Z);
                }
            }
            
            // Handle Z movement
            if (MathF.Abs(deltaPosition.Z) > 0.0001f)
            {
                Vector3 zStep = new Vector3(Position.X, Position.Y, Position.Z + deltaPosition.Z);
                bool zCollision = false;
                
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IsDestroyed) continue;
                    if (obstacle.CheckCollision(zStep, PLAYER_RADIUS))
                    {
                        zCollision = true;
                        // Stop Z velocity on collision
                        Velocity = new Vector3(Velocity.X, Velocity.Y, 0);
                        break;
                    }
                }
                
                if (!zCollision)
                {
                    Position = new Vector3(Position.X, Position.Y, zStep.Z);
                }
            }
        }
        else
        {
            Position += deltaPosition;
        }
        
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