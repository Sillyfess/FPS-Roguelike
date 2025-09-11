using System.Numerics;
using System.Linq;
using ImGuiNET;
using FPSRoguelike.Entities;
using FPSRoguelike.Combat;
using FPSRoguelike.Physics;
using FPSRoguelike.Core;

namespace FPSRoguelike.UI;

/// <summary>
/// Modern HUD implementation using Dear ImGui
/// </summary>
public class ImGuiHUD
{
    // HUD configuration
    private const float HEALTH_BAR_HEIGHT = 30f;
    private const float AMMO_DISPLAY_SIZE = 80f;
    private const float KILL_FEED_WIDTH = 300f;
    private const float KILL_FEED_HEIGHT = 150f;
    private const float DAMAGE_NUMBER_DURATION = 1.5f;
    private const float WAVE_ANNOUNCEMENT_DURATION = 3f;
    private const float HIT_MARKER_DURATION = 0.2f;
    private const float DAMAGE_INDICATOR_DURATION = 2f;
    
    // HUD state
    private List<KillFeedEntry> killFeed = new List<KillFeedEntry>();
    private List<DamageNumber> damageNumbers = new List<DamageNumber>();
    private List<DamageIndicator> damageIndicators = new List<DamageIndicator>();
    private float waveAnnouncementTimer = 0f;
    private string waveAnnouncementText = "";
    private float hitMarkerTimer = 0f;
    private bool showDebugInfo = false;
    private float currentFPS = 0f;
    private float fpsUpdateTimer = 0f;
    
    // Weapon wheel state
    private bool showWeaponWheel = false;
    
    // Minimap data
    private List<MinimapEntity> minimapEntities = new List<MinimapEntity>();
    
    // Settings reference
    private Settings? currentSettings = null;
    
    // Kill feed entry
    private class KillFeedEntry
    {
        public string Message { get; set; } = "";
        public float TimeRemaining { get; set; }
        public Vector4 Color { get; set; }
    }
    
    // Floating damage numbers
    private class DamageNumber
    {
        public Vector2 Position { get; set; }
        public float Damage { get; set; }
        public float TimeRemaining { get; set; }
        public Vector4 Color { get; set; }
        public bool IsCritical { get; set; }
    }
    
    // Damage direction indicator
    private class DamageIndicator
    {
        public float Angle { get; set; } // Direction damage came from
        public float TimeRemaining { get; set; }
        public float Intensity { get; set; } // How strong the damage was
        public float Damage { get; set; } // Damage amount
    }
    
    // Minimap entity
    private class MinimapEntity
    {
        public Vector2 Position { get; set; }
        public EntityType Type { get; set; }
        public float Rotation { get; set; }
    }
    
    private enum EntityType
    {
        Player,
        Enemy,
        Boss,
        Objective
    }
    
    public void Update(float deltaTime)
    {
        // Update kill feed
        for (int i = killFeed.Count - 1; i >= 0; i--)
        {
            killFeed[i].TimeRemaining -= deltaTime;
            if (killFeed[i].TimeRemaining <= 0)
                killFeed.RemoveAt(i);
        }
        
        // Update damage numbers
        for (int i = damageNumbers.Count - 1; i >= 0; i--)
        {
            damageNumbers[i].TimeRemaining -= deltaTime;
            damageNumbers[i].Position += new Vector2(0, -50f * deltaTime); // Float upward
            if (damageNumbers[i].TimeRemaining <= 0)
                damageNumbers.RemoveAt(i);
        }
        
        // Update damage indicators
        for (int i = damageIndicators.Count - 1; i >= 0; i--)
        {
            damageIndicators[i].TimeRemaining -= deltaTime;
            
            if (damageIndicators[i].TimeRemaining <= 0)
                damageIndicators.RemoveAt(i);
        }
        
        // Update wave announcement
        if (waveAnnouncementTimer > 0)
            waveAnnouncementTimer -= deltaTime;
            
        // Update hit marker
        if (hitMarkerTimer > 0)
            hitMarkerTimer -= deltaTime;
            
        // Update FPS counter
        fpsUpdateTimer += deltaTime;
        if (fpsUpdateTimer >= 0.5f)
        {
            currentFPS = 1f / deltaTime;
            fpsUpdateTimer = 0f;
        }
    }
    
