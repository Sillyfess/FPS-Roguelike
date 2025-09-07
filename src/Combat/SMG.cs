using System.Numerics;

namespace FPSRoguelike.Combat;

public class SMG : Weapon
{
    private const float SMG_DAMAGE = 15f;  // Lower damage per shot
    private const float SMG_FIRE_RATE = 0.1f;  // Very fast fire rate (10 shots/second)
    private const float SMG_RANGE = 100f;  // Medium-long range
    private const float SMG_PROJECTILE_SPEED = 60f;  // Fast projectiles
    private const int SMG_CAPACITY = 30;  // 30 round magazine
    private const float SMG_RELOAD_TIME = 1.5f;  // Quick reload
    private const float SMG_SPREAD = 0.05f;  // Slight bullet spread
    
    private int currentAmmo;
    private float reloadTimer = 0f;
    private bool isReloading = false;
    private Random random = new Random();
    
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => SMG_CAPACITY;
    public bool IsReloading => isReloading;
    public float ReloadProgress => isReloading ? (reloadTimer / SMG_RELOAD_TIME) : 0f;
    public float ProjectileSpeed => SMG_PROJECTILE_SPEED;
    
    public SMG()
    {
        Name = "SMG";
        Damage = SMG_DAMAGE;
        FireRate = SMG_FIRE_RATE;
        Range = SMG_RANGE;
        currentAmmo = SMG_CAPACITY;
    }
    
    public bool CanShoot()
    {
        return !isReloading && currentAmmo > 0 && CanFire();
    }
    
    public Vector3 GetSpreadDirection(Vector3 baseDirection)
    {
        // Add random spread to the base direction
        float spreadX = (float)(random.NextDouble() * 2 - 1) * SMG_SPREAD;
        float spreadY = (float)(random.NextDouble() * 2 - 1) * SMG_SPREAD;
        
        // Create perpendicular vectors for spread
        Vector3 right = Vector3.Normalize(Vector3.Cross(baseDirection, Vector3.UnitY));
        Vector3 up = Vector3.Normalize(Vector3.Cross(right, baseDirection));
        
        // Apply spread
        Vector3 spreadDirection = baseDirection + right * spreadX + up * spreadY;
        return Vector3.Normalize(spreadDirection);
    }
    
    public void Shoot()
    {
        if (!CanShoot()) return;
        
        // Mark as fired by calling base Fire method with dummy parameters
        Fire(Vector3.Zero, Vector3.UnitZ, _ => { });
        
        currentAmmo--;
        
        // Auto-reload when empty
        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }
    
    public void StartReload()
    {
        if (isReloading || currentAmmo == SMG_CAPACITY) return;
        
        isReloading = true;
        reloadTimer = 0f;
        Console.WriteLine("[SMG] Reloading...");
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        if (isReloading)
        {
            reloadTimer += deltaTime;
            
            if (reloadTimer >= SMG_RELOAD_TIME)
            {
                currentAmmo = SMG_CAPACITY;
                isReloading = false;
                reloadTimer = 0f;
                Console.WriteLine("[SMG] Reloaded!");
            }
        }
    }
}