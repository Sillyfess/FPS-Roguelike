using System.Numerics;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Entities;

// Enemy AI states for the state machine
public enum EnemyState
{
    Idle,       // Standing still, checking for player
    Patrolling, // Moving to random points
    Chasing,    // Actively pursuing the player
    Attacking,  // In range and firing projectiles
    Charging,   // Melee charge attack
    Dead        // Destroyed, awaiting cleanup
}

public class Enemy
{
    // Properties
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsAlive => Health > 0;
    public bool IsActive { get; set; } = true;
    public int Id { get; private set; }
    
    // Movement constants
    private const float DEFAULT_MOVE_SPEED = 3f;
    private const float DEFAULT_CHASE_SPEED = 5f;
    private const float DEFAULT_ROTATION_SPEED = 3f;
    
    // Combat constants
    private const float DEFAULT_ATTACK_RANGE = 15f;
    private const float DEFAULT_ATTACK_COOLDOWN = 2f;
    private const float DEFAULT_PROJECTILE_SPEED = 20f;
    private const float DEFAULT_DAMAGE = 10f;
    
    // Melee charge constants
    private const float CHARGE_RANGE = 8f; // Distance to trigger charge
    private const float CHARGE_SPEED = 15f; // Fast charge speed
    private const float CHARGE_DAMAGE = 25f; // High melee damage
    private const float CHARGE_DURATION = 0.5f; // How long the charge lasts
    private const float CHARGE_COOLDOWN = 3f; // Cooldown between charges
    private const float CHARGE_HIT_RANGE = 2f; // How close to deal damage
    
    // Movement parameters
    protected float moveSpeed = DEFAULT_MOVE_SPEED;
    protected float chaseSpeed = DEFAULT_CHASE_SPEED;
    
    // Combat parameters  
    protected float attackRange = DEFAULT_ATTACK_RANGE;
    protected float attackCooldown = DEFAULT_ATTACK_COOLDOWN;
    protected float projectileSpeed = DEFAULT_PROJECTILE_SPEED;
    protected float damage = DEFAULT_DAMAGE;
    
    // AI State
    protected EnemyState currentState = EnemyState.Idle;
    protected float stateTimer = 0f;
    protected float lastAttackTime = 0f;
    protected float lastChargeTime = 0f;
    protected Vector3 targetPosition;
    protected Vector3 patrolTarget;
    protected Vector3 chargeDirection;
    protected float yRotation = 0f;
    protected bool hasDealtChargeDamage = false;
    
    // Detection constants
    private const float DEFAULT_DETECTION_RANGE = 20f;
    private const float DEFAULT_LOSE_TARGET_RANGE = 30f;
    
    // Detection parameters
    protected float detectionRange = DEFAULT_DETECTION_RANGE;
    protected float loseTargetRange = DEFAULT_LOSE_TARGET_RANGE;
    
    // Visual
    public Vector3 Color { get; protected set; } = new Vector3(1.0f, 0.2f, 0.2f); // Red color
    protected float hitFlashTimer = 0f;
    private const float HIT_FLASH_DURATION = 0.1f;
    
    // Hitstop - individual enemy freezing on hit
    private float hitstopTimer = 0f;
    private const float HITSTOP_DURATION = 0.05f; // 50ms freeze
    private const float HITSTOP_TIME_SCALE = 0.1f; // 10% speed during hitstop
    public bool IsInHitstop => hitstopTimer > 0f;
    private const float GROUND_LEVEL = 1f;
    private const float GRAVITY_STRENGTH = 20f;
    private const float IDLE_TIMEOUT = 2f;
    private const float PATROL_REACH_DISTANCE = 0.5f;
    private const float MIN_PATROL_DISTANCE = 5f;
    private const float MAX_PATROL_DISTANCE = 10f;
    private const float EPSILON = 0.01f;
    
    private static int nextId = 0;
    
