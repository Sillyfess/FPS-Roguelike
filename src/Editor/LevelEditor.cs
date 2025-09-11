using System.Numerics;
using Silk.NET.OpenGL;
using Silk.NET.Input;
using FPSRoguelike.Environment;
using FPSRoguelike.Input;

namespace FPSRoguelike.Editor;

/// <summary>
/// Main level editor system
/// </summary>
public class LevelEditor : IDisposable
{
    // OpenGL context
    private GL? gl;
    
    // Editor components
    private EditorCamera camera;
    private GridRenderer? gridRenderer;
    private EditorUI? editorUI;
    private EditorUI.EditorState uiState;
    
    // Current level
    private Level currentLevel;
    private string? currentLevelPath;
    private bool hasUnsavedChanges = false;
    
    // Editor state
    private bool isActive = false;
    private ObstacleType currentObstacleType = ObstacleType.Crate;
    private List<Obstacle> selectedObjects = new List<Obstacle>();
    private Obstacle? ghostObject = null;
    private float placementDistance = 10f;
    
    // Undo/Redo system
    private Stack<EditorAction> undoStack = new Stack<EditorAction>();
    private Stack<EditorAction> redoStack = new Stack<EditorAction>();
    
    // Grid settings
    private bool gridSnapping = true;
    private float gridSize = 1f;
    
    // Input state
    private bool isDragging = false;
    private bool isRotating = false;
    
    // Constants
    private const float PLACEMENT_DISTANCE_MIN = 2f;
    private const float PLACEMENT_DISTANCE_MAX = 50f;
    private const float PLACEMENT_DISTANCE_SCROLL_SPEED = 2f;
    private const float ROTATION_SPEED = 90f; // degrees per second
    private const string LEVELS_DIRECTORY = "Levels";
    
    // Editor actions for undo/redo
    private abstract class EditorAction
    {
        public abstract void Execute(LevelEditor editor);
        public abstract void Undo(LevelEditor editor);
    }
    
    private class PlaceObjectAction : EditorAction
    {
        private Obstacle obstacle;
        
        public PlaceObjectAction(Obstacle obj) => obstacle = obj;
        
        public override void Execute(LevelEditor editor)
        {
            editor.currentLevel.Obstacles.Add(new Level.ObstacleData(obstacle));
            editor.hasUnsavedChanges = true;
        }
        
        public override void Undo(LevelEditor editor)
        {
            var lastObstacle = editor.currentLevel.Obstacles.LastOrDefault();
            if (lastObstacle != null)
            {
                editor.currentLevel.Obstacles.Remove(lastObstacle);
                editor.hasUnsavedChanges = true;
            }
        }
    }
    
    private class DeleteObjectAction : EditorAction
    {
        private Level.ObstacleData obstacleData;
        private int index;
        
        public DeleteObjectAction(Level.ObstacleData data, int idx)
        {
            obstacleData = data;
            index = idx;
        }
        
        public override void Execute(LevelEditor editor)
        {
            editor.currentLevel.Obstacles.RemoveAt(index);
            editor.hasUnsavedChanges = true;
        }
        
        public override void Undo(LevelEditor editor)
        {
            editor.currentLevel.Obstacles.Insert(index, obstacleData);
            editor.hasUnsavedChanges = true;
        }
    }
    
    public LevelEditor(GL openGL)
    {
        gl = openGL;
        
        // Initialize camera at a good viewing angle
        camera = new EditorCamera(new Vector3(10, 10, 10), -45f, -30f);
        camera.LookAt(Vector3.Zero);
        
        // Initialize components
        gridRenderer = new GridRenderer(openGL);
        editorUI = new EditorUI(openGL);
        uiState = new EditorUI.EditorState();
        
        // Create new empty level
        currentLevel = new Level();
        
        // Create levels directory if it doesn't exist
        if (!Directory.Exists(LEVELS_DIRECTORY))
        {
            Directory.CreateDirectory(LEVELS_DIRECTORY);
        }
    }
    
    /// <summary>
    /// Toggle editor on/off
    /// </summary>
    public void Toggle()
    {
        isActive = !isActive;
        uiState.IsActive = isActive;
        
        if (isActive)
        {
            SetStatusMessage("Level Editor Activated - Press H for help");
        }
    }
    
    /// <summary>
    /// Check if editor is active
    /// </summary>
    public bool IsActive => isActive;
    
