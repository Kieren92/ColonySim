using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Represents a single cell in the game grid.
/// </summary>
public class GridCell
{
    public Vector3 WorldPosition { get; private set; }
    public Vector2Int GridPosition { get; private set; }
    public bool IsWalkable { get; set; }
    public bool IsOccupied { get; set; }
    public GameObject OccupyingObject { get; set; }

    // Pathfinding data
    public int GCost { get; set; } // Distance from start
    public int HCost { get; set; } // Distance to target
    public int FCost => GCost + HCost; // Total cost
    public GridCell Parent { get; set; } // For path reconstruction

    public GridCell(Vector2Int gridPos, Vector3 worldPos)
    {
        GridPosition = gridPos;
        WorldPosition = worldPos;
        IsWalkable = true;
        IsOccupied = false;
    }

    public void ResetPathfinding()
    {
        GCost = int.MaxValue;
        HCost = 0;
        Parent = null;
    }
}

/// <summary>
/// Manages the game grid with pathfinding capabilities.
/// </summary>
public class GridSystem : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 50;
    [SerializeField] private int gridHeight = 50;
    [SerializeField] private float cellSize = 1f;

    [Header("Visual Debug")]
    [SerializeField] private bool showDebugGrid = true;
    [SerializeField] private Material groundMaterial;

    private GridCell[,] grid;

    public static GridSystem Instance { get; private set; }

    // Directions for pathfinding (4-directional movement)
    private static readonly Vector2Int[] directions = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // North
        new Vector2Int(1, 0),   // East
        new Vector2Int(0, -1),  // South
        new Vector2Int(-1, 0),  // West
    };

    private void Awake()
    {
        Debug.Log("GridSystem Awake called!");
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CreateGrid();
        CreateVisualGround();
    }

    private void CreateGrid()
    {
        grid = new GridCell[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridHeight; z++)
            {
                Vector3 worldPos = new Vector3(
                    (x - gridWidth / 2f) * cellSize,
                    0,
                    (z - gridHeight / 2f) * cellSize
                );

                grid[x, z] = new GridCell(new Vector2Int(x, z), worldPos);
            }
        }

        Debug.Log($"Grid created: {gridWidth}x{gridHeight} = {gridWidth * gridHeight} cells");
        Debug.Log($"Grid is null? {grid == null}");
    }

    private void CreateVisualGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.parent = transform;
        ground.transform.localPosition = Vector3.zero;

        ground.transform.localScale = new Vector3(
            gridWidth * cellSize / 10f,
            1,
            gridHeight * cellSize / 10f
        );

        if (groundMaterial != null)
        {
            ground.GetComponent<Renderer>().material = groundMaterial;
        }
    }

    /// <summary>
    /// Find a path from start to end using A* pathfinding.
    /// RETURNS: List of world positions forming the path, or null if no path exists.
    /// </summary>
    public List<Vector3> FindPath(Vector3 startWorld, Vector3 endWorld)
    {
        Vector2Int startGrid = WorldToGrid(startWorld);
        Vector2Int endGrid = WorldToGrid(endWorld);

        return FindPath(startGrid, endGrid);
    }

    /// <summary>
    /// A* pathfinding algorithm.
    /// WHY A*: Optimal pathfinding - guaranteed shortest path.
    /// </summary>
    public List<Vector3> FindPath(Vector2Int start, Vector2Int end)
    {
        // Validate positions
        if (!IsValidGridPosition(start) || !IsValidGridPosition(end))
        {
            Debug.LogWarning("Invalid start or end position for pathfinding");
            return null;
        }

        if (!GetCell(end).IsWalkable)
        {
            Debug.LogWarning("Target position is not walkable");
            return null;
        }

        // Reset all pathfinding data
        foreach (var cell in grid)
        {
            cell.ResetPathfinding();
        }

        // Setup
        GridCell startCell = GetCell(start);
        GridCell endCell = GetCell(end);

        List<GridCell> openSet = new List<GridCell>();
        HashSet<GridCell> closedSet = new HashSet<GridCell>();

        startCell.GCost = 0;
        startCell.HCost = GetDistance(start, end);
        openSet.Add(startCell);

        // A* main loop
        while (openSet.Count > 0)
        {
            // Get cell with lowest F cost
            GridCell current = GetLowestFCostCell(openSet);

            // Reached target?
            if (current == endCell)
            {
                return ReconstructPath(endCell);
            }

            openSet.Remove(current);
            closedSet.Add(current);

            // Check neighbors
            foreach (var direction in directions)
            {
                Vector2Int neighborPos = current.GridPosition + direction;

                if (!IsValidGridPosition(neighborPos))
                    continue;

                GridCell neighbor = GetCell(neighborPos);

                if (!neighbor.IsWalkable || closedSet.Contains(neighbor))
                    continue;

                int tentativeGCost = current.GCost + 1; // Each step costs 1

                if (tentativeGCost < neighbor.GCost)
                {
                    neighbor.Parent = current;
                    neighbor.GCost = tentativeGCost;
                    neighbor.HCost = GetDistance(neighborPos, end);

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }

        // No path found
        Debug.LogWarning($"No path found from {start} to {end}");
        return null;
    }

    /// <summary>
    /// Get the cell with lowest F cost from a list.
    /// </summary>
    private GridCell GetLowestFCostCell(List<GridCell> cells)
    {
        GridCell lowest = cells[0];
        for (int i = 1; i < cells.Count; i++)
        {
            if (cells[i].FCost < lowest.FCost)
            {
                lowest = cells[i];
            }
        }
        return lowest;
    }

    /// <summary>
    /// Reconstruct path by following parent pointers.
    /// </summary>
    private List<Vector3> ReconstructPath(GridCell endCell)
    {
        List<Vector3> path = new List<Vector3>();
        GridCell current = endCell;

        while (current != null)
        {
            path.Add(current.WorldPosition);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Manhattan distance heuristic.
    /// WHY: Fast and admissible for grid-based movement.
    /// </summary>
    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt((worldPos.x + (gridWidth * cellSize / 2f)) / cellSize);
        int z = Mathf.RoundToInt((worldPos.z + (gridHeight * cellSize / 2f)) / cellSize);

        x = Mathf.Clamp(x, 0, gridWidth - 1);
        z = Mathf.Clamp(z, 0, gridHeight - 1);

        return new Vector2Int(x, z);
    }

    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        if (IsValidGridPosition(gridPos))
        {
            return grid[gridPos.x, gridPos.y].WorldPosition;
        }
        return Vector3.zero;
    }

    public GridCell GetCell(Vector2Int gridPos)
    {
        if (IsValidGridPosition(gridPos))
        {
            return grid[gridPos.x, gridPos.y];
        }
        return null;
    }

    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    /// <summary>
    /// Mark a cell as occupied by a building.
    /// </summary>
    public void SetCellOccupied(Vector2Int gridPos, bool occupied, GameObject occupyingObject = null)
    {
        GridCell cell = GetCell(gridPos);
        if (cell != null)
        {
            cell.IsOccupied = occupied;
            cell.IsWalkable = !occupied; // Buildings block movement
            cell.OccupyingObject = occupyingObject;
        }
    }

    /// <summary>
    /// Get a random walkable cell.
    /// </summary>
    public Vector2Int GetRandomWalkableCell()
    {
        int attempts = 0;
        while (attempts < 100)
        {
            int x = Random.Range(0, gridWidth);
            int z = Random.Range(0, gridHeight);
            Vector2Int pos = new Vector2Int(x, z);

            GridCell cell = GetCell(pos);
            if (cell.IsWalkable && !cell.IsOccupied)
            {
                return pos;
            }

            attempts++;
        }

        return new Vector2Int(gridWidth / 2, gridHeight / 2); // Fallback to center
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGrid) return;

        if (grid != null)
        {
            Gizmos.color = Color.green;
            foreach (GridCell cell in grid)
            {
                if (!cell.IsWalkable)
                    Gizmos.color = Color.red;
                else if (cell.IsOccupied)
                    Gizmos.color = Color.yellow;
                else
                    Gizmos.color = Color.green;

                Gizmos.DrawWireCube(cell.WorldPosition, Vector3.one * cellSize * 0.9f);
            }
        }
    }

    /// <summary>
    /// Check if a grid cell is walkable (exists and not occupied by unwalkable structure)
    /// </summary>
    public bool IsCellWalkable(Vector2Int gridPos)
    {
        // Check if position is within grid bounds
        if (!IsWithinBounds(gridPos))
        {
            return false;
        }

        GridCell cell = GetCell(gridPos);
        if (cell == null)
        {
            return false;
        }

        // Cell is walkable if it's not occupied OR if it's occupied by something walkable
        // For now, we'll say a cell is walkable if it's not occupied
        // Later we can add logic for structures that block movement vs. those that don't
        return !cell.IsOccupied;
    }

    /// <summary>
    /// Check if position is within grid bounds
    /// </summary>
    public bool IsWithinBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }
}