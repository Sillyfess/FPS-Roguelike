using System.Numerics;

namespace FPSRoguelike.Combat;

public class Weapon
{
    // Weapon constants
    private const float DEFAULT_DAMAGE = 10f;
    private const float DEFAULT_FIRE_RATE = 0.2f;
    private const float DEFAULT_RANGE = 100f;
    private const float CUBE_HIT_RADIUS = 1f;
    
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
    
    public void Fire(Vector3 origin, Vector3 direction, Action<RaycastHit> onHit, HashSet<int>? destroyedTargets = null)
    {
        // Validate parameters
        if (float.IsNaN(origin.X) || float.IsNaN(origin.Y) || float.IsNaN(origin.Z) ||
            float.IsInfinity(origin.X) || float.IsInfinity(origin.Y) || float.IsInfinity(origin.Z))
        {
            throw new ArgumentException("Origin contains invalid values", nameof(origin));
        }
        
        if (float.IsNaN(direction.X) || float.IsNaN(direction.Y) || float.IsNaN(direction.Z) ||
            float.IsInfinity(direction.X) || float.IsInfinity(direction.Y) || float.IsInfinity(direction.Z))
        {
            throw new ArgumentException("Direction contains invalid values", nameof(direction));
        }
        
        if (direction.LengthSquared() < 0.0001f)
        {
            throw new ArgumentException("Direction vector is too small", nameof(direction));
        }
        
        if (onHit == null)
        {
            throw new ArgumentNullException(nameof(onHit));
        }
        
        if (!CanFire()) return;
        
        lastFireTime = currentTime;
        
        // Perform raycast
        var hit = Raycast(origin, direction, Range, destroyedTargets);
        if (hit.Hit)
        {
            onHit?.Invoke(hit);
        }
        
        // Visual/audio feedback would go here
    }
    
    public void Update(float deltaTime)
    {
        currentTime += deltaTime;
    }
    
    // Cached test positions to avoid allocation every raycast
    private static readonly Vector3[] testPositions = new Vector3[]
    {
        new Vector3(0, 1, 0),      // Center floating cube
        new Vector3(5, 1, 5),      // Corner cubes
        new Vector3(-5, 1, 5),
        new Vector3(5, 1, -5),
        new Vector3(-5, 1, -5),
        new Vector3(10, 2, 0),     // Distant cubes
        new Vector3(-10, 2, 0),
        new Vector3(0, 2, 10),
        new Vector3(0, 2, -10),
    };
    
    private RaycastHit Raycast(Vector3 origin, Vector3 direction, float maxDistance, HashSet<int>? destroyedTargets = null)
    {
        // For now, just check against some test targets
        // In a real implementation, this would check against all entities
        
        for (int i = 0; i < testPositions.Length; i++)
        {
            // Skip destroyed targets
            if (destroyedTargets != null && destroyedTargets.Contains(i))
                continue;
                
            var targetPos = testPositions[i];
            
            // Simple sphere collision check
            float radius = CUBE_HIT_RADIUS; // Cube "radius" for hit detection
            
            // Ray-sphere intersection
            Vector3 toTarget = targetPos - origin;
            float projLength = Vector3.Dot(toTarget, direction);
            
            if (projLength < 0 || projLength > maxDistance) continue;
            
            Vector3 closestPoint = origin + direction * projLength;
            float distance = Vector3.Distance(closestPoint, targetPos);
            
            if (distance <= radius)
            {
                return new RaycastHit
                {
                    Hit = true,
                    Point = closestPoint,
                    Normal = Vector3.Normalize(closestPoint - targetPos),
                    Distance = projLength,
                    Target = targetPos
                };
            }
        }
        
        return new RaycastHit { Hit = false };
    }
}

public struct RaycastHit
{
    public bool Hit;
    public Vector3 Point;
    public Vector3 Normal;
    public float Distance;
    public Vector3 Target;
}