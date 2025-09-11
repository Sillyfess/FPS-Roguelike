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
    
    static void Main(string[] args)
    {
        // Window configuration
        var options = WindowOptions.Default with
        {
            Size = new Vector2D<int>(1280, 720),
            Title = "FPS Roguelike Prototype",
            PreferredStencilBufferBits = 0,
            PreferredBitDepth = new Vector4D<int>(8, 8, 8, 8),
            PreferredDepthBufferBits = 24,
            VSync = false  // We control timing
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
        if (window == null) return;
        
        gl = GL.GetApi(window);
        game = new GameRefactored(window, gl);
        game.Initialize();
    }
    
    private static void OnUpdate(double deltaTime)
    {
        game?.Update(deltaTime);
    }
    
    private static void OnRender(double deltaTime)
    {
        game?.Render(deltaTime);
    }
    
    private static void OnClose()
    {
        game?.Dispose();
    }
}