# Code Standards & Guidelines

## Naming Conventions

### Classes and Methods
- **Classes**: PascalCase (e.g., `PlayerHealth`, `CharacterController`)
- **Public Methods**: PascalCase (e.g., `TakeDamage`, `UpdateRotation`)
- **Private Methods**: PascalCase (e.g., `HandleIdleState`, `GenerateNewPatrolTarget`)
- **Interfaces**: IPascalCase with 'I' prefix (e.g., `ILogger`, `IDisposable`)

### Variables and Fields
- **Public Properties**: PascalCase (e.g., `Health`, `IsAlive`)
- **Private Fields**: camelCase (e.g., `moveSpeed`, `lastAttackTime`)
- **Local Variables**: camelCase (e.g., `distanceToPlayer`, `direction`)
- **Parameters**: camelCase (e.g., `deltaTime`, `playerPosition`)

### Constants
- **All Constants**: UPPER_SNAKE_CASE with descriptive prefixes
  - `DEFAULT_*` for default values (e.g., `DEFAULT_MOVE_SPEED`)
  - `MAX_*` for maximum values (e.g., `MAX_PROJECTILES`)
  - `MIN_*` for minimum values (e.g., `MIN_PATROL_DISTANCE`)
  - Other descriptive names (e.g., `HIT_MARKER_DURATION`, `FIXED_TIMESTEP`)

## Code Organization

### File Structure
- One class per file (exceptions: small related classes like loggers)
- File name matches primary class name
- Using statements at top, sorted alphabetically
- Namespace matches folder structure

### Member Order Within Classes
1. Constants (public then private)
2. Static fields
3. Instance fields (public then private)
4. Properties
5. Constructors
6. Public methods
7. Private methods
8. Nested types

## Comments Policy
- **DO** add XML documentation comments (`///`) for public classes and methods
- **DO** add brief inline comments for complex logic or non-obvious calculations
- **DO** use descriptive variable and method names that reduce need for comments
- **DON'T** add obvious comments that repeat what the code does
- **DON'T** leave TODO/FIXME comments - track in ISSUES.md instead

## Common Patterns

### Thread Safety
```csharp
private readonly object lockObject = new object();
lock (lockObject) { /* critical section */ }

// Shared instances
private static readonly Random sharedRandom = new Random();
```

### Null Handling
```csharp
// Null-conditional operators
var result = object?.Property?.Method() ?? defaultValue;

// Guard clauses
if (enemy == null) throw new ArgumentNullException(nameof(enemy));
if (!enemy.IsActive) return;
```

### Performance
```csharp
// Cache expensive calculations
Vector3 direction = toTarget / distance;  // Not Vector3.Normalize(toTarget)

// Use squared distance for comparisons
if (distanceSquared < radius * radius)  // Avoid sqrt
```

### Resource Management
```csharp
public class ResourceClass : IDisposable
{
    private bool disposed = false;
    
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
                // Dispose managed resources
            }
            disposed = true;
        }
    }
}
```

## Bracing and Formatting

### Always Use Braces
```csharp
// Correct
if (condition)
{
    DoSomething();
}

// Wrong
if (condition)
    DoSomething();
```

### String Formatting
```csharp
// Use string interpolation
Console.WriteLine($"Player health: {health}/{maxHealth}");

// Not concatenation
Console.WriteLine("Player health: " + health + "/" + maxHealth);
```

## Testing Standards

### Test Naming
```csharp
// MethodName_StateUnderTest_ExpectedBehavior
[Test]
public void TakeDamage_WhenPlayerIsDead_DoesNotReduceHealth()
{
    // Arrange
    var player = new PlayerHealth(100f);
    player.TakeDamage(100f);
    
    // Act
    player.TakeDamage(10f);
    
    // Assert
    Assert.AreEqual(0f, player.Health);
}
```

## Version Control

### Commit Messages
```
[Type] Brief description (max 50 chars)

Detailed explanation if needed (wrap at 72 chars)
- Bullet points for multiple changes

Types: feat, fix, docs, style, refactor, perf, test, chore
```

### Branch Naming
- `feature/description` - New features
- `fix/issue-description` - Bug fixes
- `refactor/what-changed` - Code improvements
- `docs/what-updated` - Documentation only

## Checklist for Code Review

✓ **Naming**: Follows conventions (PascalCase, camelCase, UPPER_SNAKE_CASE)
✓ **Constants**: No magic numbers, all use descriptive UPPER_SNAKE_CASE
✓ **Comments**: XML docs on public members, inline comments for complex logic
✓ **Error Handling**: Try-catch for external operations, input validation
✓ **Performance**: Cached calculations, efficient algorithms
✓ **Resources**: IDisposable pattern, proper cleanup in Dispose()
✓ **Thread Safety**: Locks for shared state, no race conditions
✓ **Code Style**: Consistent spacing, brace placement, member ordering