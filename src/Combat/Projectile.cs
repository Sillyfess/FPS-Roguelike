using System.Numerics;

namespace FPSRoguelike.Combat;

public class Projectile
{
    public Vector3 Position { get; set; }
    public Vector3 Velocity { get; set; }
    public float Damage { get; set; }
    public float Lifetime { get; private set; }
    public bool IsActive { get; set; }
    public bool IsEnemyProjectile { get; set; }
    
    private const float MAX_LIFETIME = 5f;
    private const float PROJECTILE_RADIUS = 0.2f;
    
    public Projectile()
    {
        IsActive = false;
    }
    
    public void Fire(Vector3 origin, Vector3 direction, float speed, float damage, bool fromEnemy = false)
    {
        Position = origin;
        Velocity = direction * speed;
        Damage = damage;
        Lifetime = 0f;
        IsActive = true;
        IsEnemyProjectile = fromEnemy;
    }
    
    public void Update(float deltaTime)
    {
        if (!IsActive) return;
        
        Position += Velocity * deltaTime;
        Lifetime += deltaTime;
        
        // Deactivate if exceeded lifetime or hit ground
        if (Lifetime >= MAX_LIFETIME || Position.Y <= 0f)
        {
            IsActive = false;
        }
    }
    
    public bool CheckCollision(Vector3 targetPosition, float targetRadius)
    {
        if (!IsActive) return false;
        
        float distance = Vector3.Distance(Position, targetPosition);
        return distance <= (PROJECTILE_RADIUS + targetRadius);
    }
    
    public void Deactivate()
    {
        IsActive = false;
    }
}