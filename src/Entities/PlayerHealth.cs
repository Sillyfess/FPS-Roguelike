namespace FPSRoguelike.Entities;

public class PlayerHealth
{
    public float Health { get; private set; }
    public float MaxHealth { get; private set; }
    public bool IsAlive => Health > 0;
    public float HealthPercentage => Health / MaxHealth;
    
    // Damage and healing
    private float damageFlashTimer = 0f;
    private const float DAMAGE_FLASH_DURATION = 0.2f;
    public bool IsDamageFlashing => damageFlashTimer > 0;
    
    // Regeneration
    private float regenDelay = 5f; // Time before regen starts after taking damage
    private float regenRate = 5f; // Health per second
    private float timeSinceLastDamage = 0f;
    
    public PlayerHealth(float maxHealth = 100f)
    {
        MaxHealth = maxHealth;
        Health = maxHealth;
    }
    
    public void Update(float deltaTime)
    {
        // Update timers
        if (damageFlashTimer > 0)
        {
            damageFlashTimer -= deltaTime;
        }
        
        timeSinceLastDamage += deltaTime;
        
        // Health regeneration after delay
        if (IsAlive && Health < MaxHealth && timeSinceLastDamage >= regenDelay)
        {
            Health = Math.Min(MaxHealth, Health + regenRate * deltaTime);
        }
    }
    
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        
        Health = Math.Max(0, Health - amount);
        damageFlashTimer = DAMAGE_FLASH_DURATION;
        timeSinceLastDamage = 0f;
        
        Console.WriteLine($"Player took {amount} damage! Health: {Health}/{MaxHealth}");
        
        if (!IsAlive)
        {
            Console.WriteLine("GAME OVER - Player died!");
        }
    }
    
    public void Heal(float amount)
    {
        if (!IsAlive) return;
        
        Health = Math.Min(MaxHealth, Health + amount);
        Console.WriteLine($"Player healed {amount}! Health: {Health}/{MaxHealth}");
    }
    
    public void Respawn()
    {
        Health = MaxHealth;
        damageFlashTimer = 0f;
        timeSinceLastDamage = 0f;
        Console.WriteLine("Player respawned!");
    }
}