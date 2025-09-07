# Code Issues Report - FPS Roguelike

## Summary
This document lists all identified issues in the FPS Roguelike codebase, categorized by severity and type.

## Critical Issues

### 1. No TODO/FIXME Comments Found
- **Location**: Throughout codebase
- **Issue**: No TODO or FIXME comments exist, suggesting incomplete tracking of known issues
- **Impact**: Development debt may be hidden

## High Priority Issues

### 1. Extensive Magic Numbers
- **Location**: Multiple files
- **Issue**: Hard-coded values without named constants make code difficult to maintain and understand
- **Examples**:
  - `Enemy.cs`: 
    - Line 26-34: `moveSpeed = 3f`, `chaseSpeed = 5f`, `rotationSpeed = 3f`, `attackRange = 15f`, `attackCooldown = 2f`, `projectileSpeed = 20f`, `damage = 10f`
    - Line 45-46: `detectionRange = 20f`, `loseTargetRange = 30f`
    - Line 108-115: Hard-coded Y position values (1f) for ground level
    - Line 131: Magic number `2f` for idle timer
    - Line 151: Magic distance `0.5f` for patrol target reached
    - Line 279-281: Magic numbers for random patrol generation (5f, 10f)
  - `PlayerHealth.cs`:
    - Line 16-17: `regenDelay = 5f`, `regenRate = 5f`
    - Line 20: Default health `100f`
  - `Projectile.cs`:
    - Line 14-15: `MAX_LIFETIME = 5f`, `PROJECTILE_RADIUS = 0.2f`
    - Line 40: Ground check at `Y <= 0f`
  - `CharacterController.cs`:
    - Line 8-11: `MoveSpeed = 10f`, `JumpHeight = 2f`, `Gravity = -20f`, `AirControl = 0.3f`
    - Line 19-21: `playerHeight = 1.8f`, `groundCheckDistance = 0.1f`
    - Line 50: Jump calculation magic number `2f`
  - `Camera.cs`:
    - Line 11-13: `FieldOfView = 90f`, `NearPlane = 0.1f`, `FarPlane = 1000f`
    - Line 15-16: `mouseSensitivity = 0.002f`, `maxPitch = 89f`
    - Line 98: Sensitivity clamp values `0.0001f, 0.01f`
  - `Weapon.cs`:
    - Line 8-10: `Damage = 10f`, `FireRate = 0.2f`, `Range = 100f`
    - Line 70: Cube radius `1f` for hit detection
  - `Game.cs`:
    - Line 94: `playerStartPosition = new Vector3(0, 1.7f, 5f)`
    - Line 99: `HIT_MARKER_DURATION = 2.0f`
    - Line 105: `MAX_PROJECTILES = 100`
    - Line 110: `WAVE_DELAY = 5f`
    - Line 114: `PLAYER_RADIUS = 0.5f`

### 2. Resource Management Issues
- **Location**: Multiple classes
- **Issue**: Missing proper disposal patterns
- **Examples**:
  - `InputSystem.cs`: Has `Cleanup()` method but no IDisposable implementation
  - `Game.cs`: Creates OpenGL resources (VAO, VBO, EBO, shaders) but no cleanup/disposal
  - `Renderer.cs`: Likely has similar OpenGL resource management issues
  - `Crosshair.cs`: Creates OpenGL resources without disposal

### 3. Error Handling Deficiencies
- **Location**: Throughout codebase
- **Issue**: Minimal error handling and no try-catch blocks
- **Examples**:
  - `InputSystem.cs`: No null checks for keyboard/mouse in event handlers
  - `Enemy.cs`: No bounds checking for position updates
  - `Projectile.cs`: No validation of input parameters in `Fire()` method
  - OpenGL operations lack error checking

## Medium Priority Issues

### 1. Thread Safety Concerns
- **Location**: `InputSystem.cs`, `Enemy.cs`
- **Issue**: Event handlers modify collections without synchronization
- **Examples**:
  - `InputSystem.cs` lines 89-97, 113-120: Event handlers modify HashSets without locks
  - `Enemy.cs` line 279: Creates new Random instance per call (not thread-safe, inefficient)

### 2. Performance Issues
- **Location**: Multiple areas
- **Issue**: Inefficient operations in hot paths
- **Examples**:
  - `Enemy.cs` line 279: Creating new Random instance every patrol target generation
  - `Weapon.cs` lines 48-59: Hardcoded array allocation in raycast method
  - `InputSystem.cs` lines 46-47: Creating new HashSet copies every poll
  - Multiple Vector3 normalizations that could be cached

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

### 1. Inconsistent Naming
- **Location**: Various
- **Issue**: Mix of naming conventions
- **Examples**:
  - Constants: Some use UPPER_CASE, others use PascalCase
  - Private fields: Some use camelCase, others don't

### 2. Missing Documentation
- **Location**: All classes
- **Issue**: No XML documentation comments for public APIs
- **Impact**: Harder for team collaboration and API understanding

### 3. Console Output in Production Code
- **Location**: Multiple files
- **Issue**: Debug Console.WriteLine statements left in code
- **Examples**:
  - `Enemy.cs` line 267
  - `PlayerHealth.cs` lines 51, 55, 64, 72
  - `Weapon.cs` line 34

### 4. Incomplete Features
- **Location**: `Game.cs`
- **Issue**: Comments indicate placeholder implementations
- **Examples**:
  - Test cube positions hardcoded
  - Simple ground plane instead of proper level geometry

## Potential Bugs

### 1. Floating Point Precision
- **Location**: `Enemy.cs`, `CharacterController.cs`
- **Issue**: Direct floating point comparisons without epsilon
- **Examples**:
  - `Enemy.cs` line 108-115: Direct comparison `Position.Y > 1f`
  - Could cause jittering or missed conditions

### 2. State Management
- **Location**: `Enemy.cs`
- **Issue**: State transitions don't validate current state
- **Risk**: Could transition from Dead state back to other states

### 3. Input Accumulation
- **Location**: `InputSystem.cs`
- **Issue**: Mouse delta accumulation could overflow with high DPI mice
- **Line**: 109 - No bounds checking on accumulated delta

### 4. Division by Zero Risk
- **Location**: `PlayerHealth.cs`
- **Issue**: HealthPercentage property doesn't check for MaxHealth == 0
- **Line**: 8

## Recommendations

1. **Immediate Actions**:
   - Extract all magic numbers to named constants
   - Add proper disposal patterns for resource management
   - Implement basic error handling for critical paths

2. **Short Term**:
   - Add logging system to replace Console.WriteLine
   - Implement proper entity-component system
   - Add unit tests for core systems

3. **Long Term**:
   - Refactor to use dependency injection
   - Implement proper abstraction layers
   - Add comprehensive error handling and recovery
   - Performance profiling and optimization

## Statistics
- Total Issues Found: 35+
- Critical: 1
- High Priority: 3
- Medium Priority: 4
- Low Priority: 4
- Potential Bugs: 4