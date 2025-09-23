using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Grid ��ǥ�� �ð������� ǥ���ϴ� �ý���
/// Unity Editor������ �۵��ϸ�, ���ڿ� ��ǥ�� Scene �信 �׷��ݴϴ�
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
    /// ī�޶� Grid�� ���� �ڵ� ����
    /// </summary>
    public void SyncCameraToGrid()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // �׸��� �߾� ��ġ ���
        Vector3 gridCenter = new Vector3(
            gridOrigin.x + (gridWidth * cellSize) / 2f - cellSize / 2f,
            gridOrigin.y + (gridHeight * cellSize) / 2f - cellSize / 2f,
            mainCamera.transform.position.z
        );

        // ī�޶� ��ġ�� �׸��� �߾�����
        mainCamera.transform.position = gridCenter;

        // ī�޶� ũ�⸦ �׸��忡 ���� ���� (���� ����)
        float gridAspect = (float)gridWidth / gridHeight;
        float cameraAspect = mainCamera.aspect;

        if (gridAspect > cameraAspect)
        {
            // �׸��尡 �� ���� - ���� �������� ����
            mainCamera.orthographicSize = (gridWidth * cellSize) / (2f * cameraAspect) + 1f;
        }
        else
        {
            // �׸��尡 �� ���� - ���� �������� ����  
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
    /// �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ
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
    /// Ư�� �׸��� ��ǥ�� ��ȿ�� ���� ���� �ִ��� Ȯ��
    /// </summary>
    public bool IsValidGridPosition(Vector3Int gridPosition)
    {
        return gridPosition.x >= 0 && gridPosition.x < gridWidth &&
               gridPosition.y >= 0 && gridPosition.y < gridHeight;
    }

    /// <summary>
    /// �׸��� ���� ���� ��� ��ȿ�� ��ǥ�� ��ȯ
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

        // �׸��� ���� ���� ����
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
        // ���μ� �׸���
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * cellSize, 0, 0) + gridOrigin;
            Vector3 end = new Vector3(x * cellSize, gridHeight * cellSize, 0) + gridOrigin;
            Gizmos.DrawLine(start, end);
        }

        // ���μ� �׸���
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
        style.fontSize = Mathf.RoundToInt(textSize * 100); // ũ�� ����
        style.alignment = TextAnchor.MiddleCenter;
        style.fontStyle = FontStyle.Bold; // �������� ���� ����ü

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (showOnlyIntersections)
                {
                    Vector3 worldPos = GridToWorldPosition(new Vector3Int(x, y, 0));

                    // Scene �信���� ���̵���
                    if (SceneView.currentDrawingSceneView != null)
                    {
                        // �ؽ�Ʈ ũ�⿡ ���� ��ġ ����
                        Vector3 textPos = worldPos;
                        Handles.Label(textPos, $"{x},{y}", style);
                    }
                }
            }
        }
    }

    // �����Ϳ��� �� ���� �� �� �� ������Ʈ
    void OnValidate()
    {
        if (gridWidth < 1) gridWidth = 1;
        if (gridHeight < 1) gridHeight = 1;
        if (cellSize <= 0) cellSize = 1f;
        if (textSize <= 0) textSize = 0.1f;
        if (lineAlpha < 0) lineAlpha = 0f;
        if (lineAlpha > 1) lineAlpha = 1f;

        // �� ���� �� ī�޶� �絿��ȭ
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
        // ���� ���õ� LevelData���� ũ�� ��������
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

    // ��Ÿ�ӿ��� ����� ���� Ȯ�ο�
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void LogGridInfo()
    {
        Debug.Log($"Grid Info - Width: {gridWidth}, Height: {gridHeight}, Cell Size: {cellSize}");
        Debug.Log($"Grid Origin: {gridOrigin}");
        Debug.Log($"Total Grid Cells: {gridWidth * gridHeight}");
    }

    /// <summary>
    /// ������ ����: Ư�� ��ġ�� ����� �׸���
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