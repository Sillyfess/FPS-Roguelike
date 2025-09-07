using System.Numerics;
using FPSRoguelike.Entities;

namespace FPSRoguelike.Combat;

public class Katana : Weapon
{
    private const float KATANA_DAMAGE = 35f;
    private const float KATANA_FIRE_RATE = 0.5f;
    private const float KATANA_RANGE = 3.5f;
    private const float KATANA_ARC = 90f; // degrees
    private const float SLASH_DURATION = 0.2f;
    
    public bool IsSlashing { get; private set; }
    public float SlashProgress { get; private set; }
    public Vector3 SlashDirection { get; private set; }
    public Vector3 SlashOrigin { get; private set; }
    
    private float slashTimer = 0f;
    private float lastSlashTime = 0f;
    private float currentSlashTime = 0f;
    
    public Katana()
    {
        Name = "Katana";
        Damage = KATANA_DAMAGE;
        FireRate = KATANA_FIRE_RATE;
        Range = KATANA_RANGE;
    }
    
    public bool CanSlash()
    {
        return currentSlashTime - lastSlashTime >= FireRate;
    }
    
    public void Slash(Vector3 origin, Vector3 direction, List<Enemy> enemies, Action<Enemy> onHit)
    {
        if (!CanSlash()) return;
        
        // Update slash timing
        lastSlashTime = currentSlashTime;
        
        // Start slash animation
        IsSlashing = true;
        slashTimer = 0f;
        SlashDirection = Vector3.Normalize(direction);
        SlashOrigin = origin;
        
        // Check for enemies in arc
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.IsAlive) continue;
            
            Vector3 toEnemy = enemy.Position - origin;
            float distance = toEnemy.Length();
            
            // Check if within range
            if (distance > Range) continue;
            
            // Check if within arc
            toEnemy = Vector3.Normalize(toEnemy);
            float dot = Vector3.Dot(direction, toEnemy);
            float angle = MathF.Acos(Math.Clamp(dot, -1f, 1f)) * (180f / MathF.PI);
            
            if (angle <= KATANA_ARC / 2f)
            {
                // Hit the enemy
                onHit?.Invoke(enemy);
            }
        }
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        currentSlashTime += deltaTime;
        
        if (IsSlashing)
        {
            slashTimer += deltaTime;
            SlashProgress = Math.Min(slashTimer / SLASH_DURATION, 1f);
            
            if (slashTimer >= SLASH_DURATION)
            {
                IsSlashing = false;
                slashTimer = 0f;
                SlashProgress = 0f;
            }
        }
    }
    
    public Vector3[] GetSlashVisualizationPoints()
    {
        if (!IsSlashing) return Array.Empty<Vector3>();
        
        // Generate arc points for visualization
        var points = new List<Vector3>();
        int segments = 16;
        float currentArc = KATANA_ARC * SlashProgress;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments - 0.5f) * currentArc * (MathF.PI / 180f);
            
            // Create rotation matrix around Y axis
            float cos = MathF.Cos(angle);
            float sin = MathF.Sin(angle);
            
            Vector3 rotatedDir = new Vector3(
                SlashDirection.X * cos - SlashDirection.Z * sin,
                SlashDirection.Y,
                SlashDirection.X * sin + SlashDirection.Z * cos
            );
            
            points.Add(SlashOrigin + rotatedDir * Range);
        }
        
        return points.ToArray();
    }
}