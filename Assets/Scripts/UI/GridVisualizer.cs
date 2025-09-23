using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Grid 좌표를 시각적으로 표시하는 시스템
/// Unity Editor에서만 작동하며, 격자와 좌표를 Scene 뷰에 그려줍니다
/// </summary>
public class GridVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float cellSize = 1f;
    [SerializeField] private Vector3 gridOrigin = Vector3.zero;

    [Header("Visual Settings")]
    [SerializeField] private Color gridLineColor = Color.white;
    [SerializeField] private Color coordinateTextColor = Color.yellow;
    [SerializeField] private bool showCoordinates = true;
    [SerializeField] private bool showGrid = true;
    [SerializeField] private float lineAlpha = 0.5f;

    [Header("Coordinate Settings")]
    [SerializeField] private bool showOnlyIntersections = true;
    [SerializeField] private float textSize = 0.15f;
    [SerializeField] private bool syncWithCamera = true;

    void Start()
    {
        if (syncWithCamera)
        {
            SyncCameraToGrid();
        }
    }

    /// <summary>
    /// 카메라를 Grid에 맞춰 자동 조정
    /// </summary>
    public void SyncCameraToGrid()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // 그리드 중앙 위치 계산
        Vector3 gridCenter = new Vector3(
            gridOrigin.x + (gridWidth * cellSize) / 2f - cellSize / 2f,
            gridOrigin.y + (gridHeight * cellSize) / 2f - cellSize / 2f,
            mainCamera.transform.position.z
        );

        // 카메라 위치를 그리드 중앙으로
        mainCamera.transform.position = gridCenter;

        // 카메라 크기를 그리드에 맞춰 조정 (여백 포함)
        float gridAspect = (float)gridWidth / gridHeight;
        float cameraAspect = mainCamera.aspect;

        if (gridAspect > cameraAspect)
        {
            // 그리드가 더 넓음 - 가로 기준으로 맞춤
            mainCamera.orthographicSize = (gridWidth * cellSize) / (2f * cameraAspect) + 1f;
        }
        else
        {
            // 그리드가 더 높음 - 세로 기준으로 맞춤  
            mainCamera.orthographicSize = (gridHeight * cellSize) / 2f + 1f;
        }

        Debug.Log($"Camera synced to Grid - Position: {gridCenter}, Size: {mainCamera.orthographicSize}");
    }
    public Vector3Int WorldToGridPosition(Vector3 worldPosition)
    {
        Vector3 localPos = worldPosition - gridOrigin;
        return new Vector3Int(
            Mathf.RoundToInt(localPos.x / cellSize),
            Mathf.RoundToInt(localPos.y / cellSize),
            0
        );
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환
    /// </summary>
    public Vector3 GridToWorldPosition(Vector3Int gridPosition)
    {
        return new Vector3(
    gridPosition.x * cellSize + cellSize * 0.5f,
    gridPosition.y * cellSize + cellSize * 0.5f,
    0
) + gridOrigin;
    }

    /// <summary>
    /// 특정 그리드 좌표가 유효한 범위 내에 있는지 확인
    /// </summary>
    public bool IsValidGridPosition(Vector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    /// <summary>
    /// 그리드 범위 내의 모든 유효한 좌표를 반환
    /// </summary>
    public Vector3Int[] GetAllGridPositions()
    {
        var positions = new Vector3Int[gridWidth * gridHeight];
        int index = 0;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                positions[index] = new Vector3Int(x, y, 0);
                index++;
            }
        }

        return positions;
    }

    [ContextMenu("Sync to Scene Grid")]
    public void SyncToSceneGrid()
    {
        Grid sceneGrid = FindFirstObjectByType<Grid>();
        if (sceneGrid != null)
        {
            cellSize = sceneGrid.cellSize.x;
            gridOrigin = sceneGrid.transform.position;
            Debug.Log($"Synced to Scene Grid - Cell Size: {cellSize}, Origin: {gridOrigin}");
        }
    }
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!showGrid && !showCoordinates) return;

        // 그리드 라인 색상 설정
        Color originalColor = Gizmos.color;
        Color lineColor = gridLineColor;
        lineColor.a = lineAlpha;
        Gizmos.color = lineColor;

        if (showGrid)
        {
            DrawGridLines();
        }

        if (showCoordinates)
        {
            DrawCoordinates();
        }

        Gizmos.color = originalColor;
    }

    private void DrawGridLines()
    {
        // 세로선 그리기
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0) + gridOrigin;
            Vector3 end = new Vector3(x * cellSize, gridHeight * cellSize, 0) + gridOrigin;
            Gizmos.DrawLine(start, end);
        }

        // 가로선 그리기
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = new Vector3(0, y * cellSize, 0) + gridOrigin;
            Vector3 end = new Vector3(gridWidth * cellSize, y * cellSize, 0) + gridOrigin;
            Gizmos.DrawLine(start, end);
        }
    }

    private void DrawCoordinates()
    {
        GUIStyle style = new GUIStyle();
        style.normal.textColor = coordinateTextColor;
        style.fontSize = Mathf.RoundToInt(textSize * 100); // 크기 조정
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold; // 가독성을 위해 볼드체

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (showOnlyIntersections)
                {
                    Vector3 worldPos = GridToWorldPosition(new Vector3Int(x, y, 0));

                    // Scene 뷰에서만 보이도록
                    if (SceneView.currentDrawingSceneView != null)
                    {
                        // 텍스트 크기에 맞춰 위치 조정
                        Vector3 textPos = worldPos;
                        Handles.Label(textPos, $"{x},{y}", style);
                    }
                }
            }
        }
    }

    // 에디터에서 값 변경 시 씬 뷰 업데이트
    void OnValidate()
    {
        if (gridWidth < 1) gridWidth = 1;
        if (gridHeight < 1) gridHeight = 1;
        if (cellSize <= 0) cellSize = 1f;
        if (textSize <= 0) textSize = 0.1f;
        if (lineAlpha < 0) lineAlpha = 0f;
        if (lineAlpha > 1) lineAlpha = 1f;

        // 값 변경 시 카메라 재동기화
        if (syncWithCamera && Application.isPlaying)
        {
            SyncCameraToGrid();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Sync Camera to Grid")]
    public void SyncCameraToGridEditor()
    {
        SyncCameraToGrid();
    }

    [ContextMenu("Set Grid Size to Level Data")]
    public void SetGridSizeToLevelData()
    {
        // 현재 선택된 LevelData에서 크기 가져오기
        if (Selection.activeObject is LevelData levelData)
        {
            gridWidth = levelData.gridWidth;
            gridHeight = levelData.gridHeight;
            cellSize = levelData.cellSize;
            gridOrigin = levelData.gridOrigin;

            if (syncWithCamera)
            {
                SyncCameraToGrid();
            }

            Debug.Log($"Grid size set from LevelData: {gridWidth}x{gridHeight}");
        }
        else
        {
            Debug.LogWarning("Please select a LevelData asset first!");
        }
    }
#endif
#endif

    // 런타임에서 디버그 정보 확인용
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogGridInfo()
    {
        Debug.Log($"Grid Info - Width: {gridWidth}, Height: {gridHeight}, Cell Size: {cellSize}");
        Debug.Log($"Grid Origin: {gridOrigin}");
        Debug.Log($"Total Grid Cells: {gridWidth * gridHeight}");
    }

    /// <summary>
    /// 에디터 전용: 특정 위치에 기즈모 그리기
    /// </summary>
    public void DrawPositionGizmo(Vector3Int gridPos, Color color)
    {
#if UNITY_EDITOR
        Vector3 worldPos = GridToWorldPosition(gridPos);
        Gizmos.color = color;
        Gizmos.DrawWireCube(worldPos, Vector3.one * cellSize * 0.8f);
#endif
    }
}