    /// <summary>
    /// Update editor logic
    /// </summary>
    public void Update(float deltaTime, InputSystem input)
    {
        if (!isActive || input == null) return;
        
        // Camera movement input
        Vector3 moveInput = Vector3.Zero;
        if (input.IsKeyPressed(Key.W)) moveInput.Z += 1;
        if (input.IsKeyPressed(Key.S)) moveInput.Z -= 1;
        if (input.IsKeyPressed(Key.A)) moveInput.X -= 1;
        if (input.IsKeyPressed(Key.D)) moveInput.X += 1;
        if (input.IsKeyPressed(Key.Q)) moveInput.Y -= 1;
        if (input.IsKeyPressed(Key.E)) moveInput.Y += 1;
        
        bool fastMove = input.IsKeyPressed(Key.ShiftLeft);
        bool slowMove = input.IsKeyPressed(Key.ControlLeft);
        
        // Update camera
        camera.Update(deltaTime, moveInput, fastMove, slowMove, input.GetScrollDelta());
        
        // Mouse look
        Vector2 mouseDelta = input.GetMouseDelta();
        if (input.IsMouseButtonPressed(MouseButton.Middle))
        {
            camera.Rotate(mouseDelta.X, mouseDelta.Y);
        }
        
        // Handle editor shortcuts
        HandleEditorInput(input, deltaTime);
        
        // Update ghost object for placement preview
        UpdateGhostObject(input);
        
        // Update UI state
        UpdateUIState();
    }
    
    private void HandleEditorInput(InputSystem input, float deltaTime)
    {
        bool ctrl = input.IsKeyPressed(Key.ControlLeft) || input.IsKeyPressed(Key.ControlRight);
        bool shift = input.IsKeyPressed(Key.ShiftLeft) || input.IsKeyPressed(Key.ShiftRight);
        
        // File operations
        if (ctrl)
        {
            if (input.IsKeyJustPressed(Key.S)) SaveLevel();
            if (input.IsKeyJustPressed(Key.O)) OpenLevel();
            if (input.IsKeyJustPressed(Key.N)) NewLevel();
            if (input.IsKeyJustPressed(Key.Z)) Undo();
            if (input.IsKeyJustPressed(Key.Y)) Redo();
            if (input.IsKeyJustPressed(Key.A)) SelectAll();
            if (input.IsKeyJustPressed(Key.D)) DuplicateSelected();
        }
        
        // Object type cycling
        if (input.IsKeyJustPressed(Key.Tab))
        {
            CycleObstacleType();
        }
        
        // Placement distance adjustment with shift+scroll
        if (shift)
        {
            float scrollDelta = input.GetScrollDelta();
            if (Math.Abs(scrollDelta) > 0.01f)
            {
                placementDistance *= 1f + (scrollDelta * 0.1f);
                placementDistance = Math.Clamp(placementDistance, PLACEMENT_DISTANCE_MIN, PLACEMENT_DISTANCE_MAX);
            }
        }
        
        // Object placement/selection
        if (input.IsMouseButtonJustPressed(MouseButton.Left))
        {
            if (ghostObject != null)
            {
                PlaceObject();
            }
            else
            {
                SelectObject(input.GetMousePosition());
            }
        }
        
        // Object deletion
        if (input.IsMouseButtonJustPressed(MouseButton.Right))
        {
            DeleteObjectAtCursor(input.GetMousePosition());
        }
        
        // Delete key
        if (input.IsKeyJustPressed(Key.Delete))
        {
            DeleteSelected();
        }
        
        // Movement/Rotation
        if (input.IsKeyJustPressed(Key.G))
        {
            StartMoving();
        }
        
        if (input.IsKeyJustPressed(Key.R))
        {
            if (isRotating)
            {
                isRotating = false;
            }
            else if (selectedObjects.Count > 0)
            {
                isRotating = true;
                SetStatusMessage("Rotating selected objects");
            }
        }
        
        // Apply rotation
        if (isRotating && selectedObjects.Count > 0)
        {
            float rotationDelta = ROTATION_SPEED * deltaTime;
            if (input.IsKeyPressed(Key.Left)) rotationDelta = -rotationDelta;
            else if (!input.IsKeyPressed(Key.Right)) rotationDelta = 0;
            
            foreach (var obj in selectedObjects)
            {
                obj.Rotation += rotationDelta * (MathF.PI / 180f);
            }
            
            if (rotationDelta != 0) hasUnsavedChanges = true;
        }
        
        // Grid snapping toggle
        if (input.IsKeyJustPressed(Key.Number1))
        {
            gridSnapping = !gridSnapping;
            SetStatusMessage($"Grid snapping: {(gridSnapping ? "ON" : "OFF")}");
        }
        
        // Grid size adjustment
        if (input.IsKeyJustPressed(Key.LeftBracket))
        {
            gridSize = Math.Max(0.25f, gridSize / 2f);
            SetStatusMessage($"Grid size: {gridSize}");
        }
        if (input.IsKeyJustPressed(Key.RightBracket))
        {
            gridSize = Math.Min(10f, gridSize * 2f);
            SetStatusMessage($"Grid size: {gridSize}");
        }
        
        // Help toggle
        if (input.IsKeyJustPressed(Key.H))
        {
            uiState.ShowHelp = !uiState.ShowHelp;
        }
    }
    
