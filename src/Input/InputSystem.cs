using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;

namespace FPSRoguelike.Input;

// Handles all input from keyboard and mouse with raw input for FPS controls
public class InputSystem : IDisposable
{
    private IInputContext? inputContext;
    private IKeyboard? keyboard;
    private IMouse? mouse;
    
    // Thread safety
    private readonly object lockObject = new object();
    
    // Track current and previous states for "just pressed" detection
    private HashSet<Key> currentKeys = new();
    private HashSet<Key> previousKeys = new();
    private HashSet<MouseButton> currentMouseButtons = new();
    private HashSet<MouseButton> previousMouseButtons = new();
    
    private bool disposed = false;
    
    private Vector2 mouseDelta;
    private Vector2 accumulatedMouseDelta;  // Accumulate between fixed timesteps
    private Vector2 lastMousePosition;
    private bool firstMouseMove = true;  // Ignore first frame to prevent jump
    
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
        lock (lockObject)
        {
            // Swap key states for edge detection - reuse collections to avoid allocations
            previousKeys.Clear();
            previousKeys.UnionWith(currentKeys);
            
            previousMouseButtons.Clear();
            previousMouseButtons.UnionWith(currentMouseButtons);
            
            // Transfer accumulated mouse movement to this frame
            mouseDelta = accumulatedMouseDelta;
            accumulatedMouseDelta = Vector2.Zero;  // Reset for next accumulation
            
            // Reset scroll delta after reading (it doesn't accumulate like mouse movement)
            scrollDelta = 0f;
        }
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
    
    public Vector2 GetMouseDelta() 
    {
        lock (lockObject)
        {
            // Return the mouse delta for this frame
            return mouseDelta;
        }
    }
    
    public void ClearMouseDelta()
    {
        lock (lockObject)
        {
            accumulatedMouseDelta = Vector2.Zero;
        }
    }
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
    
    public IInputContext GetInputContext()
    {
        return inputContext ?? throw new InvalidOperationException("InputSystem not initialized");
    }
    
    public void SetCursorMode(CursorMode mode)
    {
        if (mouse != null)
        {
            mouse.Cursor.CursorMode = mode;
        }
    }
    
    // Event handlers
    private void OnKeyDown(IKeyboard keyboard, Key key, int scancode)
    {
        try
        {
            lock (lockObject)
            {
                currentKeys.Add(key);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnKeyDown: {ex.Message}");
        }
    }
    
    private void OnKeyUp(IKeyboard keyboard, Key key, int scancode)
    {
        try
        {
            lock (lockObject)
            {
                currentKeys.Remove(key);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnKeyUp: {ex.Message}");
        }
    }
    
    private void OnMouseMove(IMouse mouse, Vector2 position)
    {
        try
        {
            lock (lockObject)
            {
                // Skip first frame to avoid jump from cursor lock
                if (firstMouseMove)
                {
                    lastMousePosition = position;
                    firstMouseMove = false;
                    return;
                }
                
                // Accumulate raw mouse movement between polls
                var delta = position - lastMousePosition;
                
                // Clamp to prevent overflow with high DPI mice
                const float MAX_DELTA = 1000f;
                delta.X = Math.Clamp(delta.X, -MAX_DELTA, MAX_DELTA);
                delta.Y = Math.Clamp(delta.Y, -MAX_DELTA, MAX_DELTA);
                
                accumulatedMouseDelta += delta;
                lastMousePosition = position;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnMouseMove: {ex.Message}");
        }
    }
    
    private void OnMouseDown(IMouse mouse, MouseButton button)
    {
        try
        {
            lock (lockObject)
            {
                currentMouseButtons.Add(button);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnMouseDown: {ex.Message}");
        }
    }
    
    private void OnMouseUp(IMouse mouse, MouseButton button)
    {
        try
        {
            lock (lockObject)
            {
                currentMouseButtons.Remove(button);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnMouseUp: {ex.Message}");
        }
    }
    
    private float scrollDelta = 0f;
    public float GetScrollDelta() => scrollDelta;
    
    private void OnMouseScroll(IMouse mouse, ScrollWheel wheel)
    {
        lock (lockObject)
        {
            scrollDelta = wheel.Y;
        }
    }
    
    public void Cleanup()
    {
        Dispose();
    }
    
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    
    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                if (keyboard != null)
                {
                    keyboard.KeyDown -= OnKeyDown;
                    keyboard.KeyUp -= OnKeyUp;
                    keyboard = null;
                }
                
                if (mouse != null)
                {
                    mouse.MouseMove -= OnMouseMove;
                    mouse.MouseDown -= OnMouseDown;
                    mouse.MouseUp -= OnMouseUp;
                    mouse.Scroll -= OnMouseScroll;
                    mouse = null;
                }
                
                inputContext?.Dispose();
                inputContext = null;
            }
            
            disposed = true;
        }
    }
}