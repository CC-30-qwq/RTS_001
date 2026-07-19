using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    [Header("网格设置")]
    [Tooltip("每个单元格的大小")]
    public float cellSize = 2f;
    [Tooltip("网格宽度（单元格数量）")]
    public int gridWidth = 30;
    [Tooltip("网格高度（单元格数量）")]
    public int gridHeight = 30;

    [Header("可视化设置")]
    [Tooltip("是否显示网格线")]
    public bool showGrid = true;
    [Tooltip("网格线颜色")]
    public Color gridColor = Color.white;
    [Tooltip("网格线离地面的高度（避免与地面重叠）")]
    public float lineHeight = 0.1f;

    public LayerMask groundLayer;
    public float maxTerrainHeight;
    public float maxSlope;

    public bool showGizmo;

    private bool[,] occupied;
    private Vector3[] cachedGridCenters;
    private bool[,] groundHitCache;
    private Vector3[,] hitPointCache; // 缓存击中点位置
    private bool isCacheValid = false;
    private RaycastHit[] cachedRaycastHits; // 缓存射线检测结果，避免重复分配

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        occupied = new bool[gridWidth, gridHeight];
        CreateGridVisual();
        InitializeCaches();
    }

    void CreateGridVisual()
    {
        if (!showGrid) return;

        ClearGridVisual();
        GameObject gridContainer = new GameObject("GridContainer");
        gridContainer.transform.SetParent(transform);

        Material lineMaterial = new Material(Shader.Find("Sprites/Default")) { color = gridColor };

        // 创建纵向线
        for (int x = 0; x <= gridWidth; x++)
        {
            CreateGridLine(gridContainer, lineMaterial, x * cellSize, 0, x * cellSize, gridHeight * cellSize);
        }

        // 创建横向线
        for (int y = 0; y <= gridHeight; y++)
        {
            CreateGridLine(gridContainer, lineMaterial, 0, y * cellSize, gridWidth * cellSize, y * cellSize);
        }
    }

    void ClearGridVisual()
    {
        Transform gridContainer = transform.Find("GridContainer");
        if (gridContainer != null)
        {
            Destroy(gridContainer.gameObject);
        }
    }

    void CreateGridLine(GameObject parent, Material material, float x1, float z1, float x2, float z2)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(parent.transform);

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.startWidth = lr.endWidth = 0.05f;
        lr.material = material;
        lr.loop = false;
        lr.positionCount = 2;
        lr.SetPositions(new Vector3[] {
            new Vector3(x1, lineHeight, z1),
            new Vector3(x2, lineHeight, z2)
        });
    }

    // 世界坐标 -> 网格坐标（整数索引）
    public Vector2Int WorldToGrid(Vector3 worldPos) =>
        new Vector2Int(Mathf.FloorToInt(worldPos.x / cellSize), Mathf.FloorToInt(worldPos.z / cellSize));

    // 网格坐标 -> 世界坐标（单元格中心）
    public Vector3 GridToWorld(Vector2Int gridPos) =>
        new Vector3(gridPos.x * cellSize + cellSize * 0.5f, 0, gridPos.y * cellSize + cellSize * 0.5f);

    // 检查单元格是否可用（边界内且未被占用）
    public bool IsCellAvailable(Vector2Int gridPos) =>
        (uint)gridPos.x < (uint)gridWidth && (uint)gridPos.y < (uint)gridHeight && !occupied[gridPos.x, gridPos.y];

    // 检查多格区域是否全部可用
    public bool IsAreaAvailable(Vector2Int gridPos, int width, int height)
    {
        int startX = gridPos.x - width / 2;
        int startY = gridPos.y - height / 2;
        int endX = startX + width;
        int endY = startY + height;

        if (startX < 0 || startY < 0 || endX > gridWidth || endY > gridHeight)
            return false;

        // 检查坡度
        float minHeight = float.MaxValue;
        float maxHeight = float.MinValue;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                if (occupied[x, y])
                    return false;

                // 获取击中点高度
                Vector3 hitPoint = GetHitPoint(x, y);
                if (hitPoint != Vector3.zero)
                {
                    float pointHeight = hitPoint.y;
                    if (pointHeight < minHeight) minHeight = pointHeight;
                    if (pointHeight > maxHeight) maxHeight = pointHeight;
                }
                else
                {
                    // 如果没有击中地面，认为不可用
                    return false;
                }
            }
        }

        // 检查坡度是否超过最大允许值
        if (maxHeight - minHeight > maxSlope)
            return false;

        return true;
    }

    // 占用/释放单个单元格
    public void SetCellOccupied(Vector2Int gridPos, bool state)
    {
        if ((uint)gridPos.x < (uint)gridWidth && (uint)gridPos.y < (uint)gridHeight)
            occupied[gridPos.x, gridPos.y] = state;
    }

    // 占用/释放多格区域
    public void SetAreaOccupied(Vector2Int gridPos, int width, int height, bool state)
    {
        int startX = gridPos.x - width / 2;
        int startY = gridPos.y - height / 2;
        int endX = startX + width;
        int endY = startY + height;

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                if ((uint)x < (uint)gridWidth && (uint)y < (uint)gridHeight)
                    occupied[x, y] = state;
            }
        }
    }

    #region 地形检测
    // 初始化缓存
    private void InitializeCaches()
    {
        cachedGridCenters = new Vector3[gridWidth * gridHeight];
        groundHitCache = new bool[gridWidth, gridHeight];
        hitPointCache = new Vector3[gridWidth, gridHeight];
        cachedRaycastHits = new RaycastHit[gridWidth * gridHeight]; // 预分配射线检测结果数组
        isCacheValid = false;

        int index = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                cachedGridCenters[index] = GridToWorld(new Vector2Int(x, y));

                // 缓存击中点位置
                Vector3 center = cachedGridCenters[index];
                if (Physics.Raycast(center + new Vector3(0, maxTerrainHeight, 0), Vector3.down, out RaycastHit hit, maxTerrainHeight, groundLayer))
                {
                    groundHitCache[x, y] = true;
                    hitPointCache[x, y] = hit.point;
                    cachedRaycastHits[index] = hit; // 缓存射线检测结果
                }
                else
                {
                    groundHitCache[x, y] = false;
                    hitPointCache[x, y] = Vector3.zero; // 未击中时设为零向量
                    cachedRaycastHits[index] = new RaycastHit(); // 空结果
                }

                index++;
            }
        }
        isCacheValid = true;
    }

    // 获取所有网格中心位置（使用缓存）
    public Vector3[] GetAllGridCenters() => cachedGridCenters;

    // 获取网格中心位置（单个）
    public Vector3 GetGridCenter(int x, int y)
    {
        if (cachedGridCenters == null || (uint)x >= (uint)gridWidth || (uint)y >= (uint)gridHeight)
            return Vector3.zero;

        int index = x * gridHeight + y;
        return index < cachedGridCenters.Length ? cachedGridCenters[index] : Vector3.zero;
    }

    // 检查地面是否被击中（使用缓存）
    public bool IsGroundHit(int x, int y)
    {
        if (!isCacheValid)
            UpdateGroundHitCache();

        return (uint)x < (uint)gridWidth && (uint)y < (uint)gridHeight ? groundHitCache[x, y] : false;
    }

    // 更新地面检测缓存
    private void UpdateGroundHitCache()
    {
        if (groundHitCache == null)
            groundHitCache = new bool[gridWidth, gridHeight];

        if (hitPointCache == null)
            hitPointCache = new Vector3[gridWidth, gridHeight];

        if (cachedRaycastHits == null)
            cachedRaycastHits = new RaycastHit[gridWidth * gridHeight];

        int index = 0;
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 center = GetGridCenter(x, y);
                if (Physics.Raycast(center + new Vector3(0, maxTerrainHeight, 0), Vector3.down, out RaycastHit hit, groundLayer))
                {
                    groundHitCache[x, y] = true;
                    hitPointCache[x, y] = hit.point;
                    cachedRaycastHits[index] = hit; // 更新缓存的射线检测结果
                }
                else
                {
                    groundHitCache[x, y] = false;
                    hitPointCache[x, y] = Vector3.zero;
                    cachedRaycastHits[index] = new RaycastHit(); // 空结果
                }
                index++;
            }
        }
        isCacheValid = true;
    }

    // 使缓存失效（当需要重新计算时调用）
    public void InvalidateCache() => isCacheValid = false;

    // 获取击中点位置（单个网格）
    public Vector3 GetHitPoint(int x, int y)
    {
        if (!isCacheValid)
            UpdateGroundHitCache();

        if ((uint)x < (uint)gridWidth && (uint)y < (uint)gridHeight)
        {
            return hitPointCache[x, y];
        }
        return Vector3.zero;
    }

    // 获取击中点位置（网格坐标）
    public Vector3 GetHitPoint(Vector2Int gridPos) => GetHitPoint(gridPos.x, gridPos.y);

    // 获取击中点位置（世界坐标）
    public Vector3 GetHitPoint(Vector3 worldPos)
    {
        Vector2Int gridPos = WorldToGrid(worldPos);
        return GetHitPoint(gridPos);
    }

    // 检查网格是否被击中并获取击中点
    public bool TryGetHitPoint(int x, int y, out Vector3 hitPoint)
    {
        if (!isCacheValid)
            UpdateGroundHitCache();

        if ((uint)x < (uint)gridWidth && (uint)y < (uint)gridHeight && groundHitCache[x, y])
        {
            hitPoint = hitPointCache[x, y];
            return true;
        }
        hitPoint = Vector3.zero;
        return false;
    }
    #endregion

    void OnDrawGizmosSelected()
    {
        if (showGizmo)
        {
            // 确保缓存在编辑器中也被初始化
            if (cachedGridCenters == null)
                InitializeCaches();

            // 确保地面检测缓存已初始化
            if (groundHitCache == null)
                groundHitCache = new bool[gridWidth, gridHeight];

            // 只在选中对象时才进行昂贵的计算
            if (!isCacheValid)
                UpdateGroundHitCache();

            // 使用缓存的网格中心和地面检测结果
            int index = 0;
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // 确保索引在有效范围内
                    if (index >= cachedGridCenters.Length)
                        return;

                    Vector3 center = cachedGridCenters[index];
                    bool isHit = groundHitCache[x, y];

                    Gizmos.color = isHit ? Color.green : Color.yellow;
                    Gizmos.DrawLine(center + new Vector3(0, maxTerrainHeight, 0), center);

                    index++;
                }
            }
        }
    }
}
