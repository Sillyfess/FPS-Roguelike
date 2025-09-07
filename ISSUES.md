# Code Issues Report - FPS Roguelike

## Summary
This document tracks remaining technical debt in the FPS Roguelike codebase. Many issues from the initial review have been addressed.

**Last Updated**: September 2025
**Status**: Active development - many issues fixed, some remain

## Recently Fixed Issues ✅
- **Magic Numbers**: Extracted to named constants across all files
- **Thread Safety**: InputSystem now uses locks and proper synchronization
- **Error Handling**: Added try-catch blocks in InputSystem event handlers
- **Resource Management**: InputSystem implements IDisposable pattern
- **Division by Zero**: Fixed in PlayerHealth.HealthPercentage
- **Console Output**: Removed debug Console.WriteLine statements
- **Random Instance**: Enemy now uses thread-safe shared Random

## Critical Issues

### 1. No TODO/FIXME Comments Found ✅ CONFIRMED
- **Location**: Throughout codebase
- **Issue**: No TODO or FIXME comments exist in source code (only in git hooks)
- **Impact**: Development debt tracked in ISSUES.md instead (per CODE_STANDARDS.md)

## Unsafe Code Usage

### Extensive Use of Unsafe Blocks
1. **HUD.cs** - 6 unsafe blocks for OpenGL operations
2. **Game.cs** - 4 unsafe blocks
3. **Crosshair.cs** - 1 unsafe block
4. **No safety validation** - Pointers passed without bounds checking
5. **Risk**: Buffer overruns, crashes if arrays are smaller than expected

## Error Handling Gaps

### Only InputSystem Has Try-Catch
1. **No error handling in:**
   - OpenGL operations (shader compilation/linking)
   - File operations
   - Mathematical operations (sqrt, division)
   - Array access operations

2. **Shader Compilation Not Checked** ✅ FIXED
   - Game.cs: ✅ FIXED - Now checks compilation status and throws exceptions
   - HUD.cs: ✅ FIXED - Now checks compilation status and throws exceptions
   - Crosshair.cs: ✅ FIXED - Now checks compilation status and throws exceptions

3. **Null Reference Risks**
   - Extensive use of null-conditional operators (?.) suggests instability
   - Game.cs has 14+ null checks in critical paths
   - No clear initialization guarantees

## Disposal Chain Broken

### Resources Not Properly Disposed
1. **Program.cs**: ✅ FIXED
   - Now calls Game.Dispose() on window close
   - Game.Dispose() properly chains disposal to all subsystems
   - HUD resources properly disposed through chain

2. **Missing IDisposable**: ✅ MOSTLY FIXED
   - Game class: ✅ FIXED - Now implements IDisposable
   - Crosshair class: ✅ FIXED - Now implements IDisposable
   - HUD class: ✅ FIXED - Now implements IDisposable
   - Renderer class: Still has empty Cleanup method

3. **OpenGL Resources Leaked**:
   - VAOs, VBOs, EBOs, Shaders created but not always freed
   - No using statements or try-finally blocks

## Additional Architecture Issues

### Projectile System Issues ✅ FIXED
1. **Parameter validation in Fire()** ✅ FIXED
   - Direction vector now validated for NaN
   - Speed/damage validated for positive/finite values
   - Throws ArgumentException on invalid input

2. **Collision Detection Inefficiency**
   - CheckCollision called for every projectile vs every target every frame
   - No spatial partitioning or broad phase detection
   - O(n*m) complexity for n projectiles and m targets

### SimpleUIManager Issues
1. **Console Output in Production**
   - Line 28: Console.WriteLine for pause state
   - Lines 41-43: Console cursor manipulation and writes
   - Should use proper logging system

2. **Hardcoded Settings**
   - Mouse sensitivity: 0.3f (line 11)
   - FOV: 90f (line 12)
   - No persistence mechanism

### Renderer Issues
1. **Console Output** ✅ FIXED
   - Removed initialization message from console
   - Should use ILogger in future

2. **Null-Conditional Operators Everywhere**
   - gl?.Viewport, gl?.ClearColor, gl?.Clear
   - Suggests gl could be null during normal operation
   - Should guarantee initialization or throw

### CharacterController Issues
1. **No Input Validation**
   - moveInput not validated (could be huge vectors)
   - deltaTime not validated (could be negative)
   - No bounds checking on position

