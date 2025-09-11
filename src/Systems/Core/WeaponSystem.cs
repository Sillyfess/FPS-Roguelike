using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Combat;
using FPSRoguelike.Rendering;
using Silk.NET.OpenGL;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Manages player weapons and weapon switching
/// </summary>
public class WeaponSystem : IWeaponSystem
{
    // Dependencies
    private readonly IEntityManager entityManager;
    private readonly ICollisionSystem collisionSystem;
    private IPlayerSystem? playerSystem;
    
    // Weapons
    private readonly List<Weapon> weapons = new();
    private Revolver? revolver;
    private Katana? katana;
    private SMG? smg;
    private SlashEffect? slashEffect;
    
    // State
    private int currentWeaponIndex = 0;
    
    // Properties
    public Weapon? CurrentWeapon => currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count 
        ? weapons[currentWeaponIndex] : null;
    public int CurrentWeaponIndex => currentWeaponIndex;
    public int WeaponCount => weapons.Count;
    
    public WeaponSystem(IEntityManager entityManager, ICollisionSystem collisionSystem, 
                       IPlayerSystem? playerSystem, GL? gl = null)
    {
        this.entityManager = entityManager;
        this.collisionSystem = collisionSystem;
        this.playerSystem = playerSystem;
        
        // Initialize weapons
        revolver = new Revolver();
        katana = new Katana();
        smg = new SMG();
        
        if (gl != null)
        {
            slashEffect = new SlashEffect(gl);
        }
        
        // Add weapons to list
        weapons.Add(revolver);
        weapons.Add(katana);
        weapons.Add(smg);
    }
    
    public void Initialize()
    {
        currentWeaponIndex = 0;
    }
    
    public void Update(float deltaTime)
    {
        // Update all weapons
        foreach (var weapon in weapons)
        {
            weapon?.Update(deltaTime);
        }
        
        // Update slash effect if active
        slashEffect?.Update(deltaTime);
    }
    
    public void SelectWeapon(int index)
    {
        if (index >= 0 && index < weapons.Count)
        {
            currentWeaponIndex = index;
        }
    }
    
    public bool TryFire()
    {
        var weapon = CurrentWeapon;
        if (weapon == null || !weapon.CanFire())
        {
            return false;
        }
        
        Vector3 playerPos = playerSystem.Position;
        
        if (weapon is Katana)
        {
            // Melee attack
            weapon.UpdateFireTiming();
            
            // Find enemies in melee range
            var nearbyEnemies = entityManager.GetEnemiesInRange(playerPos, 3f);
            
            foreach (var enemy in nearbyEnemies)
            {
                enemy.TakeDamage(weapon.Damage);
            }
            
            // Trigger slash effect
            slashEffect?.Trigger();
            
            return true;
        }
        else if (weapon is Revolver || weapon is SMG)
        {
            // Ranged attack
            // Get camera forward direction (this should come from camera system)
            // For now, we'll need to pass this in or get it from somewhere
            
            // This is a simplified implementation - in practice, you'd get the actual aim direction
            Vector3 forward = new Vector3(0, 0, 1); // Placeholder
            
            weapon.UpdateFireTiming();
            
            // Fire projectile
            entityManager.FireProjectile(
                playerPos,
                forward,
                50f, // Projectile speed
                weapon.Damage,
                null
            );
            
            return true;
        }
        
        return false;
    }
    
    public void Reload()
    {
        if (CurrentWeapon is Revolver rev)
        {
            rev.StartReload();
        }
        else if (CurrentWeapon is SMG smg)
        {
            smg.StartReload();
        }
    }
    
    public Weapon? GetWeapon(int index)
    {
        return index >= 0 && index < weapons.Count ? weapons[index] : null;
    }
    
    public void Reset()
    {
        currentWeaponIndex = 0;
        
        // Reset weapon states
        foreach (var weapon in weapons)
        {
            // Weapons don't have a reset method currently
            // Could add one if needed
        }
    }
    
    public void Dispose()
    {
        slashEffect?.Dispose();
    }
    
    /// <summary>
    /// Render weapon effects (like slash)
    /// </summary>
    public void RenderEffects(Matrix4x4 viewMatrix, Matrix4x4 projMatrix)
    {
        if (playerSystem != null && slashEffect != null)
        {
            // Create slash arc points
            Vector3[] slashPoints = new Vector3[] 
            { 
                playerSystem.Position + Vector3.UnitX,
                playerSystem.Position + Vector3.UnitZ 
            };
            float progress = 1.0f; // Full slash
            slashEffect.Render(slashPoints, progress, viewMatrix, projMatrix);
        }
    }
    
    /// <summary>
    /// Set the player system reference after construction
    /// </summary>
    public void SetPlayerSystem(IPlayerSystem player)
    {
        playerSystem = player;
    }
}