    public void Render(PlayerHealth? playerHealth, Weapon? weapon, int score, int waveNumber, 
                      int enemiesRemaining, bool isPaused, float deltaTime, float playerRotationY = 0f, bool showSettings = false)
    {
        var io = ImGui.GetIO();
        var displaySize = io.DisplaySize;
        
        // Render settings menu if needed
        if (showSettings)
        {
            RenderSettingsMenu(displaySize);
            return; // Don't render HUD when settings are open
        }
        
        if (isPaused) return;
        
        // Render health bar
        RenderHealthBar(playerHealth, displaySize);
        
        // Render ammo display
        RenderAmmoDisplay(weapon, displaySize);
        
        // Render score and wave info
        RenderScoreAndWave(score, waveNumber, enemiesRemaining, displaySize);
        
        // Render minimap
        RenderMinimap(displaySize);
        
        // Render weapon wheel
        RenderWeaponWheel(displaySize);
        
        // Render damage indicators
        RenderDamageIndicators(displaySize, playerRotationY);
        
        // Render crosshair
        RenderCrosshair(displaySize);
        
        // Render kill feed
        RenderKillFeed(displaySize);
        
        // Render damage numbers
        RenderDamageNumbers();
        
        // Render wave announcement
        if (waveAnnouncementTimer > 0)
            RenderWaveAnnouncement(displaySize);
            
        // Render hit marker
        if (hitMarkerTimer > 0)
            RenderHitMarker(displaySize);
            
        // Render debug info
        if (showDebugInfo)
            RenderDebugInfo(displaySize);
    }
    
    private void RenderHealthBar(PlayerHealth? playerHealth, Vector2 displaySize)
    {
        if (playerHealth == null) return;
        
        float healthPercent = playerHealth.HealthPercentage;
        Vector4 healthColor = healthPercent > 0.5f ? new Vector4(0.2f, 0.8f, 0.2f, 1f) :
                             healthPercent > 0.25f ? new Vector4(0.8f, 0.8f, 0.2f, 1f) :
                             new Vector4(0.8f, 0.2f, 0.2f, 1f);
        
        // Position at bottom left
        ImGui.SetNextWindowPos(new Vector2(20, displaySize.Y - 100));
        ImGui.SetNextWindowSize(new Vector2(300, 80));
        ImGui.SetNextWindowBgAlpha(0.3f);
        
        ImGui.Begin("Health", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                              ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        // Health text
        ImGui.Text($"HEALTH");
        
        // Health bar background
        var drawList = ImGui.GetWindowDrawList();
        var pos = ImGui.GetCursorScreenPos();
        drawList.AddRectFilled(pos, pos + new Vector2(280, HEALTH_BAR_HEIGHT), 
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 0.8f)));
        
        // Health bar fill
        drawList.AddRectFilled(pos, pos + new Vector2(280 * healthPercent, HEALTH_BAR_HEIGHT), 
            ImGui.ColorConvertFloat4ToU32(healthColor));
        