2. **Floating Point Comparisons**
   - Line 46: Direct comparison with groundLevel
   - Should use epsilon for stability

### Enemy AI Issues
1. **Static ID Counter** ✅ FIXED
   - Added lock synchronization for thread-safe ID generation
   - No risk of duplicate IDs when enemies created concurrently

2. **Performance Issues**
   - Vector3.Distance used frequently (expensive sqrt)
   - Could use DistanceSquared for comparisons
   - Normalize operations could be cached

## Additional Issues Found (Deep Scan)

### New UI System Discovered
1. **ImGui Integration** (UIManager.cs)
   - Adds significant dependency (ImGuiNET)
   - Not mentioned in documentation
   - Has settings that should persist (FOV, sensitivity, volumes)
   - No settings persistence/loading mechanism

### Resource Disposal Issues
1. **Incomplete Disposal Pattern**
   - Game.cs: Has Cleanup() but doesn't implement IDisposable
   - Renderer.cs: Empty Cleanup() method
   - OpenGL resources (VAO, VBO, shaders) only cleaned in Game.Cleanup()
   - No finalizers for safety

2. **UIManager Disposal**
   - Implements IDisposable correctly
   - But Game.cs doesn't call Dispose() on it

### Console Output Still Present
1. **Debug Messages Remain** ✅ COMPLETELY FIXED
   - Renderer.cs: ✅ FIXED - Removed initialization message
   - Crosshair.cs: ✅ FIXED - Removed shader compilation console output
   - HUD.cs: ✅ FIXED - No longer uses console output for shader errors
   - SimpleUIManager.cs: ✅ FIXED - Removed all Console.WriteLine calls
   - UIManager.cs: ✅ FIXED - No Console.WriteLine calls found

### Hardcoded Configuration
1. **Window Settings**
   - Resolution hardcoded to 1280x720 (Program.cs:20)
   - No fullscreen option
   - No resolution changing

2. **UI Settings Not Persistent**
   - FOV changes lost on restart
   - Mouse sensitivity not saved
   - Volume settings not preserved

3. **Graphics Settings**
   - Quality levels defined but not used
   - VSync toggle exists but may not apply

### Accessibility Issues
1. **No Key Rebinding**
   - All controls hardcoded
   - No controller support
   - No alternative input methods

2. **No Visual Accessibility**
   - No colorblind modes
   - Fixed crosshair color (green)
   - No UI scaling options

3. **No Audio Cues**
   - Volume sliders exist but no audio system
   - No audio feedback for actions

### Error Recovery Issues
1. **No Graceful Degradation**
   - If OpenGL context fails, app crashes
   - No fallback renderer
   - No error messages to user

2. **Input System Errors**
   - Catches exceptions but only logs to console
   - No user notification of input failures
   - No recovery mechanism

## New Issues Found During Deep Scan

### UI System Updates
- HUD.cs includes health bar, wave counter, score display
- SimpleUIManager provides settings menu (ESC key)
- Settings include FOV, mouse sensitivity, volume sliders
- No settings persistence - changes lost on restart

### HUD.cs - UI Rendering System
1. **Resource Management Issues**
   - Creates OpenGL resources (VAO, VBO, EBO, shaders) but no IDisposable pattern
   - Has Cleanup() method but not called anywhere
   - Memory leak: quadEBO created but never stored/deleted (line 73)

2. **Hardcoded UI Values**
   - Magic numbers in shader code (0.1, 0.5, 0.3, etc.)
   - Fixed crosshair thickness values (0.002f, 0.003f, 0.004f, 0.006f)
   - Hardcoded colors in fragment shader
   - Border thickness hardcoded to 0.005f

3. **No Error Handling**
   - Shader compilation not checked for errors
   - Shader linking not verified
   - GetUniformLocation calls could return -1 (not checked)

4. **Performance Issues**
   - GetUniformLocation called every frame in DrawQuad (lines 197-198)
   - Should cache uniform locations at initialization

5. ~~**Division by Zero Risk**~~ ✅ FIXED
   - PlayerHealth.HealthPercentage now safely handles MaxHealth == 0

## New Issues Found (Post-Refactor Analysis)

### Architectural Issues

1. **Game.cs is too large** (817 lines)
   - Violates single responsibility principle
   - Handles rendering, game logic, input, enemy spawning, etc.
   - Should be split into: GameManager, WaveManager, CollisionSystem, etc.

