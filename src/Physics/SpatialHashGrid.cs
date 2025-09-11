using System.Numerics;
using System.Runtime.CompilerServices;

namespace FPSRoguelike.Physics;

/// <summary>
/// Spatial hash grid for efficient broad-phase collision detection.
/// Divides 3D space into uniform cells and hashes entities to cells based on position.
/// </summary>
public class SpatialHashGrid<T> where T : class
{
    // Grid configuration
    private readonly float cellSize;
    private readonly float inverseCellSize;
    
    // Storage: cell coordinates -> list of entities in that cell
    private readonly Dictionary<(int, int, int), List<EntityEntry>> grid;
    
    // Entity tracking for updates/removals
    private readonly Dictionary<T, (int x, int y, int z)> entityCells;
    
    // Reusable collections to avoid allocations
    private readonly HashSet<T> queryResults;
    private readonly List<(int, int, int)> neighborOffsets;
    
    // Statistics for performance monitoring
    public int CellCount => grid.Count;
    public int EntityCount => entityCells.Count;
    
    private struct EntityEntry
    {
        public T Entity;
        public Vector3 Position;
        public float Radius;
    }
    
    public SpatialHashGrid(float cellSize = 5.0f)
    {
        if (cellSize <= 0)
            throw new ArgumentException("Cell size must be positive", nameof(cellSize));
            
        this.cellSize = cellSize;
        this.inverseCellSize = 1.0f / cellSize;
        
        grid = new Dictionary<(int, int, int), List<EntityEntry>>();
        entityCells = new Dictionary<T, (int, int, int)>();
        queryResults = new HashSet<T>();
        
        // Precompute 27 neighbor offsets (including center cell)
        neighborOffsets = new List<(int, int, int)>(27);
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    neighborOffsets.Add((x, y, z));
                }
            }
        }
    }
    
    /// <summary>
    /// Insert or update an entity in the grid
    /// </summary>
    public void Insert(T entity, Vector3 position, float radius)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));
            
        // Remove from old cell if already in grid
        if (entityCells.TryGetValue(entity, out var oldCell))
        {
            RemoveFromCell(entity, oldCell);
        }
        
        // Calculate cell coordinates
        var cell = GetCellCoordinates(position);
        
        // Add to new cell
        if (!grid.TryGetValue(cell, out var entities))
        {
            entities = new List<EntityEntry>();
            grid[cell] = entities;
        }
        
        entities.Add(new EntityEntry 
        { 
            Entity = entity, 
            Position = position, 
            Radius = radius 
        });
        
        entityCells[entity] = cell;
    }
    
    /// <summary>
    /// Remove an entity from the grid
    /// </summary>
    public void Remove(T entity)
    {
        if (entity == null)
            return;
            
        if (entityCells.TryGetValue(entity, out var cell))
        {
            RemoveFromCell(entity, cell);
            entityCells.Remove(entity);
        }
    }
    
    /// <summary>
    /// Query for entities potentially colliding with a sphere
    /// Returns a new list to prevent external modification of internal state
    /// </summary>
    public List<T> Query(Vector3 position, float radius)
    {
        queryResults.Clear();
        
        // Calculate range of cells to check
        var centerCell = GetCellCoordinates(position);
        
        // Check all neighboring cells
        foreach (var offset in neighborOffsets)
        {
            var cell = (centerCell.x + offset.Item1, 
                       centerCell.y + offset.Item2, 
                       centerCell.z + offset.Item3);
            
            if (grid.TryGetValue(cell, out var entities))
            {
                // Add entities from this cell that could potentially collide
                foreach (var entry in entities)
                {
                    // Quick sphere-sphere broad phase check
                    float distSqr = Vector3.DistanceSquared(position, entry.Position);
                    float radiusSum = radius + entry.Radius;
                    
                    if (distSqr <= radiusSum * radiusSum)
                    {
                        queryResults.Add(entry.Entity);
                    }
                }
            }
        }
        
        // Return a copy to prevent external modification
        return queryResults.ToList();
    }
    
    /// <summary>
    /// Query for entities in a specific cell and its neighbors
    /// </summary>
    public IEnumerable<T> QueryCell(int x, int y, int z)
    {
        foreach (var offset in neighborOffsets)
        {
            var cell = (x + offset.Item1, y + offset.Item2, z + offset.Item3);
            
            if (grid.TryGetValue(cell, out var entities))
            {
                foreach (var entry in entities)
                {
                    yield return entry.Entity;
                }
            }
        }
    }
    
    /// <summary>
    /// Get all entity pairs that might be colliding (for entity-entity collision)
    /// </summary>
    public IEnumerable<(T, T)> GetPotentialCollisionPairs()
    {
        var checkedPairs = new HashSet<(T, T)>();
        
        foreach (var cell in grid.Values)
        {
            // Check entities within the same cell
            for (int i = 0; i < cell.Count; i++)
            {
                for (int j = i + 1; j < cell.Count; j++)
                {
                    var pair = OrderPair(cell[i].Entity, cell[j].Entity);
                    if (checkedPairs.Add(pair))
                    {
                        yield return pair;
                    }
                }
            }
        }
        
        // Check entities in adjacent cells
        foreach (var (cellCoord, cellEntities) in grid)
        {
            // Only check half the neighbors to avoid duplicate checks
            for (int i = 0; i < 13; i++) // Half of 27 neighbors
            {
                var offset = neighborOffsets[i];
                var neighborCell = (cellCoord.Item1 + offset.Item1,
                                   cellCoord.Item2 + offset.Item2,
                                   cellCoord.Item3 + offset.Item3);
                
                if (grid.TryGetValue(neighborCell, out var neighborEntities))
                {
                    foreach (var entity1 in cellEntities)
                    {
                        foreach (var entity2 in neighborEntities)
                        {
                            var pair = OrderPair(entity1.Entity, entity2.Entity);
                            if (checkedPairs.Add(pair))
                            {
                                yield return pair;
                            }
                        }
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Clear all entities from the grid
    /// </summary>
    public void Clear()
    {
        grid.Clear();
        entityCells.Clear();
        queryResults.Clear();
    }
    
    /// <summary>
    /// Update an entity's position (more efficient than Remove + Insert)
    /// </summary>
    public void Update(T entity, Vector3 newPosition, float radius)
    {
        if (!entityCells.TryGetValue(entity, out var oldCell))
        {
            // Entity not in grid, just insert it
            Insert(entity, newPosition, radius);
            return;
        }
        
        var newCell = GetCellCoordinates(newPosition);
        
        // If still in same cell, just update position
        if (oldCell == newCell)
        {
            if (grid.TryGetValue(oldCell, out var entities))
            {
                for (int i = 0; i < entities.Count; i++)
                {
                    if (ReferenceEquals(entities[i].Entity, entity))
                    {
                        entities[i] = new EntityEntry 
                        { 
                            Entity = entity, 
                            Position = newPosition, 
                            Radius = radius 
                        };
                        break;
                    }
                }
            }
        }
        else
        {
            // Moved to different cell
            RemoveFromCell(entity, oldCell);
            Insert(entity, newPosition, radius);
        }
    }
    
    private (int x, int y, int z) GetCellCoordinates(Vector3 position)
    {
        return (
            (int)MathF.Floor(position.X * inverseCellSize),
            (int)MathF.Floor(position.Y * inverseCellSize),
            (int)MathF.Floor(position.Z * inverseCellSize)
        );
    }
    
    private void RemoveFromCell(T entity, (int, int, int) cell)
    {
        if (grid.TryGetValue(cell, out var entities))
        {
            // More efficient removal using index-based approach
            for (int i = entities.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(entities[i].Entity, entity))
                {
                    entities.RemoveAt(i);
                    break; // Only one instance per entity in a cell
                }
            }
            
            // Remove cell if empty to save memory
            if (entities.Count == 0)
            {
                grid.Remove(cell);
            }
        }
    }
    
    private (T, T) OrderPair(T a, T b)
    {
        // Use object reference comparison for consistent ordering
        // This avoids hash collision issues
        if (ReferenceEquals(a, b)) return (a, b);
        
        // Use RuntimeHelpers to get a stable hash code based on object identity
        int hashA = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(a);
        int hashB = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(b);
        
        // If hashes are equal (rare), use reference comparison as tiebreaker
        if (hashA == hashB)
        {
            // Compare memory addresses for a stable ordering
            unsafe
            {
                var ptrA = *(IntPtr*)Unsafe.AsPointer(ref a);
                var ptrB = *(IntPtr*)Unsafe.AsPointer(ref b);
                return ptrA.ToInt64() < ptrB.ToInt64() ? (a, b) : (b, a);
            }
        }
        
        return hashA < hashB ? (a, b) : (b, a);
    }
    
    /// <summary>
    /// Get debug statistics about grid usage
    /// </summary>
    public string GetDebugStats()
    {
        int totalEntities = 0;
        int maxEntitiesPerCell = 0;
        float avgEntitiesPerCell = 0;
        
        foreach (var cell in grid.Values)
        {
            totalEntities += cell.Count;
            maxEntitiesPerCell = Math.Max(maxEntitiesPerCell, cell.Count);
        }
        
        if (grid.Count > 0)
        {
            avgEntitiesPerCell = (float)totalEntities / grid.Count;
        }
        
        return $"Cells: {grid.Count}, Entities: {totalEntities}, Max/Cell: {maxEntitiesPerCell}, Avg/Cell: {avgEntitiesPerCell:F1}";
    }
}