using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Combat;
using FPSRoguelike.Rendering;
using FPSRoguelike.Core;
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
    
    public WeaponSystem(IEntityManager entityManager, ICollisionSystem collisionSystem, GL? gl = null)
    {
        this.entityManager = entityManager;
        this.collisionSystem = collisionSystem;
        
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
    
    public bool TryFire(Vector3 position, Vector3 aimDirection)
    {
        var weapon = CurrentWeapon;
        if (weapon == null || !weapon.CanFire())
        {
            return false;
        }
        
        if (weapon is Katana)
        {
            // Melee attack using arc collision
            weapon.UpdateFireTiming();
            
            // Find enemies in the swing arc
            var enemiesInArc = entityManager.GetEnemiesInArc(
                position, 
                aimDirection, 
                Constants.KATANA_RANGE, 
                Constants.KATANA_ARC_ANGLE
            );
            
            foreach (var enemy in enemiesInArc)
            {
                enemy.TakeDamage(weapon.Damage);
            }
            
            // Trigger slash effect
            slashEffect?.Trigger();
            
            return true;
        }
        else if (weapon is Revolver || weapon is SMG)
        {
            // Ranged attack - use provided aim direction
            weapon.UpdateFireTiming();
            
            // Fire projectile
            entityManager.FireProjectile(
                position,
                aimDirection,
                Constants.DEFAULT_PROJECTILE_SPEED,
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
    public void RenderEffects(Vector3 playerPosition, Matrix4x4 viewMatrix, Matrix4x4 projMatrix)
    {
        if (slashEffect != null)
        {
            // Create slash arc points
            Vector3[] slashPoints = new Vector3[] 
            { 
                playerPosition + Vector3.UnitX,
                playerPosition + Vector3.UnitZ 
            };
            float progress = 1.0f; // Full slash
            slashEffect.Render(slashPoints, progress, viewMatrix, projMatrix);
        }
    }
}