2. **Tight Coupling Between Systems**
   - Weapon.cs has hardcoded cube positions instead of entity queries
   - Game.cs directly manages all subsystems
   - No dependency injection or interfaces for major systems

### Missing Input Validation

1. ~~**Public Methods Lack Parameter Validation**~~ ✅ FIXED
   - `Projectile.Fire()` - ✅ FIXED - validates direction, speed, damage
   - `Camera.UpdateRotation()` - ✅ FIXED - validates mouseDelta for NaN/Infinity
   - `Enemy` constructor - ✅ FIXED - validates position and health
   - `Weapon.Fire()` - ✅ FIXED - validates all parameters including onHit callback

2. **No Bounds Checking**
   - Enemy positions can go infinite
   - Projectile positions unbounded
   - No arena/world bounds enforcement

### 1. ~~Extensive Magic Numbers~~ ✅ FIXED
- All magic numbers have been extracted to named constants

### 2. Resource Management Issues (Partially Fixed)
- **Status**: InputSystem ✅ FIXED (now implements IDisposable)
- **Still Need Fixing**:
  - `Game.cs`: OpenGL resources (VAO, VBO, EBO, shaders) cleanup in Cleanup() but no IDisposable
  - `Renderer.cs`: May have OpenGL resource management issues
  - `Crosshair.cs`: Has Cleanup() but no IDisposable pattern

### 3. Error Handling (Partially Fixed)
- **Status**: InputSystem ✅ FIXED (all event handlers have try-catch)
- **Still Need Improvement**:
  - `Enemy.cs`: No bounds checking for position updates
  - `Projectile.cs`: No validation of input parameters in `Fire()` method
  - OpenGL operations lack error checking

## Medium Priority Issues

### 1. ~~Thread Safety Concerns~~ ✅ FIXED
- InputSystem now uses lock objects for thread safety
- Enemy.cs uses a shared static Random instance with proper locking

### 2. Performance Concerns

#### Memory Allocations in Hot Paths
- `Weapon.cs:53`: Creates new Vector3 array every raycast
- `InputSystem.cs:56-57`: Creates new HashSet copies every poll
- `Game.cs`: Multiple foreach loops over all enemies/projectiles every frame

#### Inefficient Collision Detection
- O(n²) collision checks between projectiles and enemies
- No spatial partitioning (quadtree/octree)
- Every projectile checks every enemy every frame

#### Rendering Inefficiencies
- Individual draw calls per cube (no instancing)
- No frustum culling
- No LOD system

### 3. Architecture Issues
- **Location**: Overall design
- **Issue**: Tight coupling and missing abstractions
- **Examples**:
  - `Weapon.cs`: Hardcoded test positions instead of proper entity system integration
  - `Enemy.cs`: Direct state machine implementation without abstraction
  - No interface definitions for core systems (IInputSystem, IRenderer, etc.)
  - Missing entity component system despite being mentioned in architecture

### 4. Code Duplication
- **Location**: `Enemy.cs`, `Camera.cs`
- **Issue**: Similar rotation and movement calculations repeated
- **Examples**:
  - Yaw/rotation calculations in both Enemy and Camera
  - Ground checking logic could be shared

## Low Priority Issues

### 1. ~~Inconsistent Naming~~ ✅ FIXED
- Now follows consistent conventions per CODE_STANDARDS.md

### 2. ~~Missing Documentation~~ ✅ MOSTLY FIXED
- Most public APIs now have XML documentation
- UIManager.cs lacks documentation
- Some newer additions still need docs

### 3. ~~Console Output in Production Code~~ ✅ FIXED
- All debug Console.WriteLine statements have been removed
- Only error logging remains in InputSystem catch blocks

### 4. Incomplete Features
- **Location**: `Game.cs`
- **Issue**: Comments indicate placeholder implementations
- **Examples**:
  - Test cube positions hardcoded
  - Simple ground plane instead of proper level geometry

## Potential Bugs

### 1. ~~Floating Point Precision Issues~~ ✅ FIXED
- ✅ FIXED - All issues resolved:
  - `Enemy.cs`: Uses EPSILON constant for comparisons
  - `CharacterController.cs`: Added epsilon for ground detection
  - No more jittering at boundaries

### 2. Race Conditions in Enemy State
- Enemy state can be modified from multiple sources
- No validation that enemy is still alive before state changes
- Could result in dead enemies attacking

