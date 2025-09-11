using System.Numerics;

namespace FPSRoguelike.Combat;

public class Weapon
{
    // Weapon constants
    private const float DEFAULT_DAMAGE = 10f;
    private const float DEFAULT_FIRE_RATE = 0.2f;
    private const float DEFAULT_RANGE = 100f;
    
    public string Name { get; set; } = "Basic Pistol";
    public float Damage { get; set; } = DEFAULT_DAMAGE;
    public float FireRate { get; set; } = DEFAULT_FIRE_RATE; // Time between shots in seconds
    public float Range { get; set; } = DEFAULT_RANGE;
    
    private float lastFireTime = 0f;
    private float currentTime = 0f;
    
    public bool CanFire()
    {
        return currentTime - lastFireTime >= FireRate;
    }
    
    /// <summary>
    /// Updates the fire timing without actually firing.
    /// Used by weapons that handle their own projectile spawning.
    /// </summary>
    public void UpdateFireTiming()
    {
        if (CanFire())
        {
            lastFireTime = currentTime;
        }
    }
    
    public virtual void Update(float deltaTime)
    {
        currentTime += deltaTime;
    }
    
}