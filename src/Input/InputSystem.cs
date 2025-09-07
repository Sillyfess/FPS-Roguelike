using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace FPSRoguelike.Input;

public class InputSystem
{
    private IInputContext? inputContext;
    private IKeyboard? keyboard;
    private IMouse? mouse;
    
    private HashSet<Key> currentKeys = new();
    private HashSet<Key> previousKeys = new();
    private HashSet<MouseButton> currentMouseButtons = new();
    private HashSet<MouseButton> previousMouseButtons = new();
    
    private Vector2 mouseDelta;
    private Vector2 accumulatedMouseDelta; // Accumulate delta between polls
    private Vector2 lastMousePosition;
    private bool firstMouseMove = true;
    
    public void Initialize(IWindow window)
    {
        inputContext = window.CreateInput();
        
        if (inputContext.Keyboards.Count > 0)
        {
            keyboard = inputContext.Keyboards[0];
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }
        
        if (inputContext.Mice.Count > 0)
        {
            mouse = inputContext.Mice[0];
            mouse.MouseMove += OnMouseMove;
            mouse.MouseDown += OnMouseDown;
            mouse.MouseUp += OnMouseUp;
            mouse.Scroll += OnMouseScroll;
        }
    }
    
    public void Poll()
    {
        // Swap key states
        previousKeys = new HashSet<Key>(currentKeys);
        previousMouseButtons = new HashSet<MouseButton>(currentMouseButtons);
        
        // Transfer accumulated mouse delta to current frame
        mouseDelta = accumulatedMouseDelta;
        accumulatedMouseDelta = Vector2.Zero;
    }
    
    // Keyboard input
    public bool IsKeyPressed(Key key) => currentKeys.Contains(key);
    public bool IsKeyJustPressed(Key key) => currentKeys.Contains(key) && !previousKeys.Contains(key);
    public bool IsKeyJustReleased(Key key) => !currentKeys.Contains(key) && previousKeys.Contains(key);
    
    // Mouse input
    public bool IsMouseButtonPressed(MouseButton button) => currentMouseButtons.Contains(button);
    public bool IsMouseButtonJustPressed(MouseButton button) => 
        currentMouseButtons.Contains(button) && !previousMouseButtons.Contains(button);
    public bool IsMouseButtonJustReleased(MouseButton button) => 
        !currentMouseButtons.Contains(button) && previousMouseButtons.Contains(button);
    
    public Vector2 GetMouseDelta() => mouseDelta;
    public Vector2 GetMousePosition() => lastMousePosition;
    
    // Input axis mapping
    public float GetAxis(string axisName)
    {
        return axisName switch
        {
            "Horizontal" => (IsKeyPressed(Key.D) ? 1f : 0f) - (IsKeyPressed(Key.A) ? 1f : 0f),
            "Vertical" => (IsKeyPressed(Key.W) ? 1f : 0f) - (IsKeyPressed(Key.S) ? 1f : 0f),
            "MouseX" => mouseDelta.X,
            "MouseY" => mouseDelta.Y,
            _ => 0f
        };
    }
    
    public Vector2 GetMovementInput()
    {
        return new Vector2(GetAxis("Horizontal"), GetAxis("Vertical"));
    }
    
    // Event handlers
    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        currentKeys.Add(key);
    }
    
    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        currentKeys.Remove(key);
    }
    
    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        if (firstMouseMove)
        {
            lastMousePosition = position;
            firstMouseMove = false;
            return;
        }
        
        // Accumulate mouse delta until next poll
        accumulatedMouseDelta += position - lastMousePosition;
        lastMousePosition = position;
    }
    
    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        currentMouseButtons.Add(button);
    }
    
    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        currentMouseButtons.Remove(button);
    }
    
    private float scrollDelta = 0f;
    public float GetScrollDelta() => scrollDelta;
    
    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        scrollDelta = wheel.Y;
    }
    
    public void Cleanup()
    {
        if (keyboard != null)
        {
            keyboard.KeyDown -= OnKeyDown;
            keyboard.KeyUp -= OnKeyUp;
        }
        
        if (mouse != null)
        {
            mouse.MouseMove -= OnMouseMove;
            mouse.MouseDown -= OnMouseDown;
            mouse.MouseUp -= OnMouseUp;
            mouse.Scroll -= OnMouseScroll;
        }
        
        inputContext?.Dispose();
    }
}