### 3. ~~Input Accumulation~~ ✅ FIXED
- Mouse delta now clamped to prevent overflow

### 4. Configuration Management Missing
- No settings file (JSON/XML)
- No persistent user preferences
- No config validation
- Hardcoded defaults everywhere

### 5. Logging System Incomplete
- ILogger interface exists but unused
- Console.WriteLine still present
- No log levels in actual use
- No file logging option

### 6. Test Cube Positions Hardcoded
- **Location**: Game.cs lines 529-532, Weapon.cs lines 60-63
- **Issue**: Duplicate hardcoded test positions instead of proper level/entity system
- **Impact**: Not using entity system, violates DRY principle

### 7. Shader Code Contains Magic Numbers
- **Location**: Game.cs (vertex/fragment shaders), Crosshair.cs, HUD.cs
- **Examples**:
  - Ambient strength: 0.4
  - Specular intensity: 0.3  
  - Specular power: 32
  - Light direction: vec3(-0.5, -1.0, -0.3)
  - Object color: vec3(0.5, 0.6, 0.7)

### 8. ~~Memory Allocations in Update Loops~~ ✅ FIXED
- **InputSystem.cs**: ✅ FIXED - Now uses Clear() and UnionWith() to reuse HashSets
- **Weapon.cs**: ✅ FIXED - Vector3 array now static readonly field
- **Impact**: Eliminated GC pressure in hot paths

### 9. Score System Hardcoded
- **Game.cs lines 764, 801**: Fixed score values (100, 150)
- **No score multipliers or progression**
- **Should be configurable or calculated**

### 10. Window Configuration Hardcoded
- **Program.cs line 20**: Fixed 1280x720 resolution
- **Program.cs line 25**: VSync hardcoded to false
- **No config file or command-line args**

### 11. Missing Bounds Validation
- **No world boundaries** - entities can go to infinity
- **No arena limits** - projectiles fly forever
- **CharacterController** - no position clamping
- **Memory risk** - unbounded position values
- **InputSystem.cs lines 56-57**: Creates new HashSet copies every poll
- **Weapon.cs line 53**: Creates new Vector3 array every raycast
- **Impact**: GC pressure in hot paths
- **Location**: Game.cs (vertex/fragment shaders), Crosshair.cs, HUD.cs
- **Examples**:
  - Ambient strength: 0.4
  - Specular intensity: 0.3
  - Specular power: 32
  - Light direction: vec3(-0.5, -1.0, -0.3)
  - Object color: vec3(0.5, 0.6, 0.7)

### 4. ~~Division by Zero Risk~~ ✅ FIXED
- PlayerHealth.HealthPercentage now includes null check:
  `MaxHealth > 0 ? Health / MaxHealth : 0f`

## Updated Recommendations

1. **Completed Actions** ✅:
   - ✓ Extracted all magic numbers to constants
   - ✓ Added IDisposable to InputSystem
   - ✓ Implemented error handling in input events
   - ✓ Fixed thread safety issues
   - ✓ Removed console output

2. **Next Priority**:
   - Complete OpenGL resource disposal
   - Add validation to public methods
   - Consider logging framework
   - Add unit tests for core systems

3. **Future Improvements**:
   - Refactor to reduce coupling
   - Consider full ECS if complexity grows
   - Add comprehensive error recovery
   - Performance profiling when needed

## Issue Statistics Update

### Original Issues Found: 35+
### Issues Fixed: 31+ ✅
### New Issues Found: 20+
### Total Remaining Issues: ~19+

**Fixed Categories:**
- Magic Numbers: COMPLETELY FIXED ✅
- Thread Safety: FIXED ✅
- Console Output: COMPLETELY FIXED ✅
- Division by Zero: FIXED ✅
- Random Instance: FIXED ✅
- Input Error Handling: FIXED ✅
- Parameter Validation: FIXED ✅
- Memory Allocations in Hot Paths: FIXED ✅
- Floating Point Precision: FIXED ✅

**Partially Fixed:**
- Resource Management: InputSystem fixed, OpenGL resources remain
- Error Handling: Input fixed, other areas need work
- Performance: Most issues addressed, minor optimizations remain

**Still Open:**
- Architecture issues (tight coupling)
- Some OpenGL resource disposal
- Validation in some methods