    private void UpdateGhostObject(InputSystem input)
    {
        // Create ghost object for placement preview
        var (origin, direction) = camera.GetViewRay();
        Vector3 placementPos = origin + direction * placementDistance;
        
        // Snap to grid if enabled
        if (gridSnapping)
        {
            placementPos.X = MathF.Round(placementPos.X / gridSize) * gridSize;
            placementPos.Y = MathF.Round(placementPos.Y / gridSize) * gridSize;
            placementPos.Z = MathF.Round(placementPos.Z / gridSize) * gridSize;
        }
        
        // Create or update ghost object
        if (ghostObject == null || ghostObject.Type != currentObstacleType)
        {
            ghostObject = new Obstacle(placementPos, currentObstacleType);
        }
        else
        {
            ghostObject.Position = placementPos;
        }
    }
    
    private void UpdateUIState()
    {
        uiState.CurrentTool = isRotating ? "Rotate" : isDragging ? "Move" : "Place";
        uiState.CurrentObstacleType = currentObstacleType;
        uiState.SelectedObjectCount = selectedObjects.Count;
        uiState.GridSnapping = gridSnapping;
        uiState.GridSize = gridSize;
        uiState.UndoStackSize = undoStack.Count;
        uiState.RedoStackSize = redoStack.Count;
    }
    
    private void CycleObstacleType()
    {
        var types = Enum.GetValues<ObstacleType>();
        int currentIndex = Array.IndexOf(types, currentObstacleType);
        currentIndex = (currentIndex + 1) % types.Length;
        currentObstacleType = types[currentIndex];
        SetStatusMessage($"Object type: {currentObstacleType}");
    }
    
    private void PlaceObject()
    {
        if (ghostObject == null) return;
        
        var action = new PlaceObjectAction(ghostObject);
        ExecuteAction(action);
        
        // Create new ghost object for next placement
        ghostObject = new Obstacle(ghostObject.Position, ghostObject.Type);
        SetStatusMessage($"Placed {currentObstacleType}");
    }
    
    private void SelectObject(Vector2 mousePos)
    {
        // TODO: Implement ray casting to select objects
        // For now, clear selection
        selectedObjects.Clear();
    }
    
    private void DeleteObjectAtCursor(Vector2 mousePos)
    {
        // TODO: Implement ray casting to find object under cursor
        // For now, delete last placed object
        if (currentLevel.Obstacles.Count > 0)
        {
            int index = currentLevel.Obstacles.Count - 1;
            var action = new DeleteObjectAction(currentLevel.Obstacles[index], index);
            ExecuteAction(action);
            SetStatusMessage("Deleted object");
        }
    }
    
    private void DeleteSelected()
    {
        if (selectedObjects.Count == 0) return;
        
        // TODO: Delete all selected objects
        selectedObjects.Clear();
        SetStatusMessage($"Deleted {selectedObjects.Count} objects");
    }
    
    private void SelectAll()
    {
        selectedObjects.Clear();
        selectedObjects.AddRange(currentLevel.GetObstacles());
        SetStatusMessage($"Selected {selectedObjects.Count} objects");
    }
    
    private void DuplicateSelected()
    {
        if (selectedObjects.Count == 0) return;
        
        foreach (var obj in selectedObjects.ToList())
        {
            var duplicate = new Obstacle(obj.Position + Vector3.One * 2f, obj.Type, obj.Rotation);
            var action = new PlaceObjectAction(duplicate);
            ExecuteAction(action);
        }
        
        SetStatusMessage($"Duplicated {selectedObjects.Count} objects");
    }
    
