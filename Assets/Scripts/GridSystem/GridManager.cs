// GridSystem.cs
using UnityEngine;
using System.Collections.Generic;

public class GridManager : Singleton<GridManager>
{
    [Header("Grid Settings")]
    public Vector2Int gridSize = new Vector2Int(10, 10);
    public float cellSize = 1f;
    public Vector2 gridOrigin = Vector2.zero;
    public bool showGridGizmos = true;

    [Header("Debug")]
    public GameObject gridCellPrefab;

    private Transform player;
    private Cell[,] grid;
    private List<Cell> edgeCell = new List<Cell>();
    public List<Cell> EdgeCell { get { return edgeCell; } }


    void Start()
    {
        player = GameManager.Instance.player.transform;
        InitializeGrid();
        SetEdgeCells();
    }
    void InitializeGrid()
    {
        grid = new Cell[gridSize.x, gridSize.y];

        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2 worldPos = GridToWorldPosition(new Vector2Int(x, y));
                grid[x, y] = new Cell(
                    position: new Vector2Int(x, y),
                    worldPosition: worldPos,
                    isWalkable: true
                );

                if (gridCellPrefab)
                {
                    GameObject cellObj = Instantiate(gridCellPrefab, worldPos, Quaternion.identity);
                    cellObj.transform.SetParent(transform);
                    grid[x, y].debugObject = cellObj;
                }
            }
        }
    }

    #region Coordinate Conversion
    public Vector2 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector2(
            gridPos.x * cellSize + gridOrigin.x,
            gridPos.y * cellSize + gridOrigin.y
        );
    }
    public Vector2Int WorldToGridPosition(Vector2 worldPos)
    {
        return new Vector2Int(
           Mathf.FloorToInt(Mathf.Clamp(((worldPos.x - gridOrigin.x) / cellSize), 0, gridSize.x - 1)),
           Mathf.FloorToInt(Mathf.Clamp(((worldPos.y - gridOrigin.y) / cellSize), 0, gridSize.y - 1))
        );
    }
    #endregion

    #region Pathfinding
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        Cell startCell = grid[start.x, start.y];
        Cell endCell = grid[end.x, end.y];
        List<Cell> openSet = new List<Cell>();
        HashSet<Cell> closedSet = new HashSet<Cell>();

        startCell.hCost = GetDistance(start, end);
        openSet.Add(startCell);

        int maxIterations = 50;
        int iterations = 0;

        while (openSet.Count > 0 && iterations < maxIterations)
        {
            iterations++;
            Cell currentCell = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].fCost <= currentCell.fCost)
                {
                    if (openSet[i].hCost < currentCell.hCost)
                        currentCell = openSet[i];
                }
            }
            openSet.Remove(currentCell);
            closedSet.Add(currentCell);

            if (currentCell.position == end)
            {
                foreach (Cell cell in closedSet)
                {
                    cell.parent = null;
                    cell.hCost = 0;
                    cell.gCost = 0;
                }
                return RetracePath(currentCell);
            }

            foreach (Cell neighbor in GetNeighbors(currentCell))
            {
                if (!neighbor.isWalkable || closedSet.Contains(neighbor))
                {
                    continue;
                }
                float newCostToNeighbour = currentCell.gCost + GetDistance(currentCell.position, neighbor.position);
                if (newCostToNeighbour < neighbor.gCost || !openSet.Contains(neighbor))
                {
                    neighbor.gCost = newCostToNeighbour;
                    neighbor.hCost = GetDistance(neighbor.position, endCell.position);
                    neighbor.parent = currentCell;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }
        return new List<Vector2Int>();
    }
    private List<Vector2Int> RetracePath(Cell endCell)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Cell currentCell = endCell;

        while (currentCell != null)
        {
            path.Add(currentCell.position);
            currentCell = currentCell.parent;
        }
        foreach (Vector2Int cell in path)
        {
            grid[cell.x, cell.y].parent = null;
            grid[cell.x, cell.y].hCost = 0;
            grid[cell.x, cell.y].gCost = 0;
        }
        path.Reverse();
        return path;
    }
    private List<Cell> GetNeighbors(Cell cell)
    {
        List<Cell> neighbors = new List<Cell>();

        // 四方向移动
        Vector2Int[] directions = {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left,
            new Vector2Int(1,1),
            new Vector2Int(1,-1),
            new Vector2Int(-1,1),
            new Vector2Int(-1,-1)
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int neighborPos = cell.position + dir;
            if (IsValidGridPosition(neighborPos))
                neighbors.Add(grid[neighborPos.x, neighborPos.y]);
        }
        return neighbors;
    }
    private int GetDistance(Vector2Int a, Vector2Int b)
    {
        // 曼哈顿距离
        int dx = Mathf.Abs(a.x - b.x);
        int dy = Mathf.Abs(a.y - b.y);
        if (dx > dy) return 14 * dy + (dx - dy) * 10;
        return 14 * dx + (dy - dx) * 10;
    }

    #endregion

    #region Grid Position Function
    public bool IsValidGridPosition(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridSize.x &&
               gridPos.y >= 0 && gridPos.y < gridSize.y;
    }
    void SetEdgeCells()
    {
        for (int i = 0; i < gridSize.x; i++)
        {
            edgeCell.Add(grid[i, 0]);
        }
        for (int i = 1; i < gridSize.y - 1; i++)
        {
            edgeCell.Add(grid[gridSize.x - 1, i]);
        }
        for (int i = gridSize.x - 2; i > 0; i--)
        {
            edgeCell.Add(grid[i, gridSize.y - 1]);
        }
        for (int i = gridSize.y - 2; i > 1; i--)
        {
            edgeCell.Add(grid[0, i]);
        }
    }
    public Vector2 GetRandomDirection(float minAngle = 0, float maxAngle = 360)
    {
        float angle = Random.Range(minAngle, maxAngle);
        return Quaternion.Euler(0, 0, angle) * Vector2.right;
    }
    public Vector2 GetPlayerAvoidancePosition(float minDistance)
    {
        Vector2 basePos = player.position;
        int attempts = 0;
        do
        {
            Vector2 offset = Random.insideUnitCircle * minDistance;
            Vector2Int gridPos = WorldToGridPosition(basePos + offset);
            if (IsValidGridPosition(gridPos))
            {
                return GridToWorldPosition(gridPos);
            }
            attempts++;
        } while (attempts < 50);
        return basePos;
    }
    public Vector2Int? GetValidSpawnPosition(
            SpawnStrategy strategy,
            int playerRadius = 0,
            Vector2? direction = null,
            int maxAttempts = 20
        )
    {
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2Int gridPos = Vector2Int.zero;

            switch (strategy)
            {
                case SpawnStrategy.RandomWalkable:
                    gridPos = new Vector2Int(
                        Random.Range(0, gridSize.x),
                        Random.Range(0, gridSize.y)
                    );
                    break;

                case SpawnStrategy.AvoidPlayerArea:
                    Vector2Int playerCell = WorldToGridPosition(player.position);
                    gridPos = new Vector2Int(
                        Random.Range(0, gridSize.x),
                        Random.Range(0, gridSize.y)
                    );
                    // 排除玩家周围区域
                    if (Vector2Int.Distance(playerCell, gridPos) < playerRadius) continue;
                    break;

                case SpawnStrategy.ScreenEdges:
                    int edge = Random.Range(0, 4);
                    switch (edge)
                    {
                        case 0: // 左边缘
                            gridPos = new Vector2Int(0, Random.Range(0, gridSize.y));
                            break;
                        case 1: // 右边缘
                            gridPos = new Vector2Int(gridSize.x - 1, Random.Range(0, gridSize.y));
                            break;
                        case 2: // 上边缘
                            gridPos = new Vector2Int(Random.Range(0, gridSize.x), gridSize.y - 1);
                            break;
                        case 3: // 下边缘
                            gridPos = new Vector2Int(Random.Range(0, gridSize.x), 0);
                            break;
                    }
                    break;

                case SpawnStrategy.RandomWithDirection:
                    Vector2 dir = direction.Value.normalized;
                    float angleVariance = 30f;
                    Vector2 variedDir = Quaternion.Euler(0, 0, Random.Range(-angleVariance, angleVariance)) * dir;
                    gridPos = WorldToGridPosition(
                        player.position + (Vector3)variedDir * gridSize.x / 2
                    );
                    break;
            }
            if (IsValidGridPosition(gridPos))
            {
                return gridPos;
            }
        }
        return null;
    }
    public List<Vector2Int> GetEdgeCells(SpawnStrategy strategy)
    {
        List<Vector2Int> cells = new List<Vector2Int>();
        foreach (var cell in GridManager.Instance.EdgeCell)
        {
            cells.Add(cell.position);
        }
        return cells;
    }
    #endregion

    void OnDrawGizmos()
    {
        if (!showGridGizmos || !Application.isPlaying) return;

        Gizmos.color = Color.cyan;
        for (int x = 0; x < gridSize.x; x++)
        {
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector3 center = new Vector3(
                    x * cellSize + gridOrigin.x /*+ cellSize / 2*/,
                    y * cellSize + gridOrigin.y /*+ cellSize / 2*/,
                    0
                );
                Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0));
            }
        }
    }
}

[System.Serializable]
public class Cell
{
    public Vector2Int position;
    public Vector2 worldPosition;
    public bool isWalkable;
    public Cell parent;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    [HideInInspector] public GameObject debugObject;

    public Cell(Vector2Int position, Vector2 worldPosition, bool isWalkable, Cell parent = null, float gCost = 0, float hCost = 0)
    {
        this.position = position;
        this.worldPosition = worldPosition;
        this.isWalkable = isWalkable;
        this.parent = parent;
        this.gCost = gCost;
        this.hCost = hCost;
    }

    public void SetWalkable(bool state)
    {
        isWalkable = state;
        if (debugObject)
            debugObject.GetComponent<SpriteRenderer>().color = state ? Color.white : Color.red;
    }
}

