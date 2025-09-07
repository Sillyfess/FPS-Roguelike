using System.Numerics;

namespace FPSRoguelike.Entities;

public enum EnemyState
{
    Idle,
    Patrolling,
    Chasing,
    Attacking,
    Dead
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
    
    // Movement parameters
    private float moveSpeed = 3f;
    private float chaseSpeed = 5f;
    private float rotationSpeed = 3f;
    
    // Combat parameters
    private float attackRange = 15f;
    private float attackCooldown = 2f;
    private float projectileSpeed = 20f;
    private float damage = 10f;
    
    // AI State
    private EnemyState currentState = EnemyState.Idle;
    private float stateTimer = 0f;
    private float lastAttackTime = 0f;
    private Vector3 targetPosition;
    private Vector3 patrolTarget;
    private float yRotation = 0f;
    
    // Detection
    private float detectionRange = 20f;
    private float loseTargetRange = 30f;
    private bool hasTarget = false;
    
    // Visual
    public Vector3 Color { get; private set; } = new Vector3(1.0f, 0.2f, 0.2f); // Red color
    private float hitFlashTimer = 0f;
    private const float HIT_FLASH_DURATION = 0.1f;
    
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
        
        // State machine logic
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
        if (Position.Y > 1f)
        {
            Velocity = new Vector3(Velocity.X, Velocity.Y - 20f * deltaTime, Velocity.Z);
        }
        else if (Position.Y < 1f)
        {
            Position = new Vector3(Position.X, 1f, Position.Z);
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }
        
        // Apply velocity
        Position += Velocity * deltaTime;
    }
    
    private void HandleIdleState(float distanceToPlayer)
    {
        Velocity = Vector3.Zero;
        
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
            hasTarget = true;
        }
        else if (stateTimer > 2f)
        {
            ChangeState(EnemyState.Patrolling);
        }
    }
    
    private void HandlePatrollingState(float deltaTime, float distanceToPlayer)
    {
        if (distanceToPlayer <= detectionRange)
        {
            ChangeState(EnemyState.Chasing);
            hasTarget = true;
            return;
        }
        
        // Move towards patrol target
        Vector3 toTarget = patrolTarget - Position;
        toTarget.Y = 0; // Keep on ground
        float distance = toTarget.Length();
        
        if (distance > 0.5f)
        {
            Vector3 direction = Vector3.Normalize(toTarget);
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
        if (distanceToPlayer > loseTargetRange)
        {
            hasTarget = false;
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        if (distanceToPlayer <= attackRange)
        {
            ChangeState(EnemyState.Attacking);
            return;
        }
        
        // Chase the player
        Vector3 toPlayer = targetPosition - Position;
        toPlayer.Y = 0; // Keep on ground
        
        if (toPlayer.LengthSquared() > 0)
        {
            Vector3 direction = Vector3.Normalize(toPlayer);
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
            hasTarget = false;
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
        if (currentState != EnemyState.Attacking) return false;
        if (currentTime - lastAttackTime < attackCooldown) return false;
        
        lastAttackTime = currentTime;
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
        hitFlashTimer = HIT_FLASH_DURATION;
        
        // Alert to player if not already chasing
        if (currentState == EnemyState.Idle || currentState == EnemyState.Patrolling)
        {
            ChangeState(EnemyState.Chasing);
            hasTarget = true;
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
        Console.WriteLine($"Enemy {Id} destroyed!");
    }
    
    private void ChangeState(EnemyState newState)
    {
        currentState = newState;
        stateTimer = 0f;
    }
    
    private void GenerateNewPatrolTarget()
    {
        // Generate random patrol point within range
        Random rand = new Random();
        float angle = (float)(rand.NextDouble() * Math.PI * 2);
        float distance = 5f + (float)(rand.NextDouble() * 10f);
        
        patrolTarget = Position + new Vector3(
            MathF.Sin(angle) * distance,
            0,
            MathF.Cos(angle) * distance
        );
    }
    
    public float GetYRotation() => yRotation;
}