        // Health bar border
        drawList.AddRect(pos, pos + new Vector2(280, HEALTH_BAR_HEIGHT), 
            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, 0.5f)), 0, 0, 2f);
        
        // Health value
        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + HEALTH_BAR_HEIGHT + 5);
        ImGui.Text($"{(int)playerHealth.Health} / {(int)playerHealth.MaxHealth}");
        
        ImGui.End();
    }
    
    private void RenderAmmoDisplay(Weapon? weapon, Vector2 displaySize)
    {
        if (weapon == null) return;
        
        // Position at bottom right
        ImGui.SetNextWindowPos(new Vector2(displaySize.X - 200, displaySize.Y - 100));
        ImGui.SetNextWindowSize(new Vector2(180, 80));
        ImGui.SetNextWindowBgAlpha(0.3f);
        
        ImGui.Begin("Ammo", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                            ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        // Weapon name
        ImGui.Text(weapon.Name.ToUpper());
        
        // Check if weapon has ammo (Revolver/SMG)
        if (weapon is Revolver revolver)
        {
            // Ammo count with large font
            var fonts = ImGui.GetIO().Fonts;
            if (fonts.Fonts.Size > 0)
            {
                ImGui.PushFont(fonts.Fonts[0]); // Use default for now
                ImGui.Text($"{revolver.CurrentAmmo} / {revolver.MaxAmmo}");
                ImGui.PopFont();
            }
            else
            {
                ImGui.Text($"{revolver.CurrentAmmo} / {revolver.MaxAmmo}");
            }
            
            // Reload indicator
            if (revolver.IsReloading)
            {
                ImGui.ProgressBar(revolver.ReloadProgress, new Vector2(160, 10), "RELOADING");
            }
        }
        else if (weapon is SMG smg)
        {
            ImGui.Text($"{smg.CurrentAmmo} / {smg.MaxAmmo}");
            
            if (smg.IsReloading)
            {
                ImGui.ProgressBar(smg.ReloadProgress, new Vector2(160, 10), "RELOADING");
            }
        }
        else
        {
            // Melee weapon - show ready status
            ImGui.Text("MELEE WEAPON");
            if (weapon.CanFire())
            {
                ImGui.TextColored(new Vector4(0.2f, 0.8f, 0.2f, 1f), "READY");
            }
            else
            {
                ImGui.TextColored(new Vector4(0.8f, 0.2f, 0.2f, 1f), "COOLDOWN");
            }
        }
        
        ImGui.End();
    }
    
    private void RenderScoreAndWave(int score, int waveNumber, int enemiesRemaining, Vector2 displaySize)
    {
        // Top center info
        ImGui.SetNextWindowPos(new Vector2(displaySize.X / 2 - 150, 20));
        ImGui.SetNextWindowSize(new Vector2(300, 60));
        ImGui.SetNextWindowBgAlpha(0.3f);
        
        ImGui.Begin("Score", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                             ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        // Center align text
        string scoreText = $"SCORE: {score}";
        float textWidth = ImGui.CalcTextSize(scoreText).X;
        ImGui.SetCursorPosX((300 - textWidth) / 2);
        ImGui.Text(scoreText);
        
        string enemyText = enemiesRemaining > 0 ? $"ENEMIES: {enemiesRemaining}" : "BOSS FIGHT!";
        textWidth = ImGui.CalcTextSize(enemyText).X;
        ImGui.SetCursorPosX((300 - textWidth) / 2);
        
        if (enemiesRemaining == 0)
            ImGui.TextColored(new Vector4(1f, 0.2f, 0.2f, 1f), enemyText);
        else
            ImGui.Text(enemyText);
        
        ImGui.End();
    }
    
    private void RenderCrosshair(Vector2 displaySize)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var center = displaySize / 2;
        uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 1f, 0.2f, 0.8f));
        float thickness = 2f;
        float size = 15f;
        float gap = 5f;
        
        // Horizontal line
        drawList.AddLine(center - new Vector2(size + gap, 0), center - new Vector2(gap, 0), color, thickness);
        drawList.AddLine(center + new Vector2(gap, 0), center + new Vector2(size + gap, 0), color, thickness);
        
        // Vertical line
        drawList.AddLine(center - new Vector2(0, size + gap), center - new Vector2(0, gap), color, thickness);
        drawList.AddLine(center + new Vector2(0, gap), center + new Vector2(0, size + gap), color, thickness);
        
        // Center dot
        drawList.AddCircleFilled(center, 2f, ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.2f, 0.2f, 1f)));
    }
    
    private void RenderKillFeed(Vector2 displaySize)
    {
        if (killFeed.Count == 0) return;
        
        ImGui.SetNextWindowPos(new Vector2(displaySize.X - KILL_FEED_WIDTH - 20, 100));
        ImGui.SetNextWindowSize(new Vector2(KILL_FEED_WIDTH, KILL_FEED_HEIGHT));
        ImGui.SetNextWindowBgAlpha(0.2f);
        
        ImGui.Begin("KillFeed", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                                ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        foreach (var entry in killFeed)
        {
            float alpha = Math.Min(1f, entry.TimeRemaining / 0.5f); // Fade out
            var color = entry.Color;
            color.W = alpha;
            ImGui.TextColored(color, entry.Message);
        }
        
        ImGui.End();
    }
    
    private void RenderDamageNumbers()
    {
        var drawList = ImGui.GetForegroundDrawList();
        
        foreach (var dmg in damageNumbers)
        {
            float alpha = dmg.TimeRemaining / DAMAGE_NUMBER_DURATION;
            var color = dmg.Color;
            color.W = alpha;
            
            string text = dmg.IsCritical ? $"{(int)dmg.Damage}!" : $"{(int)dmg.Damage}";
            float scale = dmg.IsCritical ? 1.5f : 1f;
            
            // Draw text with outline for visibility
            var pos = dmg.Position;
            drawList.AddText(pos + new Vector2(1, 1), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, alpha)), text);
            drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(color), text);
        }
    }
    
    private void RenderWaveAnnouncement(Vector2 displaySize)
    {
        float alpha = Math.Min(1f, waveAnnouncementTimer / 0.5f); // Fade in/out
        var color = new Vector4(1f, 0.8f, 0.2f, alpha);
        
        var drawList = ImGui.GetForegroundDrawList();
        var textSize = ImGui.CalcTextSize(waveAnnouncementText);
        var pos = new Vector2(displaySize.X / 2 - textSize.X / 2, displaySize.Y / 3);
        
        // Draw with shadow
        drawList.AddText(pos + new Vector2(2, 2), ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, alpha * 0.8f)), 
            waveAnnouncementText);
        drawList.AddText(pos, ImGui.ColorConvertFloat4ToU32(color), waveAnnouncementText);
    }
    
    private void RenderHitMarker(Vector2 displaySize)
    {
        var drawList = ImGui.GetForegroundDrawList();
        var center = displaySize / 2;
        float alpha = hitMarkerTimer / HIT_MARKER_DURATION;
        uint color = ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 1f, 1f, alpha));
        float size = 20f;
        float thickness = 3f;
        
        // X shape
        drawList.AddLine(center - new Vector2(size, size), center + new Vector2(size, size), color, thickness);
        drawList.AddLine(center - new Vector2(size, -size), center + new Vector2(size, -size), color, thickness);
    }
    
    private void RenderDebugInfo(Vector2 displaySize)
    {
        ImGui.SetNextWindowPos(new Vector2(10, 10));
        ImGui.SetNextWindowSize(new Vector2(200, 100));
        ImGui.SetNextWindowBgAlpha(0.5f);
        
        ImGui.Begin("Debug", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                             ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        ImGui.Text($"FPS: {currentFPS:F1}");
        ImGui.Text($"Frame Time: {1000f / currentFPS:F2}ms");
        ImGui.Text($"Damage Numbers: {damageNumbers.Count}");
        ImGui.Text($"Kill Feed: {killFeed.Count}");
        
        ImGui.End();
    }
    
    // Public methods to trigger HUD events
    public void AddKillFeedEntry(string message, Vector4 color)
    {
        killFeed.Insert(0, new KillFeedEntry 
        { 
            Message = message, 
            TimeRemaining = 5f, 
            Color = color 
        });
        
        // Keep only last 5 entries
        if (killFeed.Count > 5)
            killFeed.RemoveAt(killFeed.Count - 1);
    }
    
    public void AddDamageNumber(Vector2 screenPos, float damage, bool isCritical = false)
    {
        damageNumbers.Add(new DamageNumber
        {
            Position = screenPos,
            Damage = damage,
            TimeRemaining = DAMAGE_NUMBER_DURATION,
            Color = isCritical ? new Vector4(1f, 0.8f, 0.2f, 1f) : new Vector4(1f, 1f, 1f, 1f),
            IsCritical = isCritical
        });
    }
    
    public void ShowWaveAnnouncement(string text)
    {
        waveAnnouncementText = text;
        waveAnnouncementTimer = WAVE_ANNOUNCEMENT_DURATION;
    }
    
    public void ShowHitMarker()
    {
        hitMarkerTimer = HIT_MARKER_DURATION;
    }
    
    public void ToggleDebugInfo()
    {
        showDebugInfo = !showDebugInfo;
    }
    
    public bool IsDebugInfoVisible()
    {
        return showDebugInfo;
    }
    
    private bool shouldCloseSettings = false;
    
    public bool ShouldCloseSettings()
    {
        bool result = shouldCloseSettings;
        shouldCloseSettings = false;
        return result;
    }
    
    private void RenderSettingsMenu(Vector2 displaySize)
    {
        // Dark overlay
        var drawList = ImGui.GetBackgroundDrawList();
        drawList.AddRectFilled(Vector2.Zero, displaySize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.7f)));
        
        // Center the settings window
        ImGui.SetNextWindowPos(displaySize / 2, ImGuiCond.Always, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 300));
        
        ImGui.Begin("Settings", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | 
                                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings);
        
        ImGui.Text("Game Settings");
        ImGui.Separator();
        
        // Use actual settings values or defaults if settings not available
        float sensitivity = currentSettings?.MouseSensitivity ?? 0.3f;
        float fov = currentSettings?.FieldOfView ?? 90f;
        float masterVolume = currentSettings?.MasterVolume ?? 1.0f;
        
        // Mouse sensitivity slider
        if (ImGui.SliderFloat("Mouse Sensitivity", ref sensitivity, 0.1f, 1.0f))
        {
            if (currentSettings != null)
                currentSettings.MouseSensitivity = sensitivity;
        }
        
        // FOV slider
        if (ImGui.SliderFloat("Field of View", ref fov, 60f, 120f))
        {
            if (currentSettings != null)
                currentSettings.FieldOfView = fov;
        }
        
        // Volume controls
        if (ImGui.SliderFloat("Master Volume", ref masterVolume, 0f, 1f))
        {
            if (currentSettings != null)
                currentSettings.MasterVolume = masterVolume;
        }
        
        ImGui.Separator();
        
        // Controls info
        ImGui.Text("Controls:");
        ImGui.BulletText("WASD - Move");
        ImGui.BulletText("Mouse - Look");
        ImGui.BulletText("Space - Jump");
        ImGui.BulletText("LMB - Shoot");
        ImGui.BulletText("Q/Tab - Weapon Wheel");
        ImGui.BulletText("R - Reload/Respawn");
        ImGui.BulletText("F1 - Toggle Debug");
        ImGui.BulletText("F3 - Level Editor");
        
        ImGui.Separator();
        
        if (ImGui.Button("Resume Game"))
        {
            // Save settings when closing menu
            currentSettings?.Save();
            shouldCloseSettings = true;
        }
        
        ImGui.End();
    }
    
    // Minimap functionality
    public void UpdateMinimapEntities(List<Enemy> enemies, CharacterController player, float playerYaw = 0f)
    {
        minimapEntities.Clear();
        
        // Add player
        minimapEntities.Add(new MinimapEntity
        {
            Position = new Vector2(player.Position.X, player.Position.Z),
            Type = EntityType.Player,
            Rotation = playerYaw
        });
        
        // Add enemies
        foreach (var enemy in enemies.Where(e => e.IsAlive))
        {
            minimapEntities.Add(new MinimapEntity
            {
                Position = new Vector2(enemy.Position.X, enemy.Position.Z),
                Type = enemy.MaxHealth > 100 ? EntityType.Boss : EntityType.Enemy,
                Rotation = 0f
            });
        }
    }
    
    private void RenderMinimap(Vector2 displaySize)
    {
        const float MINIMAP_SIZE = 200f;
        const float MINIMAP_ZOOM = 0.05f; // Scale world units to minimap pixels
        
        // Position at top right
        ImGui.SetNextWindowPos(new Vector2(displaySize.X - MINIMAP_SIZE - 20, 20));
        ImGui.SetNextWindowSize(new Vector2(MINIMAP_SIZE, MINIMAP_SIZE));
        ImGui.SetNextWindowBgAlpha(0.3f);
        
        ImGui.Begin("Minimap", ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | 
                               ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar);
        
        var drawList = ImGui.GetWindowDrawList();
        var windowPos = ImGui.GetWindowPos();
        var center = windowPos + new Vector2(MINIMAP_SIZE / 2, MINIMAP_SIZE / 2);
        
        // Draw background
        drawList.AddRectFilled(windowPos, windowPos + new Vector2(MINIMAP_SIZE, MINIMAP_SIZE),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.8f)));
        
        // Draw border
        drawList.AddRect(windowPos, windowPos + new Vector2(MINIMAP_SIZE, MINIMAP_SIZE),
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)), 0f, 0, 2f);
        
        // Find player for centering
        var player = minimapEntities.FirstOrDefault(e => e.Type == EntityType.Player);
        if (player != null)
        {
            // Draw entities relative to player
            foreach (var entity in minimapEntities)
            {
                var relativePos = (entity.Position - player.Position) * MINIMAP_ZOOM;
                // Rotate based on player facing
                var rotatedPos = new Vector2(
                    relativePos.X * MathF.Cos(-player.Rotation) - relativePos.Y * MathF.Sin(-player.Rotation),
                    relativePos.X * MathF.Sin(-player.Rotation) + relativePos.Y * MathF.Cos(-player.Rotation)
                );
                var screenPos = center + rotatedPos;
                
                // Skip if outside minimap bounds
                if (Vector2.Distance(screenPos, center) > MINIMAP_SIZE / 2 - 10) continue;
                
                // Draw entity based on type
                switch (entity.Type)
                {
                    case EntityType.Player:
                        // Draw player as triangle pointing forward
                        var p1 = screenPos + new Vector2(0, -8);
                        var p2 = screenPos + new Vector2(-6, 6);
                        var p3 = screenPos + new Vector2(6, 6);
                        drawList.AddTriangleFilled(p1, p2, p3,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.8f, 0.2f, 1f)));
                        break;
                        
                    case EntityType.Enemy:
                        drawList.AddCircleFilled(screenPos, 4f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 1f)));
                        break;
                        
                    case EntityType.Boss:
                        drawList.AddCircleFilled(screenPos, 6f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.5f, 0f, 1f)));
                        drawList.AddCircle(screenPos, 8f,
                            ImGui.ColorConvertFloat4ToU32(new Vector4(1f, 0.8f, 0f, 1f)), 0, 2f);
                        break;
                }
            }
        }
        
        // Draw compass directions
        ImGui.SetCursorPos(new Vector2(MINIMAP_SIZE / 2 - 5, 5));
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "N");
        
        ImGui.End();
    }
    
    // Weapon wheel functionality
    public void ShowWeaponWheel(List<Weapon> availableWeapons)
    {
        showWeaponWheel = true;
        weaponWheelWeapons = availableWeapons;
    }
    
    public void HideWeaponWheel()
    {
        showWeaponWheel = false;
    }
    
    private List<Weapon> weaponWheelWeapons = new List<Weapon>();
    
    private void RenderWeaponWheel(Vector2 displaySize)
    {
        if (!showWeaponWheel || weaponWheelWeapons.Count == 0) return;
        
        // Darken background
        var drawList = ImGui.GetForegroundDrawList();
        drawList.AddRectFilled(Vector2.Zero, displaySize,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0, 0, 0, 0.5f)));
        
        var center = displaySize / 2;
        float radius = 150f;
        
        // Draw wheel background
        drawList.AddCircleFilled(center, radius + 20,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.1f, 0.1f, 0.1f, 0.9f)));
        drawList.AddCircle(center, radius + 20,
            ImGui.ColorConvertFloat4ToU32(new Vector4(0.3f, 0.3f, 0.3f, 1f)), 0, 3f);
        
        // Calculate angle for each weapon
        float angleStep = MathF.PI * 2f / weaponWheelWeapons.Count;
        
        for (int i = 0; i < weaponWheelWeapons.Count; i++)
        {
            float angle = -MathF.PI / 2 + angleStep * i; // Start from top
            var pos = center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            
            // Draw weapon slot
            drawList.AddCircleFilled(pos, 40f,
                ImGui.ColorConvertFloat4ToU32(new Vector4(0.2f, 0.2f, 0.2f, 1f)));
            
            // Highlight if mouse is over
            var mousePos = ImGui.GetIO().MousePos;
            bool isHovered = Vector2.Distance(mousePos, pos) < 40f;
            
            if (isHovered)
            {
                drawList.AddCircle(pos, 42f,
                    ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.2f, 0.2f, 1f)), 0, 3f);
            }
            
            // Draw weapon icon/name
            ImGui.SetCursorScreenPos(pos - new Vector2(30, 8));
            ImGui.Text(weaponWheelWeapons[i].Name);
        }
        
        // Instructions
        ImGui.SetCursorScreenPos(center - new Vector2(50, -radius - 50));
        ImGui.Text("Click to select");
    }
    
    // Method to set the current settings reference
    public void SetSettings(Settings settings)
    {
        currentSettings = settings;
    }
    
    // Damage indicators
    public void AddDamageIndicator(Vector3 damageSource, Vector3 playerPosition, float damage)
    {
        // Calculate direction from player to damage source
        var direction = damageSource - playerPosition;
        direction.Y = 0; // Ignore vertical component
        var angle = MathF.Atan2(direction.X, direction.Z);
        
        damageIndicators.Add(new DamageIndicator
        {
            Angle = angle,
            TimeRemaining = DAMAGE_INDICATOR_DURATION,
            Damage = damage
        });
    }
    
    private void RenderDamageIndicators(Vector2 displaySize, float playerRotationY)
    {
        var center = displaySize / 2;
        var drawList = ImGui.GetForegroundDrawList();
        
        foreach (var indicator in damageIndicators)
        {
            float alpha = indicator.TimeRemaining / DAMAGE_INDICATOR_DURATION;
            
            // Calculate screen angle relative to player view
            float relativeAngle = indicator.Angle - playerRotationY;
            
            // Position indicator at edge of screen
            float distance = Math.Min(displaySize.X, displaySize.Y) * 0.3f;
            var indicatorPos = center + new Vector2(
                MathF.Sin(relativeAngle) * distance,
                -MathF.Cos(relativeAngle) * distance
            );
            
            // Draw directional arrow
            var color = ImGui.ColorConvertFloat4ToU32(new Vector4(0.8f, 0.1f, 0.1f, alpha));
            
            // Calculate arrow points
            var forward = new Vector2(MathF.Sin(relativeAngle), -MathF.Cos(relativeAngle));
            var right = new Vector2(forward.Y, -forward.X);
            
            var p1 = indicatorPos + forward * 20f;
            var p2 = indicatorPos - forward * 10f - right * 15f;
            var p3 = indicatorPos - forward * 10f + right * 15f;
            
            drawList.AddTriangleFilled(p1, p2, p3, color);
            
            // Draw damage amount
            if (indicator.Damage > 0)
            {
                ImGui.SetCursorScreenPos(indicatorPos - new Vector2(15, 30));
                ImGui.TextColored(new Vector4(1f, 0.2f, 0.2f, alpha), $"-{(int)indicator.Damage}");
            }
        }
    }
}