using UnityEngine;

public class BuildingPlacement : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("所有可建造的建筑（从Inspector拖入）")]
    public BuildingData[] availableBuildings;
    [Tooltip("地面层，用于射线检测")]
    public LayerMask groundLayer;
    [Tooltip("可放置时的预览材质")]
    public Material previewValidMaterial;
    [Tooltip("不可放置时的预览材质")]
    public Material previewInvalidMaterial;

    public bool showGrid;

    private int selectedBuildingIndex = -1;
    private GameObject previewInstance;
    private Renderer[] previewRenderers;
    private Material[] originalMaterials; // 保存原始材质，用于恢复
    private bool canPlace;
    private BuildingData currentBuildingData; // 缓存当前建筑数据，避免重复访问数组
    private Vector2Int lastGridPos = new Vector2Int(-999, -999); // 缓存上一次的网格位置，避免重复计算

    void Update()
    {
        if (selectedBuildingIndex < 0 || previewInstance == null) return;

        UpdatePreviewPosition();
        HandleInput();
    }

    // 由UI按钮调用
    public void SelectBuilding(int index)
    {
        if (index < 0 || index >= availableBuildings.Length) return;
        selectedBuildingIndex = index;
        currentBuildingData = availableBuildings[selectedBuildingIndex];
        CreatePreview();
    }

    void CreatePreview()
    {
        if (previewInstance != null) Destroy(previewInstance);

        previewInstance = Instantiate(currentBuildingData.prefab);
        previewInstance.layer = LayerMask.NameToLayer("Ignore Raycast");

        previewRenderers = previewInstance.GetComponentsInChildren<Renderer>();
        // 保存原始材质
        originalMaterials = new Material[previewRenderers.Length];
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            originalMaterials[i] = previewRenderers[i].material;
        }

        // 设置为半透明预览模式
        SetPreviewMaterial(previewValidMaterial);
    }

    void UpdatePreviewPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 1000, groundLayer))
        {
            Vector3 worldPos = hit.point;
            Vector2Int gridPos = GridManager.Instance.WorldToGrid(worldPos);

            // 优化：仅当网格位置改变时才重新计算
            if (gridPos != lastGridPos)
            {
                lastGridPos = gridPos;
                // 检查区域是否可用
                canPlace = GridManager.Instance.IsAreaAvailable(gridPos, currentBuildingData.width, currentBuildingData.height);

                int startX = gridPos.x - currentBuildingData.width / 2;
                int startY = gridPos.y - currentBuildingData.height / 2;

                // 计算建筑占据区域的世界中心
                float centerX = startX * GridManager.Instance.cellSize + currentBuildingData.width * GridManager.Instance.cellSize * 0.5f;
                float centerZ = startY * GridManager.Instance.cellSize + currentBuildingData.height * GridManager.Instance.cellSize * 0.5f;
                Vector3 buildingCenter = new Vector3(centerX, GridManager.Instance.GetHitPoint(gridPos).y, centerZ);
                previewInstance.transform.position = buildingCenter;

                // 更新网格高亮
                UpdateGridHighlight(gridPos);

                // 根据合法性改变预览颜色
                SetPreviewMaterial(canPlace ? previewValidMaterial : previewInvalidMaterial);
            }
        }
        else
        {
            // 鼠标不在地面上时清除高亮
            ClearGridHighlight();
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0) && canPlace)
        {
            PlaceBuilding();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            CancelPlacement();
        }
    }

    void PlaceBuilding()
    {
        // 优化：直接使用缓存的网格位置，避免重复计算
        Vector2Int gridPos = lastGridPos;

        // 占用网格
        GridManager.Instance.SetAreaOccupied(gridPos, currentBuildingData.width, currentBuildingData.height, true);

        // 将预览对象转为真实建筑
        GameObject realBuilding = Instantiate(currentBuildingData.prefab, previewInstance.transform.position, Quaternion.identity);
        // 可添加建筑完成逻辑（如播放音效、添加组件等）

        // 退出放置模式
        CancelPlacement();
    }

    public void CancelPlacement()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
            previewInstance = null;
        }
        // 清除网格高亮
        ClearGridHighlight();
        selectedBuildingIndex = -1;
        currentBuildingData = null;
        lastGridPos = new Vector2Int(-999, -999); // 重置缓存位置
    }

    void SetPreviewMaterial(Material mat)
    {
        if (previewRenderers == null) return;
        for (int i = 0; i < previewRenderers.Length; i++)
        {
            // 优化：只在材质不同时才设置，避免不必要的GPU调用
            if (previewRenderers[i].material != mat)
                previewRenderers[i].material = mat;
        }
    }

    // 添加网格高亮功能
    private GameObject gridHighlight;
    private MeshRenderer highlightRenderer;
    private MeshFilter highlightMeshFilter;
    private Mesh highlightMesh;

    void CreateGridHighlight()
    {
        if (gridHighlight != null) return;

        gridHighlight = new GameObject("GridHighlight");
        gridHighlight.transform.SetParent(transform);
        
        // 添加MeshRenderer
        highlightRenderer = gridHighlight.AddComponent<MeshRenderer>();
        highlightRenderer.material = new Material(Shader.Find("Sprites/Default"));
        
        // 添加MeshFilter
        highlightMeshFilter = gridHighlight.AddComponent<MeshFilter>();
        highlightMesh = new Mesh();
        highlightMeshFilter.mesh = highlightMesh;
    }

    void UpdateGridHighlight(Vector2Int gridPos)
    {
        if (!showGrid) return; // 如果不显示网格，也不显示高亮

        CreateGridHighlight();

        // 计算建筑区域的四个角点
        float centerX = gridPos.x * GridManager.Instance.cellSize;
        float centerZ = gridPos.y * GridManager.Instance.cellSize;
        float startX = centerX - currentBuildingData.width / 2 * GridManager.Instance.cellSize;
        float startZ = centerZ - currentBuildingData.height / 2 * GridManager.Instance.cellSize;
        float endX = startX + currentBuildingData.width * GridManager.Instance.cellSize;
        float endZ = startZ + currentBuildingData.height * GridManager.Instance.cellSize;

        // 设置四个顶点
        float highlightHeight = GridManager.Instance.GetHitPoint(gridPos).y + 0.05f;
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(startX, highlightHeight, startZ);
        vertices[1] = new Vector3(endX, highlightHeight, startZ);
        vertices[2] = new Vector3(endX, highlightHeight, endZ);
        vertices[3] = new Vector3(startX, highlightHeight, endZ);

        // 设置三角形索引（两个三角形组成一个四边形）
        int[] triangles = new int[6] { 0, 1, 2, 0, 2, 3 };

        // 设置UV坐标
        Vector2[] uvs = new Vector2[4];
        uvs[0] = new Vector2(0, 0);
        uvs[1] = new Vector2(1, 0);
        uvs[2] = new Vector2(1, 1);
        uvs[3] = new Vector2(0, 1);

        // 更新网格
        highlightMesh.Clear();
        highlightMesh.vertices = vertices;
        highlightMesh.triangles = triangles;
        highlightMesh.uv = uvs;
        highlightMesh.RecalculateNormals();

        // 设置材质颜色
        highlightRenderer.material.color = canPlace ? new Color(0, 1, 0, 0.5f) : new Color(1, 0, 0, 0.5f);
    }

    void ClearGridHighlight()
    {
        if (gridHighlight != null)
        {
            Destroy(gridHighlight);
            gridHighlight = null;
        }
    }
}
