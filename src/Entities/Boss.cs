using System.Numerics;
using FPSRoguelike.Environment;

namespace FPSRoguelike.Entities;

public class Boss : Enemy
{
    // Boss-specific constants
    private const float BOSS_MOVE_SPEED = 4f;
    private const float BOSS_CHASE_SPEED = 8f;
    private const float BOSS_CHARGE_SPEED = 20f;
    private const float BOSS_CHARGE_RANGE = 15f;
    private const float BOSS_CHARGE_DAMAGE = 50f;
    private const float BOSS_CHARGE_COOLDOWN = 2f;
    private const float BOSS_CHARGE_DURATION = 0.8f;
    private const float BOSS_MELEE_RANGE = 3f;
    private const float BOSS_MELEE_DAMAGE = 30f;
    private const float BOSS_MELEE_COOLDOWN = 1f;
    
    // Boss is larger
    public float Size { get; private set; } = 3f;
    
    // Melee attack tracking
    private float lastMeleeTime = 0f;
    private bool hasMeleeReady = false;
    
    public Boss(Vector3 startPosition, float health = 500f) : base(startPosition, health)
    {
        // Override base enemy parameters with boss-specific values
        moveSpeed = BOSS_MOVE_SPEED;
        chaseSpeed = BOSS_CHASE_SPEED;
        
        // Boss doesn't use ranged attacks - set attack range to 0 to disable
        attackRange = 0f;
        
        // Boss has better detection
        detectionRange = 30f;
        loseTargetRange = 40f;
        
        // Boss color - darker red/purple
        Color = new Vector3(0.6f, 0.1f, 0.3f);
        
        // Boss immediately starts chasing
        ChangeState(EnemyState.Chasing);
    }
    
    protected override void HandleChasingState(float deltaTime, float distanceToPlayer)
    {
        // Lost the player - go back to patrolling
        if (distanceToPlayer > loseTargetRange)
        {
            ChangeState(EnemyState.Patrolling);
            return;
        }
        
        // Check for charge opportunity
        float currentTime = (float)DateTime.Now.Subtract(DateTime.UnixEpoch).TotalSeconds;
        if (distanceToPlayer <= BOSS_CHARGE_RANGE && distanceToPlayer > BOSS_MELEE_RANGE && 
            currentTime - lastChargeTime >= BOSS_CHARGE_COOLDOWN)
        {
            InitiateBossCharge();
            return;
        }
        
        // Check for melee attack if very close
        if (distanceToPlayer <= BOSS_MELEE_RANGE && currentTime - lastMeleeTime >= BOSS_MELEE_COOLDOWN)
        {
            // Set flag for melee damage
            hasMeleeReady = true;
            lastMeleeTime = currentTime;
        }
        
        // Chase the player
        Vector3 toPlayer = targetPosition - Position;
        toPlayer.Y = 0; // Keep on ground
        
        float lengthSq = toPlayer.LengthSquared();
        if (lengthSq > 0)
        {
            float length = MathF.Sqrt(lengthSq);
            Vector3 direction = toPlayer / length;
            Velocity = direction * chaseSpeed;
            
            // Rotate to face player
            yRotation = MathF.Atan2(direction.X, direction.Z);
        }
    }
    
    protected override void HandleChargingState(float deltaTime, float distanceToPlayer, Vector3 playerPosition)
    {
        // Check if charge duration is over
        if (stateTimer >= BOSS_CHARGE_DURATION)
        {
            // Return to chasing after charge
            ChangeState(EnemyState.Chasing);
            hasDealtChargeDamage = false;
            return;
        }
        
        // Move at boss charge speed in the locked direction
        Velocity = chargeDirection * BOSS_CHARGE_SPEED;
        
        // Check if we're close enough to deal damage (larger hit range for boss)
        if (!hasDealtChargeDamage && distanceToPlayer <= BOSS_MELEE_RANGE)
        {
            hasDealtChargeDamage = true;
        }
        
        // Visual feedback - turn bright red during charge
        Color = new Vector3(1.0f, 0.2f, 0.2f);
    }
    
    private void InitiateBossCharge()
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
        
        Console.WriteLine($"[BOSS] CHARGING!");
    }
    
    // Boss never uses ranged attacks
    public override bool CanAttack(float currentTime)
    {
        return false; // Boss doesn't shoot projectiles
    }
    
    public bool HasMeleeDamageReady => hasMeleeReady && !IsInHitstop;
    public float GetMeleeDamage() => BOSS_MELEE_DAMAGE;
    
    public void ConsumeMeleeDamage()
    {
        hasMeleeReady = false;
    }
    
    // Override charge damage for boss
    public override float GetChargeDamage() => BOSS_CHARGE_DAMAGE;
    
    // Boss takes damage but shows different visual feedback
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        
        // Boss flashes yellow when hit
        if (IsAlive)
        {
            Color = hitFlashTimer > 0 ? new Vector3(1.0f, 1.0f, 0.3f) : new Vector3(0.6f, 0.1f, 0.3f);
        }
    }
}