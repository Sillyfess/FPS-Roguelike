using System.Numerics;

namespace FPSRoguelike.Environment;

public enum ObstacleType
{
    Crate,      // Small cover, can jump over
    Wall,       // Tall wall segment
    Pillar,     // Cylindrical pillar
    Barrier,    // Medium height barrier
    Platform    // Raised platform
}

public class Obstacle
{
    public Vector3 Position { get; set; }
    public Vector3 Size { get; set; }
    public ObstacleType Type { get; set; }
    public Vector3 Color { get; set; }
    public float Rotation { get; set; }
    public bool IsDestructible { get; set; }
    public float Health { get; private set; }
    
    // Collision bounds
    public Vector3 MinBounds => Position - Size / 2f;
    public Vector3 MaxBounds => Position + Size / 2f;
    
    public Obstacle(Vector3 position, ObstacleType type, float rotation = 0f)
    {
        Position = position;
        Type = type;
        Rotation = rotation;
        
        // Set properties based on type
        switch (type)
        {
            case ObstacleType.Crate:
                Size = new Vector3(2f, 2f, 2f);
                Color = new Vector3(0.6f, 0.4f, 0.2f); // Brown
                IsDestructible = true;
                Health = 50f;
                break;
                
            case ObstacleType.Wall:
                Size = new Vector3(8f, 6f, 1f);
                Color = new Vector3(0.5f, 0.5f, 0.5f); // Gray
                IsDestructible = false;
                Health = -1f;
                break;
                
            case ObstacleType.Pillar:
                Size = new Vector3(2f, 8f, 2f);
                Color = new Vector3(0.4f, 0.4f, 0.45f); // Dark gray
                IsDestructible = false;
                Health = -1f;
                break;
                
            case ObstacleType.Barrier:
                Size = new Vector3(6f, 3f, 1f);
                Color = new Vector3(0.3f, 0.3f, 0.4f); // Blue-gray
                IsDestructible = true;
                Health = 100f;
                break;
                
            case ObstacleType.Platform:
                Size = new Vector3(10f, 0.5f, 10f);
                Color = new Vector3(0.35f, 0.35f, 0.35f); // Dark gray
                IsDestructible = false;
                Health = -1f;
                break;
        }
    }
    
    public bool CheckCollision(Vector3 point, float radius = 0.5f)
    {
        // Proper sphere-AABB collision detection
        // Find the closest point on the AABB to the sphere center
        Vector3 closestPoint = Vector3.Clamp(point, MinBounds, MaxBounds);
        
        // Calculate the distance between the sphere center and closest point
        float distanceSquared = Vector3.DistanceSquared(point, closestPoint);
        
        // Check if the distance is less than the radius
        return distanceSquared <= (radius * radius);
    }
    
    public void TakeDamage(float damage)
    {
        if (!IsDestructible || Health <= 0) return;
        
        Health -= damage;
        if (Health <= 0)
        {
            // Obstacle destroyed - could spawn particles or drops here
            Health = 0;
        }
    }
    
    public bool IsDestroyed => IsDestructible && Health <= 0;
}