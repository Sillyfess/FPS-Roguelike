namespace FPSRoguelike.Entities;

/// <summary>
/// Manages player health, damage, and regeneration mechanics
/// </summary>
public class PlayerHealth
{
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsAlive => Health > 0;
    public float HealthPercentage => MaxHealth > 0 ? Health / MaxHealth : 0f;  // For UI display
    
    // Damage and healing
    private float damageFlashTimer = 0f;
    private const float DAMAGE_FLASH_DURATION = 0.2f;  // Screen flash duration
    public bool IsDamageFlashing => damageFlashTimer > 0;
    
    // Regeneration constants
    private const float DEFAULT_REGEN_DELAY = 5f;
    private const float DEFAULT_REGEN_RATE = 5f;
    private const float DEFAULT_MAX_HEALTH = 100f;
    
    // Regeneration system
    private float regenDelay = DEFAULT_REGEN_DELAY;
    private float regenRate = DEFAULT_REGEN_RATE;
    private float timeSinceLastDamage = 0f;
    
    public PlayerHealth(float maxHealth = DEFAULT_MAX_HEALTH)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
    }
    
    public void Update(float deltaTime)
    {
        // Update damage flash effect
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= deltaTime;
        }
        
        timeSinceLastDamage += deltaTime;
        
        // Start regenerating health after delay period
        if (IsAlive && Health < MaxHealth && timeSinceLastDamage >= regenDelay)
        {
            Health = Math.Min(MaxHealth, Health + regenRate * deltaTime);
        }
    }
    
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;  // Can't damage dead player
        
        Health = Math.Max(0, Health - amount);
        damageFlashTimer = DAMAGE_FLASH_DURATION;  // Trigger visual feedback
        timeSinceLastDamage = 0f;  // Reset regen timer
        
        // Player took damage
        
        if (!IsAlive)
        {
            // Player died - game over
        }
    }
    
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        
        Health = Math.Min(MaxHealth, Health + amount);
        // Player healed
    }
    
    public void Respawn()
    {
        Health = MaxHealth;
        damageFlashTimer = 0f;
        timeSinceLastDamage = 0f;
        // Player respawned
    }
}