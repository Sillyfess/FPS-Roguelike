using System.Numerics;

namespace FPSRoguelike.Entities;

// Enemy AI states for the state machine
public enum EnemyState
{
    Idle,       // Standing still, checking for player
    Patrolling, // Moving to random points
    Chasing,    // Actively pursuing the player
    Attacking,  // In range and firing projectiles
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
    
    // Movement parameters
    private float moveSpeed = DEFAULT_MOVE_SPEED;
    private float chaseSpeed = DEFAULT_CHASE_SPEED;
    
    // Combat parameters  
    private float attackRange = DEFAULT_ATTACK_RANGE;
    private float attackCooldown = DEFAULT_ATTACK_COOLDOWN;
    private float projectileSpeed = DEFAULT_PROJECTILE_SPEED;
    private float damage = DEFAULT_DAMAGE;
    
    // AI State
    private EnemyState currentState = EnemyState.Idle;
    private float stateTimer = 0f;
    private float lastAttackTime = 0f;
    private Vector3 targetPosition;
    private Vector3 patrolTarget;
    private float yRotation = 0f;
    
    // Detection constants
    private const float DEFAULT_DETECTION_RANGE = 20f;
    private const float DEFAULT_LOSE_TARGET_RANGE = 30f;
    
    // Detection parameters
    private float detectionRange = DEFAULT_DETECTION_RANGE;
    private float loseTargetRange = DEFAULT_LOSE_TARGET_RANGE;
    
    // Visual
    public Vector3 Color { get; private set; } = new Vector3(1.0f, 0.2f, 0.2f); // Red color
    private float hitFlashTimer = 0f;
    private const float HIT_FLASH_DURATION = 0.1f;
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
        Id = nextId++;
        Position = startPosition;
        MaxHealth = health;
        Health = health;
        
        // Start with a random patrol target
        GenerateNewPatrolTarget();
    }
    
    public void Update(float deltaTime, Vector3 playerPosition)
    {
        // Dead enemies don't update
        if (!IsAlive)
        {
            currentState = EnemyState.Dead;
            return;
        }
        
        // Update timers
        stateTimer += deltaTime;
        if (hitFlashTimer > 0)
        {
            hitFlashTimer -= deltaTime;
            Color = hitFlashTimer > 0 ? new Vector3(1.0f, 1.0f, 1.0f) : new Vector3(1.0f, 0.2f, 0.2f);
        }
        
        // Check player distance
        float distanceToPlayer = Vector3.Distance(Position, playerPosition);
        targetPosition = playerPosition;
        
        // Execute behavior based on current AI state
        switch (currentState)
        {
            case EnemyState.Idle:
                HandleIdleState(distanceToPlayer);
                break;
                
            case EnemyState.Patrolling:
                HandlePatrollingState(deltaTime, distanceToPlayer);
                break;
                
            case EnemyState.Chasing:
                HandleChasingState(deltaTime, distanceToPlayer);
                break;
                
            case EnemyState.Attacking:
                HandleAttackingState(deltaTime, distanceToPlayer);
                break;
        }
        
        // Apply simple gravity if above ground
        if (Position.Y > GROUND_LEVEL + EPSILON)
        {
            Velocity = new Vector3(Velocity.X, Velocity.Y - GRAVITY_STRENGTH * deltaTime, Velocity.Z);
        }
        else if (Position.Y < GROUND_LEVEL)
        {
            Position = new Vector3(Position.X, GROUND_LEVEL, Position.Z);
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }
        
        // Apply velocity
        Position += Velocity * deltaTime;
    }
    
    private void HandleIdleState(float distanceToPlayer)
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
    
    private void HandlePatrollingState(float deltaTime, float distanceToPlayer)
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
    
    private void HandleChasingState(float deltaTime, float distanceToPlayer)
    {
        // Lost the player - go back to patrolling
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        // Close enough to attack
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
    
    private void HandleAttackingState(float deltaTime, float distanceToPlayer)
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
    
    public bool CanAttack(float currentTime)
    {
        // Must be in attack state
        if (currentState != EnemyState.Attacking) return false;
        // Check if cooldown has expired
        if (currentTime - lastAttackTime < attackCooldown) return false;
        
        lastAttackTime = currentTime; // Reset cooldown
        return true;
    }
    
    public Vector3 GetAttackDirection()
    {
        Vector3 toPlayer = targetPosition - Position;
        return Vector3.Normalize(toPlayer);
    }
    
    public float GetProjectileSpeed() => projectileSpeed;
    public float GetDamage() => damage;
    
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        
        Health = Math.Max(0, Health - amount);
        hitFlashTimer = HIT_FLASH_DURATION; // Trigger white flash
        
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
    
    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }
    
    // Thread-safe random instance
    private static readonly Random sharedRandom = new Random();
    
    private void GenerateNewPatrolTarget()
    {
        // Generate random patrol point within range
        float angle, distance;
        lock (sharedRandom)
        {
            angle = (float)(sharedRandom.NextDouble() * Math.PI * 2);
            distance = MIN_PATROL_DISTANCE + (float)(sharedRandom.NextDouble() * MAX_PATROL_DISTANCE);
        }
        
        patrolTarget = Position + new Vector3(
            MathF.Sin(angle) * distance,
            0,
            MathF.Cos(angle) * distance
        );
    }
    
    public float GetYRotation() => yRotation;
}