    public Enemy(Vector3 startPosition, float health = 30f)
    {
        // Validate parameters
        if (float.IsNaN(startPosition.X) || float.IsNaN(startPosition.Y) || float.IsNaN(startPosition.Z) ||
            float.IsInfinity(startPosition.X) || float.IsInfinity(startPosition.Y) || float.IsInfinity(startPosition.Z))
        {
            throw new ArgumentException("Start position contains invalid values", nameof(startPosition));
        }
        
        if (health <= 0 || float.IsNaN(health) || float.IsInfinity(health))
        {
            throw new ArgumentException("Health must be positive and finite", nameof(health));
        }
        
        // Use thread-safe atomic increment for ID generation
        Id = System.Threading.Interlocked.Increment(ref nextId);
        Position = startPosition;
        MaxHealth = health;
        Health = health;
        
        // Start with a random patrol target
        GenerateNewPatrolTarget();
    }
    
    public void Update(float deltaTime, Vector3 playerPosition, List<Obstacle>? obstacles = null)
    {
        // Dead enemies don't update
        if (!IsAlive)
        {
            currentState = EnemyState.Dead;
            return;
        }
        
        // Update hitstop timer first (always at normal speed)
        if (hitstopTimer > 0)
        {
            hitstopTimer -= deltaTime;
            if (hitstopTimer < 0) hitstopTimer = 0;
        }
        
        // Apply time scale during hitstop for this enemy only
        float scaledDeltaTime = IsInHitstop ? deltaTime * HITSTOP_TIME_SCALE : deltaTime;
        
        // Update timers with scaled time
        stateTimer += scaledDeltaTime;
        if (hitFlashTimer > 0)
        {
            hitFlashTimer -= deltaTime; // Visual flash uses real time
            Color = hitFlashTimer > 0 ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(1.0f, 0.2f, 0.2f);
        }
        
        // Check player distance
        float distanceToPlayer = Vector3.Distance(Position, playerPosition);
        targetPosition = playerPosition;
        
        // Execute behavior based on current AI state (only if not in hitstop)
        if (!IsInHitstop)
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    HandleIdleState(distanceToPlayer);
                    break;
                    
                case EnemyState.Patrolling:
                    HandlePatrollingState(scaledDeltaTime, distanceToPlayer);
                    break;
                    
                case EnemyState.Chasing:
                    HandleChasingState(scaledDeltaTime, distanceToPlayer);
                    break;
                    
                case EnemyState.Attacking:
                    HandleAttackingState(scaledDeltaTime, distanceToPlayer);
                    break;
                    
                case EnemyState.Charging:
                    HandleChargingState(scaledDeltaTime, distanceToPlayer, playerPosition);
                    break;
            }
        }
        else
        {
            // During hitstop, enemy is frozen - no state updates
            Velocity = Vector3.Zero;
        }
        
        // Apply simple gravity and velocity (only if not in hitstop)
        if (!IsInHitstop)
        {
            if (Position.Y > GROUND_LEVEL + EPSILON)
            {
                Velocity = new Vector3(Velocity.X, Velocity.Y - GRAVITY_STRENGTH * scaledDeltaTime, Velocity.Z);
            }
            else if (Position.Y < GROUND_LEVEL)
            {
                Position = new Vector3(Position.X, GROUND_LEVEL, Position.Z);
                Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
            }
            
            // Apply velocity with collision detection
            Vector3 newPosition = Position + Velocity * scaledDeltaTime;
            
            // Check collision with obstacles
            if (obstacles != null && Velocity.LengthSquared() > 0)
            {
                const float ENEMY_RADIUS = 0.75f;
                bool collisionDetected = false;
                
                foreach (var obstacle in obstacles)
                {
                    if (obstacle.IsDestroyed) continue;
                    
                    // Check if new position would collide with obstacle
                    if (obstacle.CheckCollision(new Vector3(newPosition.X, Position.Y, newPosition.Z), ENEMY_RADIUS))
                    {
                        collisionDetected = true;
                        
                        // Try sliding along the obstacle
                        // First try moving only on X axis
                        Vector3 xOnly = new Vector3(Position.X + Velocity.X * scaledDeltaTime, Position.Y, Position.Z);
                        if (!obstacle.CheckCollision(xOnly, ENEMY_RADIUS))
                        {
                            Position = xOnly;
                        }
                        // Try moving only on Z axis
                        else
                        {
                            Vector3 zOnly = new Vector3(Position.X, Position.Y, Position.Z + Velocity.Z * scaledDeltaTime);
                            if (!obstacle.CheckCollision(zOnly, ENEMY_RADIUS))
                            {
                                Position = zOnly;
                            }
                            else
                            {
                                // Can't move at all, generate new patrol target if patrolling
                                if (currentState == EnemyState.Patrolling)
                                {
                                    GenerateNewPatrolTarget();
                                }
                            }
                        }
                        break;
                    }
                }
                
                if (!collisionDetected)
                {
                    Position = newPosition;
                }
            }
            else
            {
                Position += Velocity * scaledDeltaTime;
            }
        }
    }
    
    protected virtual void HandleIdleState(float distanceToPlayer)
    {
        Velocity = Vector3.Zero; // Stand still
        
        // Player detected - start chasing
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
        }
        // Been idle too long - start patrolling
        else if (stateTimer > IDLE_TIMEOUT)
        {
            ChangeState(EnemyState.Patrolling);
        }
    }
    
    protected virtual void HandlePatrollingState(float deltaTime, float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }
        
        // Move towards patrol target
        Vector3 toTarget = patrolTarget - Position;
        toTarget.Y = 0; // Keep on ground
        float distance = toTarget.Length();
        
        if (distance > PATROL_REACH_DISTANCE)
        {
            // Cache normalized direction to avoid redundant calculation
            Vector3 direction = toTarget / distance; // More efficient than Normalize
            Velocity = direction * moveSpeed;
            
            // Rotate to face movement direction
            yRotation = MathF.Atan2(direction.X, direction.Z);
        }
        else
        {
            // Reached patrol point, generate new one
            GenerateNewPatrolTarget();
            ChangeState(EnemyState.Idle);
        }
    }
    
    protected virtual void HandleChasingState(float deltaTime, float distanceToPlayer)
    {
        // Lost the player - go back to patrolling
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        // Check for charge opportunity (closer than attack range, charge not on cooldown)
        float currentTime = (float)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        if (distanceToPlayer <= CHARGE_RANGE && currentTime - lastChargeTime >= CHARGE_COOLDOWN)
        {
            InitiateCharge();
            return;
        }
        
        // Close enough to attack normally
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(EnemyState.Attacking);
            return;
        }
        
        // Chase the player
        Vector3 toPlayer = targetPosition - Position;
        toPlayer.Y = 0; // Keep on ground
        
        float lengthSq = toPlayer.LengthSquared();
        if (lengthSq > 0)
        {
            float length = MathF.Sqrt(lengthSq);
            Vector3 direction = toPlayer / length; // More efficient than Normalize
            Velocity = direction * chaseSpeed;
            
            // Rotate to face player
            yRotation = MathF.Atan2(direction.X, direction.Z);
        }
    }
    
    protected virtual void HandleAttackingState(float deltaTime, float distanceToPlayer)
    {
        Velocity = Vector3.Zero; // Stop to attack
        
        if (distanceToPlayer > attackRange)
        {
            ChangeState(EnemyState.Chasing);
            return;
        }
        
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        // Face the player
        Vector3 toPlayer = targetPosition - Position;
        toPlayer.Y = 0;
        if (toPlayer.LengthSquared() > 0)
        {
            yRotation = MathF.Atan2(toPlayer.X, toPlayer.Z);
        }
        
        // Attack logic will be handled by Game.cs checking CanAttack()
    }
    
    public virtual bool CanAttack(float currentTime)
    {
        // Must be in attack state
        if (currentState != EnemyState.Attacking) return false;
        // Check if cooldown has expired
        if (currentTime - lastAttackTime < attackCooldown) return false;
        
        // Don't reset cooldown here - only reset when attack actually fires
        return true;
    }
    
    public void ConsumeAttackCooldown(float currentTime)
    {
        lastAttackTime = currentTime; // Reset cooldown when attack is actually fired
    }
    
    public Vector3 GetAttackDirection()
    {
        Vector3 toPlayer = targetPosition - Position;
        return Vector3.Normalize(toPlayer);
    }
    
    public float GetProjectileSpeed() => projectileSpeed;
    public float GetDamage() => damage;
    
    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        
        Health = Math.Max(0, Health - amount);
        hitFlashTimer = HIT_FLASH_DURATION; // Trigger white flash
        hitstopTimer = HITSTOP_DURATION; // Trigger hitstop for this enemy only
        
        // Getting hit alerts enemy to player location
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrolling)
        {
            ChangeState(EnemyState.Chasing);
        }
        
        if (Health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        currentState = EnemyState.Dead;
        IsActive = false;
        Velocity = Vector3.Zero;
        // Enemy destroyed
    }
    
    protected void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }
    
    // Per-instance random to avoid lock contention
    private readonly Random random = new Random(Guid.NewGuid().GetHashCode());
    
    private void GenerateNewPatrolTarget()
    {
        // Generate random patrol point within range
        float angle = (float)(random.NextDouble() * Math.PI * 2);
        float distance = MIN_PATROL_DISTANCE + (float)(random.NextDouble() * MAX_PATROL_DISTANCE);
        
        patrolTarget = Position + new Vector3(
            MathF.Sin(angle) * distance,
            0,
            MathF.Cos(angle) * distance
        );
    }
    
    protected virtual void HandleChargingState(float deltaTime, float distanceToPlayer, Vector3 playerPosition)
    {
        // Check if charge duration is over
        if (stateTimer >= CHARGE_DURATION)
        {
            // Return to chasing after charge
            ChangeState(EnemyState.Chasing);
            hasDealtChargeDamage = false;
            return;
        }
        
        // Move at charge speed in the locked direction
        Velocity = chargeDirection * CHARGE_SPEED;
        
        // Check if we're close enough to deal damage
        if (!hasDealtChargeDamage && distanceToPlayer <= CHARGE_HIT_RANGE)
        {
            // This flag will be checked by Game.cs to deal damage
            hasDealtChargeDamage = true;
        }
        
        // Visual feedback - turn orange during charge
        Color = new Vector3(1.0f, 0.6f, 0.2f);
    }
    
    private void InitiateCharge()
    {
        ChangeState(EnemyState.Charging);
        
        // Lock in the charge direction at the start
        Vector3 toPlayer = targetPosition - Position;
        toPlayer.Y = 0; // Keep on ground
        
        if (toPlayer.LengthSquared() > 0)
        {
            chargeDirection = Vector3.Normalize(toPlayer);
            yRotation = MathF.Atan2(chargeDirection.X, chargeDirection.Z);
        }
        else
        {
            chargeDirection = new Vector3(MathF.Sin(yRotation), 0, MathF.Cos(yRotation));
        }
        
        lastChargeTime = (float)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        hasDealtChargeDamage = false;
        
        // Initiating charge
    }
    
    public bool IsCharging => currentState == EnemyState.Charging;
    public bool HasChargeDamageReady => IsCharging && hasDealtChargeDamage && !IsInHitstop;
    public virtual float GetChargeDamage() => CHARGE_DAMAGE;
    
    public void ConsumeChargeDamage()
    {
        hasDealtChargeDamage = false;
    }
    
    public float GetYRotation() => yRotation;
}