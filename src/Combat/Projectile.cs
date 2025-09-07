using System.Numerics;

namespace FPSRoguelike.Combat;

// Projectile for both player and enemy attacks
// Uses object pooling for performance
public class Projectile
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Lifetime { get; private set; }
    public bool IsActive { get; set; }  // False = available in pool
    public bool IsEnemyProjectile { get; set; }  // Determines collision target
    
    private const float MAX_LIFETIME = 5f;  // Auto-deactivate after 5 seconds
    private const float PROJECTILE_RADIUS = 0.2f;  // Collision sphere radius
    
    public Projectile()
    {
        IsActive = false;
    }
    
    // Initialize projectile from pool and launch it
    public void Fire(Vector3 origin, Vector3 direction, float speed, float damage, bool fromEnemy = false)
    {
        // Validate direction vector
        if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z))
        {
            throw new ArgumentException("Direction vector contains NaN values", nameof(direction));
        }
        
        // Normalize direction if not already (with proper epsilon for floating point comparison)
        float lengthSquared = direction.LengthSquared();
        const float NORMALIZATION_EPSILON = 0.0001f; // Much tighter tolerance for unit vectors
        if (Math.Abs(lengthSquared - 1.0f) > NORMALIZATION_EPSILON && lengthSquared > 0)
        {
            direction = Vector3.Normalize(direction);
        }
        
        // Validate speed and damage
        if (speed <= 0 || float.IsNaN(speed) || float.IsInfinity(speed))
        {
            throw new ArgumentException("Speed must be positive and finite", nameof(speed));
        }
        
        if (damage < 0 || float.IsNaN(damage) || float.IsInfinity(damage))
        {
            throw new ArgumentException("Damage must be non-negative and finite", nameof(damage));
        }
        
        Position = origin;
        Velocity = direction * speed;
        Damage = damage;
        Lifetime = 0f;
        IsActive = true;  // Mark as in-use
        IsEnemyProjectile = fromEnemy;
    }
    
    public void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        // Move projectile forward
        Position += Velocity * deltaTime;
        Lifetime += deltaTime;
        
        // Return to pool if expired or hit ground
        const float GROUND_LEVEL = 0f;
        if (Lifetime >= MAX_LIFETIME || Position.Y <= GROUND_LEVEL)
        {
            IsActive = false;
        }
    }
    
    // Sphere-sphere collision test
    public bool CheckCollision(Vector3 targetPosition, float targetRadius)
    {
        if (!IsActive) return false;
        
        float distance = Vector3.Distance(Position, targetPosition);
        return distance <= (PROJECTILE_RADIUS + targetRadius);  // Combined radii
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}