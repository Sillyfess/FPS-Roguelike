using System.Numerics;
using ImGuiNET;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace FPSRoguelike.UI;

/// <summary>
/// Controller for ImGui integration with Silk.NET
/// </summary>
public class ImGuiWrapper : IDisposable
{
    private Silk.NET.OpenGL.Extensions.ImGui.ImGuiController? imGuiController;
    private GL gl;
    private IWindow window;
    private IInputContext inputContext;
    private bool disposed = false;
    
    // ImGui configuration
    private const float DEFAULT_FONT_SIZE = 16f;
    private const float LARGE_FONT_SIZE = 24f;
    private const float SMALL_FONT_SIZE = 12f;
    
    public ImGuiWrapper(GL openGL, IWindow gameWindow, IInputContext input)
    {
        gl = openGL;
        window = gameWindow;
        inputContext = input;
    }
    
    public void Initialize()
    {
        // Create the ImGui controller
        imGuiController = new Silk.NET.OpenGL.Extensions.ImGui.ImGuiController(gl, window, inputContext);
        
        // Configure ImGui
        ConfigureImGuiStyle();
        // Note: Fonts are managed by the ImGuiController, no need to rebuild manually
    }
    
    private void ConfigureImGuiStyle()
    {
        var style = ImGui.GetStyle();
        
        // Modern FPS game style - dark theme with accent colors
        style.WindowRounding = 5.0f;
        style.FrameRounding = 3.0f;
        style.ScrollbarRounding = 3.0f;
        style.GrabRounding = 3.0f;
        style.WindowBorderSize = 1.0f;
        style.FrameBorderSize = 1.0f;
        style.PopupBorderSize = 1.0f;
        style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
        
        // Color scheme - Dark with red accents (FPS style)
        var colors = style.Colors;
        
        // Window background
        colors[(int)ImGuiCol.WindowBg] = new Vector4(0.06f, 0.06f, 0.08f, 0.94f);
        colors[(int)ImGuiCol.ChildBg] = new Vector4(0.08f, 0.08f, 0.10f, 0.94f);
        colors[(int)ImGuiCol.PopupBg] = new Vector4(0.08f, 0.08f, 0.10f, 0.94f);
        
        // Border
        colors[(int)ImGuiCol.Border] = new Vector4(0.20f, 0.20f, 0.24f, 0.50f);
        colors[(int)ImGuiCol.BorderShadow] = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        
        // Frame backgrounds
        colors[(int)ImGuiCol.FrameBg] = new Vector4(0.10f, 0.10f, 0.12f, 0.54f);
        colors[(int)ImGuiCol.FrameBgHovered] = new Vector4(0.20f, 0.20f, 0.24f, 0.54f);
        colors[(int)ImGuiCol.FrameBgActive] = new Vector4(0.25f, 0.25f, 0.30f, 0.67f);
        
        // Title bar
        colors[(int)ImGuiCol.TitleBg] = new Vector4(0.04f, 0.04f, 0.06f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new Vector4(0.08f, 0.08f, 0.10f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new Vector4(0.02f, 0.02f, 0.03f, 0.51f);
        
        // Menu bar
        colors[(int)ImGuiCol.MenuBarBg] = new Vector4(0.08f, 0.08f, 0.10f, 1.00f);
        
        // Scrollbar
        colors[(int)ImGuiCol.ScrollbarBg] = new Vector4(0.02f, 0.02f, 0.03f, 0.53f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new Vector4(0.20f, 0.20f, 0.24f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new Vector4(0.25f, 0.25f, 0.30f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new Vector4(0.30f, 0.30f, 0.35f, 1.00f);
        
        // Check mark
        colors[(int)ImGuiCol.CheckMark] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f); // Red accent
        
        // Slider
        colors[(int)ImGuiCol.SliderGrab] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f); // Red accent
        colors[(int)ImGuiCol.SliderGrabActive] = new Vector4(0.90f, 0.30f, 0.30f, 1.00f);
        
        // Button
        colors[(int)ImGuiCol.Button] = new Vector4(0.15f, 0.15f, 0.18f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f); // Red accent on hover
        colors[(int)ImGuiCol.ButtonActive] = new Vector4(0.70f, 0.15f, 0.15f, 1.00f);
        
        // Header
        colors[(int)ImGuiCol.Header] = new Vector4(0.20f, 0.20f, 0.24f, 0.31f);
        colors[(int)ImGuiCol.HeaderHovered] = new Vector4(0.80f, 0.20f, 0.20f, 0.80f); // Red accent
        colors[(int)ImGuiCol.HeaderActive] = new Vector4(0.70f, 0.15f, 0.15f, 1.00f);
        
        // Separator
        colors[(int)ImGuiCol.Separator] = new Vector4(0.20f, 0.20f, 0.24f, 0.50f);
        colors[(int)ImGuiCol.SeparatorHovered] = new Vector4(0.40f, 0.40f, 0.44f, 0.78f);
        colors[(int)ImGuiCol.SeparatorActive] = new Vector4(0.50f, 0.50f, 0.54f, 1.00f);
        
        // Resize grip
        colors[(int)ImGuiCol.ResizeGrip] = new Vector4(0.20f, 0.20f, 0.24f, 0.25f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new Vector4(0.80f, 0.20f, 0.20f, 0.67f);
        colors[(int)ImGuiCol.ResizeGripActive] = new Vector4(0.70f, 0.15f, 0.15f, 0.95f);
        
        // Tab
        colors[(int)ImGuiCol.Tab] = new Vector4(0.15f, 0.15f, 0.18f, 0.86f);
        colors[(int)ImGuiCol.TabHovered] = new Vector4(0.80f, 0.20f, 0.20f, 0.80f);
        colors[(int)ImGuiCol.TabActive] = new Vector4(0.70f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.TabUnfocused] = new Vector4(0.10f, 0.10f, 0.12f, 0.97f);
        colors[(int)ImGuiCol.TabUnfocusedActive] = new Vector4(0.20f, 0.20f, 0.24f, 1.00f);
        
        // Plot
        colors[(int)ImGuiCol.PlotLines] = new Vector4(0.61f, 0.61f, 0.64f, 1.00f);
        colors[(int)ImGuiCol.PlotLinesHovered] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogram] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogramHovered] = new Vector4(0.90f, 0.30f, 0.30f, 1.00f);
        
        // Text
        colors[(int)ImGuiCol.Text] = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new Vector4(0.50f, 0.50f, 0.54f, 1.00f);
        colors[(int)ImGuiCol.TextSelectedBg] = new Vector4(0.80f, 0.20f, 0.20f, 0.35f);
        
        // Drag/Drop
        colors[(int)ImGuiCol.DragDropTarget] = new Vector4(0.80f, 0.20f, 0.20f, 0.90f);
        
        // Nav
        colors[(int)ImGuiCol.NavHighlight] = new Vector4(0.80f, 0.20f, 0.20f, 1.00f);
        colors[(int)ImGuiCol.NavWindowingHighlight] = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);
        
        // Modal
        colors[(int)ImGuiCol.ModalWindowDimBg] = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);
    }
    
    
    public void Update(float deltaTime)
    {
        if (!disposed && imGuiController != null)
        {
            imGuiController.Update(deltaTime);
        }
    }
    
    public void Render()
    {
        if (!disposed && imGuiController != null)
        {
            imGuiController.Render();
        }
    }
    
    public void Dispose()
    {
        if (!disposed)
        {
            imGuiController?.Dispose();
            disposed = true;
        }
    }
}