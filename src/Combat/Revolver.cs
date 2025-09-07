using System.Numerics;

namespace FPSRoguelike.Combat;

public class Revolver : Weapon
{
    private const float REVOLVER_DAMAGE = 100f;  // Very high damage per shot
    private const float REVOLVER_FIRE_RATE = 0.8f;  // Slower than katana
    private const float REVOLVER_RANGE = 150f;  // Long range
    private const float REVOLVER_PROJECTILE_SPEED = 80f;  // Fast projectile
    private const int REVOLVER_CAPACITY = 6;  // 6 shots before reload
    private const float REVOLVER_RELOAD_TIME = 2.0f;  // 2 second reload
    
    private int currentAmmo;
    private float reloadTimer = 0f;
    private bool isReloading = false;
    
    public int CurrentAmmo => currentAmmo;
    public int MaxAmmo => REVOLVER_CAPACITY;
    public bool IsReloading => isReloading;
    public float ReloadProgress => isReloading ? (reloadTimer / REVOLVER_RELOAD_TIME) : 0f;
    public float ProjectileSpeed => REVOLVER_PROJECTILE_SPEED;
    
    public Revolver()
    {
        Name = "Revolver";
        Damage = REVOLVER_DAMAGE;
        FireRate = REVOLVER_FIRE_RATE;
        Range = REVOLVER_RANGE;
        currentAmmo = REVOLVER_CAPACITY;
    }
    
    public bool CanShoot()
    {
        return !isReloading && currentAmmo > 0 && CanFire();
    }
    
    public void Shoot()
    {
        if (!CanShoot()) return;
        
        // Mark as fired by calling base Fire method with dummy parameters
        // This updates the lastFireTime to prevent rapid firing
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
        if (isReloading || currentAmmo == REVOLVER_CAPACITY) return;
        
        isReloading = true;
        reloadTimer = 0f;
        Console.WriteLine("[Revolver] Reloading...");
    }
    
    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        if (isReloading)
        {
            reloadTimer += deltaTime;
            
            if (reloadTimer >= REVOLVER_RELOAD_TIME)
            {
                currentAmmo = REVOLVER_CAPACITY;
                isReloading = false;
                reloadTimer = 0f;
                Console.WriteLine("[Revolver] Reloaded!");
            }
        }
    }
}