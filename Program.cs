using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;
using FPSRoguelike.Core;

namespace FPSRoguelike;

class Program
{
    private static IWindow? window;
    private static GL? gl;
    private static GameRefactored? game;
    private static bool initialized = false;
    private static string? initializationError = null;
    
    static void Main(string[] args)
    {
        // Load settings to get user preferences
        var settings = Settings.Load();
        
        // Window configuration
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "FPS Roguelike Prototype",
            PreferredStencilBufferBits = 0,
            PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8),
            PreferredDepthBufferBits = 24,
            VSync = settings.VSync  // Apply user's VSync preference
        };
        
        window = Window.Create(options);
        
        window.Load += OnLoad;
        window.Update += OnUpdate;
        window.Render += OnRender;
        window.Closing += OnClose;
        
        window.Run();
    }
    
    private static void OnLoad()
    {
        try
        {
            if (window == null)
            {
                throw new InvalidOperationException("Window is null during OnLoad");
            }
            
            gl = GL.GetApi(window);
            if (gl == null)
            {
                throw new InvalidOperationException("Failed to initialize OpenGL context");
            }
            
            game = new GameRefactored(window, gl);
            game.Initialize();
            initialized = true;
        }
        catch (Exception ex)
        {
            initializationError = $"Failed to initialize game: {ex.Message}";
            Console.WriteLine($"[FATAL ERROR] {initializationError}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            
            // Try to show error dialog if possible
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c echo {initializationError} && pause",
                    UseShellExecute = true
                });
            }
            catch { }
            
            // Close the window to exit cleanly
            window?.Close();
        }
    }
    
    private static void OnUpdate(double deltaTime)
    {
        if (!initialized)
        {
            // Skip update if initialization failed
            return;
        }
        
        if (game == null)
        {
            Console.WriteLine("[ERROR] Game is null during update");
            return;
        }
        
        try
        {
            game.Update(deltaTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Update failed: {ex.Message}");
            // Don't crash on update errors, try to continue
        }
    }
    
    private static void OnRender(double deltaTime)
    {
        if (!initialized)
        {
            // Show error message in window if initialization failed
            if (gl != null && !string.IsNullOrEmpty(initializationError))
            {
                gl.ClearColor(0.5f, 0f, 0f, 1f); // Red background for error
                gl.Clear(ClearBufferMask.ColorBufferBit);
            }
            return;
        }
        
        if (game == null)
        {
            Console.WriteLine("[ERROR] Game is null during render");
            return;
        }
        
        try
        {
            game.Render(deltaTime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Render failed: {ex.Message}");
            // Clear to a safe color on render error
            gl?.ClearColor(0.1f, 0.1f, 0.1f, 1f);
            gl?.Clear(ClearBufferMask.ColorBufferBit);
        }
    }
    
    private static void OnClose()
    {
        try
        {
            game?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Disposal failed: {ex.Message}");
            // Continue closing even if disposal fails
        }
        finally
        {
            gl = null;
            game = null;
            window = null;
        }
    }
}