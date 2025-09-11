using Silk.NET.Input;
using Silk.NET.Windowing;
using System.Numerics;
using FPSRoguelike.Core;

namespace FPSRoguelike.Input;

// Handles all input from keyboard and mouse with raw input for FPS controls
public class InputSystem : IDisposable
{
    private IInputContext? inputContext;
    private IKeyboard? keyboard;
    private IMouse? mouse;
    
    // Thread safety
    private readonly object lockObject = new object();
    
    // Logging
    private readonly ILogger logger;
    
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
    
    // Input failure tracking
    private int inputErrorCount = 0;
    private const int MAX_INPUT_ERRORS = 10;
    
    public InputSystem(ILogger? logger = null)
    {
        this.logger = logger ?? new NullLogger();
    }
    
    public void Initialize(IWindow window)
    {
        inputContext = window.CreateInput();
        
        if (inputContext != null && inputContext.Keyboards != null && inputContext.Keyboards.Count > 0)
        {
            keyboard = inputContext.Keyboards[0];
            keyboard.KeyDown += OnKeyDown;
            keyboard.KeyUp += OnKeyUp;
        }
        
        if (inputContext != null && inputContext.Mice != null && inputContext.Mice.Count > 0)
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
            
            // Only transfer mouse delta if there's new data
            // This prevents losing data on multiple polls
            if (accumulatedMouseDelta != Vector2.Zero)
            {
                mouseDelta = accumulatedMouseDelta;
                accumulatedMouseDelta = Vector2.Zero;  // Reset for next accumulation
            }
            // If no new mouse data, keep the previous delta for one more frame
            // This ensures smooth mouse input even with timing mismatches
            
            // Reset scroll delta after reading (it doesn't accumulate like mouse movement)
            scrollDelta = 0f;
        }
    }
    
    // Keyboard input
    public bool IsKeyPressed(Key key)
    {
        lock (lockObject)
        {
            return currentKeys.Contains(key);
        }
    }
    
    public bool IsKeyJustPressed(Key key)
    {
        lock (lockObject)
        {
            return currentKeys.Contains(key) && !previousKeys.Contains(key);
        }
    }
    
    public bool IsKeyJustReleased(Key key)
    {
        lock (lockObject)
        {
            return !currentKeys.Contains(key) && previousKeys.Contains(key);
        }
    }
    
    // Mouse input
    public bool IsMouseButtonPressed(MouseButton button)
    {
        lock (lockObject)
        {
            return currentMouseButtons.Contains(button);
        }
    }
    
    public bool IsMouseButtonJustPressed(MouseButton button)
    {
        lock (lockObject)
        {
            return currentMouseButtons.Contains(button) && !previousMouseButtons.Contains(button);
        }
    }
    
    public bool IsMouseButtonJustReleased(MouseButton button)
    {
        lock (lockObject)
        {
            return !currentMouseButtons.Contains(button) && previousMouseButtons.Contains(button);
        }
    }
    
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
    public Vector2 GetMousePosition()
    {
        lock (lockObject)
        {
            return lastMousePosition;
        }
    }
    
    // Input axis mapping
    public float GetAxis(string axisName)
    {
        lock (lockObject)
        {
            return axisName switch
            {
                "Horizontal" => (currentKeys.Contains(Key.D) ? 1f : 0f) - (currentKeys.Contains(Key.A) ? 1f : 0f),
                "Vertical" => (currentKeys.Contains(Key.W) ? 1f : 0f) - (currentKeys.Contains(Key.S) ? 1f : 0f),
                "MouseX" => mouseDelta.X,
                "MouseY" => mouseDelta.Y,
                _ => 0f
            };
        }
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
            inputErrorCount++;
            logger.LogError($"KeyDown event failed for key {key}: {ex.Message}");
            
            // If we're getting too many errors, try to recover
            if (inputErrorCount > MAX_INPUT_ERRORS)
            {
                logger.LogWarning("Too many input errors, attempting to reinitialize keyboard events");
                ReinitializeKeyboardEvents();
            }
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
            logger.LogError($"KeyUp event failed for key {key}: {ex.Message}");
            // Key up failures are less critical, but still log them
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
                
                // Validate position to prevent NaN/Infinity issues
                if (!IsValidVector2(position))
                {
                    logger.LogWarning($"Invalid mouse position received: {position}");
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
            inputErrorCount++;
            logger.LogError($"MouseMove event failed at position {position}: {ex.Message}");
            
            // Mouse movement is critical for FPS games
            if (inputErrorCount > MAX_INPUT_ERRORS)
            {
                logger.LogWarning("Too many mouse errors, attempting recovery");
                ReinitializeMouseEvents();
            }
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
            logger.LogError($"MouseDown event failed for button {button}: {ex.Message}");
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
            logger.LogError($"MouseUp event failed for button {button}: {ex.Message}");
        }
    }
    
    private float scrollDelta = 0f;
    public float GetScrollDelta()
    {
        lock (lockObject)
        {
            return scrollDelta;
        }
    }
    
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
    
    // Helper methods for error recovery
    private bool IsValidVector2(Vector2 v)
    {
        return !float.IsNaN(v.X) && !float.IsNaN(v.Y) && 
               !float.IsInfinity(v.X) && !float.IsInfinity(v.Y);
    }
    
    private void ReinitializeKeyboardEvents()
    {
        try
        {
            if (keyboard != null)
            {
                // Unsubscribe and resubscribe to events
                keyboard.KeyDown -= OnKeyDown;
                keyboard.KeyUp -= OnKeyUp;
                keyboard.KeyDown += OnKeyDown;
                keyboard.KeyUp += OnKeyUp;
                inputErrorCount = 0;
                logger.LogInfo("Keyboard events reinitialized successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to reinitialize keyboard events: {ex.Message}");
        }
    }
    
    private void ReinitializeMouseEvents()
    {
        try
        {
            if (mouse != null)
            {
                // Unsubscribe and resubscribe to events
                mouse.MouseMove -= OnMouseMove;
                mouse.MouseDown -= OnMouseDown;
                mouse.MouseUp -= OnMouseUp;
                mouse.Scroll -= OnMouseScroll;
                
                mouse.MouseMove += OnMouseMove;
                mouse.MouseDown += OnMouseDown;
                mouse.MouseUp += OnMouseUp;
                mouse.Scroll += OnMouseScroll;
                
                inputErrorCount = 0;
                firstMouseMove = true; // Reset first move flag
                logger.LogInfo("Mouse events reinitialized successfully");
            }
        }
        catch (Exception ex)
        {
            logger.LogError($"Failed to reinitialize mouse events: {ex.Message}");
        }
    }
}