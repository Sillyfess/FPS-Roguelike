using System.Numerics;
using FPSRoguelike.Systems.Interfaces;
using FPSRoguelike.Entities;
using FPSRoguelike.Physics;
using FPSRoguelike.Rendering;
using FPSRoguelike.Input;
using FPSRoguelike.UI;
using Silk.NET.Input;

namespace FPSRoguelike.Systems.Core;

/// <summary>
/// Manages player state, health, movement, and input
/// </summary>
public class PlayerSystem : IPlayerSystem
{
    // Dependencies
    private readonly InputSystem inputSystem;
    private readonly IEntityManager entityManager;
    private readonly IWeaponSystem? weaponSystem;
    
    // Components
    private PlayerHealth playerHealth;
    private CharacterController characterController;
    private Camera camera;
    
    // Constants
    private const float PLAYER_START_HEIGHT = 1.7f;
    private const float PLAYER_START_Z = -10f;
    private const float RESPAWN_COOLDOWN = 2f;
    private const int POINTS_PER_KILL = 100;
    private const int POINTS_PER_BOSS = 500;
    
    // State
    private Vector3 playerStartPosition = new Vector3(0, PLAYER_START_HEIGHT, PLAYER_START_Z);
    private int score = 0;
    private float respawnTimer = 0f;
    private bool canRespawn = true;
    
    // Properties
    public Vector3 Position => characterController?.Position ?? Vector3.Zero;
    public PlayerHealth? Health => playerHealth;
    public bool IsAlive => playerHealth?.IsAlive ?? false;
    public int Score => score;
    public Camera Camera => camera;
    
    public PlayerSystem(InputSystem inputSystem, IEntityManager entityManager, 
                       IWeaponSystem? weaponSystem)
    {
        this.inputSystem = inputSystem;
        this.entityManager = entityManager;
        this.weaponSystem = weaponSystem;
        
        // Initialize components
        characterController = new CharacterController(playerStartPosition);
        camera = new Camera(characterController.Position);
        playerHealth = new PlayerHealth(100f);
    }
    
    public void Initialize()
    {
        score = 0;
        respawnTimer = 0f;
        canRespawn = true;
        
        // Reset player position
        characterController = new CharacterController(playerStartPosition);
        camera = new Camera(characterController.Position);
        playerHealth = new PlayerHealth(100f);
    }
    
    public void Update(float deltaTime)
    {
        // Update player health
        playerHealth?.Update(deltaTime);
        
        // Handle respawn timer
        if (!IsAlive && !canRespawn)
        {
            respawnTimer += deltaTime;
            if (respawnTimer >= RESPAWN_COOLDOWN)
            {
                canRespawn = true;
            }
        }
        
        // Check for respawn input
        if (!IsAlive && canRespawn && inputSystem.IsKeyPressed(Key.R))
        {
            Respawn();
        }
    }
    
    public void ProcessInput(float deltaTime)
    {
        if (!IsAlive) return;
        
        // Handle movement input
        Vector3 moveInput = Vector3.Zero;
        
        if (inputSystem.IsKeyPressed(Key.W))
            moveInput += camera.GetForwardMovement();
        if (inputSystem.IsKeyPressed(Key.S))
            moveInput -= camera.GetForwardMovement();
        if (inputSystem.IsKeyPressed(Key.D))
            moveInput += camera.GetRightMovement();
        if (inputSystem.IsKeyPressed(Key.A))
            moveInput -= camera.GetRightMovement();
        
        // Normalize diagonal movement
        if (moveInput.LengthSquared() > 0)
            moveInput = Vector3.Normalize(moveInput);
        
        // Handle jump input
        bool jumpPressed = inputSystem.IsKeyPressed(Key.Space);
        
        // Update character controller with physics and collision
        characterController.Update(moveInput, jumpPressed, deltaTime, entityManager.Obstacles.ToList());
        
        // Update camera position to follow player
        camera.Position = characterController.Position;
        
        // Handle weapon switching
        if (inputSystem.IsKeyPressed(Key.Number1))
            weaponSystem.SelectWeapon(1); // Katana
        else if (inputSystem.IsKeyPressed(Key.Number2))
            weaponSystem.SelectWeapon(0); // Revolver
        else if (inputSystem.IsKeyPressed(Key.Number3))
            weaponSystem.SelectWeapon(2); // SMG
        
        // Handle reload
        if (inputSystem.IsKeyPressed(Key.R))
            weaponSystem.Reload();
        
        // Handle firing
        if (inputSystem.IsMouseButtonPressed(MouseButton.Left))
            weaponSystem.TryFire();
    }
    
    public void UpdateCameraRotation(Vector2 mouseDelta, float sensitivity)
    {
        // Apply mouse sensitivity
        mouseDelta *= sensitivity;
        camera.UpdateRotation(mouseDelta);
    }
    
    public void AddScore(int points)
    {
        score += points;
    }
    
    public void TakeDamage(float damage, Vector3 damageSource)
    {
        playerHealth?.TakeDamage(damage);
        
        // Calculate damage direction for UI indicators
        // TODO: Pass damage indicator to UI system
        {
            Vector3 toSource = damageSource - Position;
            toSource.Y = 0; // Project to horizontal plane
            toSource = Vector3.Normalize(toSource);
            
            // Calculate angle relative to player forward
            Vector3 forward = camera.GetForwardMovement();
            forward.Y = 0;
            forward = Vector3.Normalize(forward);
            
            float angle = MathF.Atan2(toSource.X, toSource.Z) - MathF.Atan2(forward.X, forward.Z);
            
            // TODO: uiSystem.AddDamageIndicator(angle);
        }
    }
    
    public void Respawn()
    {
        if (!canRespawn) return;
        
        // Reset position
        characterController = new CharacterController(playerStartPosition);
        camera.Position = characterController.Position;
        
        // Reset health
        playerHealth = new PlayerHealth(100f);
        
        // Reset respawn state
        canRespawn = false;
        respawnTimer = 0f;
        
        // Don't reset score on respawn
    }
    
    public void Reset()
    {
        Initialize();
    }
    
    public void Dispose()
    {
        // No resources to dispose
    }
    
    /// <summary>
    /// Called when an enemy is killed
    /// </summary>
    public void OnEnemyKilled(Enemy enemy)
    {
        if (enemy is Boss)
        {
            AddScore(POINTS_PER_BOSS);
        }
        else
        {
            AddScore(POINTS_PER_KILL);
        }
    }
}