    private void StartMoving()
    {
        if (selectedObjects.Count > 0)
        {
            isDragging = true;
            SetStatusMessage("Moving selected objects");
        }
    }
    
    private void ExecuteAction(EditorAction action)
    {
        action.Execute(this);
        undoStack.Push(action);
        redoStack.Clear();
        hasUnsavedChanges = true;
    }
    
    private void Undo()
    {
        if (undoStack.Count > 0)
        {
            var action = undoStack.Pop();
            action.Undo(this);
            redoStack.Push(action);
            SetStatusMessage("Undo");
        }
    }
    
    private void Redo()
    {
        if (redoStack.Count > 0)
        {
            var action = redoStack.Pop();
            action.Execute(this);
            undoStack.Push(action);
            SetStatusMessage("Redo");
        }
    }
    
    private void SaveLevel()
    {
        if (string.IsNullOrEmpty(currentLevelPath))
        {
            // Generate default filename
            currentLevelPath = Path.Combine(LEVELS_DIRECTORY, $"level_{DateTime.Now:yyyyMMdd_HHmmss}.json");
        }
        
        try
        {
            currentLevel.SaveToFile(currentLevelPath);
            hasUnsavedChanges = false;
            SetStatusMessage($"Saved: {Path.GetFileName(currentLevelPath)}");
        }
        catch (Exception ex)
        {
            SetStatusMessage($"Save failed: {ex.Message}", true);
        }
    }
    
    private void OpenLevel()
    {
        // For now, just load the first level file found
        var levelFiles = Directory.GetFiles(LEVELS_DIRECTORY, "*.json");
        if (levelFiles != null && levelFiles.Length > 0)
        {
            try
            {
                currentLevel = Level.LoadFromFile(levelFiles[0]);
                currentLevelPath = levelFiles[0];
                hasUnsavedChanges = false;
                SetStatusMessage($"Loaded: {Path.GetFileName(levelFiles[0])}");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Load failed: {ex.Message}", true);
            }
        }
        else
        {
            SetStatusMessage("No level files found", true);
        }
    }
    
    private void NewLevel()
    {
        if (hasUnsavedChanges)
        {
            // TODO: Show confirmation dialog
        }
        
        currentLevel = new Level();
        currentLevelPath = null;
        hasUnsavedChanges = false;
        selectedObjects.Clear();
        undoStack.Clear();
        redoStack.Clear();
        SetStatusMessage("New level created");
    }
    
    private void SetStatusMessage(string message, bool isError = false)
    {
        uiState.StatusMessage = message;
        uiState.StatusMessageTime = DateTime.Now;
        Console.WriteLine($"[Editor] {message}");
    }
    
    /// <summary>
    /// Render the editor view
    /// </summary>
    public void Render(float screenWidth, float screenHeight)
    {
        if (!isActive || gl == null) return;
        
        float aspectRatio = screenWidth / screenHeight;
        Matrix4x4 view = camera.GetViewMatrix();
        Matrix4x4 projection = camera.GetProjectionMatrix(aspectRatio);
        
        // Render grid
        gridRenderer?.Render(view, projection);
        
        // Render ghost object with transparency
        if (ghostObject != null)
        {
            // TODO: Render ghost object with transparency
        }
        
        // Render selected objects with highlight
        // TODO: Render selection highlights
        
        // Render UI
        editorUI?.Render(uiState, screenWidth, screenHeight);
    }
    
    /// <summary>
    /// Get the current level obstacles for rendering
    /// </summary>
    public List<Obstacle> GetLevelObstacles()
    {
        return currentLevel.GetObstacles();
    }
    
    /// <summary>
    /// Get the camera for rendering
    /// </summary>
    public EditorCamera GetCamera() => camera;
    
    /// <summary>
    /// Load a level into the game
    /// </summary>
    public static Level? LoadLevelForGame(string filename)
    {
        string path = Path.Combine(LEVELS_DIRECTORY, filename);
        if (File.Exists(path))
        {
            try
            {
                return Level.LoadFromFile(path);
            }
            catch
            {
                return null;
            }
        }
        return null;
    }
    
    public void Dispose()
    {
        gridRenderer?.Dispose();
        editorUI?.Dispose();
    }
}