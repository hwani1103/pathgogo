using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ������ ��� ������ �����ϴ� �ý���
/// ���ο� ��Ű��ó: PathValidator, CollisionDetector, MovementSimulator�� ����
/// </summary>
public class PathSelectionManager : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Material pathLineMaterial;
    [SerializeField] private Color availablePathColor = Color.yellow;
    [SerializeField] private Color selectedPathColor = Color.green;
    [SerializeField] private Color invalidPathColor = Color.red;
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Flag Settings")]
    [SerializeField] private GameObject flagPrefab;
    [SerializeField] private float flagScale = 0.5f;

    [Header("Collision Feedback")]
    [SerializeField] private bool showCollisionWarnings = true;
    [SerializeField] private Color collisionWarningColor = Color.red;

    // ���� ���õ� ĳ����
    private GamePiece currentCharacter;

    // ��� ����
    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private List<GameObject> pathFlags = new List<GameObject>();
    private List<LineRenderer> availablePathLines = new List<LineRenderer>();
    private LineRenderer finalPathLine;

    // ���ο� ��Ű��ó ����
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;

    // ���� ����
    private GridVisualizer gridVisualizer;
    private LevelLoader levelLoader;
    private TouchInputManager touchInputManager;

    void Awake()
    {
        // ���� ����
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        levelLoader = FindFirstObjectByType<LevelLoader>();
        touchInputManager = FindFirstObjectByType<TouchInputManager>();

        // �� �ý��� ���� (���� GameObject�� �ִٰ� ����)
        pathValidator = GetComponent<PathValidator>();
        collisionDetector = GetComponent<CollisionDetector>();
        movementSimulator = GetComponent<MovementSimulator>();

        // ������ �ڵ� ����
        if (pathValidator == null)
            pathValidator = gameObject.AddComponent<PathValidator>();
        if (collisionDetector == null)
            collisionDetector = gameObject.AddComponent<CollisionDetector>();
        if (movementSimulator == null)
            movementSimulator = gameObject.AddComponent<MovementSimulator>();

        // ���� ��� Line Renderer ����
        CreateFinalPathLineRenderer();
    }

    /// <summary>
    /// ĳ���� ���� �� ��� ���� ��� ����
    /// </summary>
    public void StartPathSelection(GamePiece character)
    {
        if (character == null) return;

        currentCharacter = character;
        currentPath.Clear();

        // ĳ���� ���� ��ġ�� ��ο� �߰�
        currentPath.Add(character.GetCurrentGridPosition());

        // ������ ��ε� ǥ��
        ShowAvailablePaths();
    }

    /// <summary>
    /// ��� ���� ��� ����
    /// </summary>
    public void EndPathSelection()
    {
        currentCharacter = null;
        currentPath.Clear();

        ClearAllVisuals();
    }

    /// <summary>
    /// Ư�� ��ġ ���� ó�� (��ǥ ��ȯ ������)
    /// </summary>
    public void SelectPosition(Vector3Int gridPosition)
    {
        if (currentCharacter == null) return;

        Vector3Int currentPos = currentPath.Count > 0 ? currentPath[currentPath.Count - 1] : currentCharacter.GetCurrentGridPosition();

        // ���� ������ ��ġ���� Ȯ��
        if (!IsValidSelection(currentPos, gridPosition))
        {
            ShowInvalidSelectionFeedback(gridPosition);
            return;
        }

        // ��ο� �߰�
        currentPath.Add(gridPosition);

        // Flag ����
        CreateFlag(gridPosition);

        // ���� ���� Ƚ�� Ȯ��
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        if (remainingSelections <= 0 || pathValidator.IsGoalPosition(gridPosition, currentCharacter))
        {
            // ��� �ϼ�
            CompletePath();
        }
        else
        {
            // ���� ������ ���� ������ ��� ������Ʈ
            ShowAvailablePaths();
        }
    }

    /// <summary>
    /// ��ȿ�� �������� Ȯ�� (PathValidator ���)
    /// </summary>
    private bool IsValidSelection(Vector3Int fromPos, Vector3Int toPos)
    {
        // �⺻ ����
        if (fromPos.x != toPos.x && fromPos.y != toPos.y) return false;
        if (fromPos == toPos) return false;

        // ��� ��ȿ�� �˻�
        if (!pathValidator.IsValidPosition(toPos) ||
            pathValidator.HasObstacleInPath(fromPos, toPos, currentCharacter))
            return false;

        int remaining = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        // ������ ������ ���� �ݵ�� Goal�̾�� ��
        if (remaining == 1)
        {
            return pathValidator.IsGoalPosition(toPos, currentCharacter);
        }

        // ������-1 ������ ���� Goal�� ������ �־�� �� (�߰� �ʿ�)
        if (remaining == 2)
        {
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);
            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                // Goal�� ������ �ְ� ��ο� ��ֹ��� ������ ���
                if ((toPos.x == goalPos.x || toPos.y == goalPos.y) &&
                    !pathValidator.HasObstacleInPath(toPos, goalPos, currentCharacter))
                {
                    return true;
                }
            }
            return false; // � Goal���� ������ ���� ������ �ź�
        }

        return true;
    }

    /// <summary>
    /// ���� ��ġ���� ������ ��� ��� ǥ�� (PathValidator ���)
    /// </summary>
    private void ShowAvailablePaths()
    {
        // ���� ���ε� ����
        ClearAvailablePathLines();

        if (currentCharacter == null || currentPath.Count == 0) return;

        Vector3Int currentPos = currentPath[currentPath.Count - 1];
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        if (remainingSelections <= 0) return;

        // �������� ���� Ȯ��
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (Vector3Int direction in directions)
        {
            ShowDirectionPath(currentPos, direction, remainingSelections);
        }
    }

    /// <summary>
    /// Ư�� �������� ������ ��� ǥ�� (PathValidator ���)
    /// </summary>
    private void ShowDirectionPath(Vector3Int startPos, Vector3Int direction, int remainingSelections)
    {
        Debug.Log($"ShowDirectionPath - startPos: {startPos}, direction: {direction}, remainingSelections: {remainingSelections}");

        // PathValidator�� ����Ͽ� �ش� ������ ��ȿ�� ��ġ�� ��������
        List<Vector3Int> pathPositions = pathValidator.GetValidPositionsInDirection(
    startPos, direction, currentCharacter);

        Debug.Log($"Before filtering - pathPositions count: {pathPositions.Count}");

        // ������ ���� (remainingSelections == 1): Goal�� ǥ��
        if (remainingSelections == 1)
        {
            Debug.Log("Applying FINAL selection filter - Only goals should be shown");
            // Goal ��ġ�� ���͸�
            List<Vector3Int> goalOnlyPositions = new List<Vector3Int>();
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);

            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                if (pathPositions.Contains(goalPos))
                {
                    goalOnlyPositions.Add(goalPos);
                }
            }
            pathPositions = goalOnlyPositions;
            Debug.Log($"Final selection - only goals: {pathPositions.Count} positions");
        }
        // ������-1 ���� (remainingSelections == 2): Goal�� �������� ����
        else if (remainingSelections == 2)
        {
            Debug.Log("Applying LAST-1 selection filter - Only positions in line with goals");
            pathPositions = pathValidator.FilterLastSelectionPositions(pathPositions, currentCharacter);
            Debug.Log($"Last-1 selection filtered: {pathPositions.Count} positions");
        }

        if (pathPositions.Count > 0)
        {
            CreateAvailablePathLine(startPos, pathPositions);
        }
        else
        {
            Debug.Log($"No valid positions after filtering for direction {direction}");
        }
    }

    /// <summary>
    /// �ϼ��� ����� �浹 �˻�
    /// </summary>
    private bool ValidateCompletePathWithCollision()
    {
        if (currentPath.Count < 2 || collisionDetector == null) return true;

        // ��� ��ü�� ��ȿ�� �˻�
        if (!pathValidator.ValidateCompletePath(currentPath, currentCharacter))
            return false;

        // �浹 �˻� ����
        var collisionResult = collisionDetector.CheckSingleCharacterPath(
            currentCharacter.GetCharacterId(),
            currentPath
        );

        if (collisionResult.hasCollision)
        {
            Debug.LogWarning($"Collision detected: {collisionDetector.GetCollisionMessage(collisionResult)}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// �߸��� ���ÿ� ���� �ǵ�� ǥ��
    /// </summary>
    private void ShowInvalidSelectionFeedback(Vector3Int gridPosition)
    {
        // ���� �Ǵ� ���� ȿ�� (�����)
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }

        // �ð��� �ǵ��
        StartCoroutine(ShowInvalidPositionEffect(gridPosition));

        Debug.Log($"Invalid selection at {gridPosition}");
    }

    /// <summary>
    /// �浹 ��� ǥ��
    /// </summary>
    private void ShowCollisionWarning()
    {
        if (!showCollisionWarnings) return;

        // UI �޽����� �ð��� �ǵ�� ǥ��
        Debug.LogWarning("Path would cause collision with other characters!");

        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
    }

    /// <summary>
    /// �߸��� ��ġ ���� �� �ð� ȿ��
    /// </summary>
    private System.Collections.IEnumerator ShowInvalidPositionEffect(Vector3Int gridPosition)
    {
        if (gridVisualizer == null) yield break;

        // ������ �ӽ� ��Ŀ ����
        GameObject tempMarker = new GameObject("InvalidMarker");
        tempMarker.transform.position = gridVisualizer.GridToWorldPosition(gridPosition);

        var renderer = tempMarker.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSimpleSquareSprite();
        renderer.color = invalidPathColor;
        renderer.sortingOrder = 25;

        // ũ�� �ִϸ��̼�
        Vector3 originalScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * 1.2f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            tempMarker.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            renderer.color = Color.Lerp(invalidPathColor, Color.clear, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(tempMarker);
    }

    /// <summary>
    /// ������ ���簢�� ��������Ʈ ���� (�ӽ� ��Ŀ��)
    /// </summary>
    private Sprite CreateSimpleSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
    }

    /// <summary>
    /// ������ ��� Line Renderer ���� (���� �ڵ� �����ϵ� ���� ���� ����)
    /// </summary>
    private void CreateAvailablePathLine(Vector3Int startPos, List<Vector3Int> pathPositions)
    {
        GameObject lineObj = new GameObject("AvailablePath");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.material = pathLineMaterial;
        line.startColor = availablePathColor;
        line.endColor = availablePathColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = pathPositions.Count + 1;
        line.sortingOrder = 15;
        line.useWorldSpace = true;

        // ������ ����
        Vector3 startWorldPos = gridVisualizer.GridToWorldPosition(startPos);
        line.SetPosition(0, startWorldPos);

        // ��� ���� ����
        for (int i = 0; i < pathPositions.Count; i++)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(pathPositions[i]);
            line.SetPosition(i + 1, worldPos);
        }

        availablePathLines.Add(line);
    }

    /// <summary>
    /// ��� �ϼ� ó��
    /// </summary>
    private void CompletePath()
    {
        if (currentCharacter == null) return;

        // ������ ��� ���ε� �����
        ClearAvailablePathLines();

        // ���� ��� ǥ��
        ShowFinalPath();

        // ��� ���� (���� �̵���)
        SaveCompletedPath();

        // ĳ���͸� �Ϸ� ���·� ����
        SetCharacterCompleted(currentCharacter);

        // 1�� �� ���� �� ����
        Invoke(nameof(CompleteCleanup), 1f);

        pathValidator.DebugLogPath(currentPath, $"Completed path for {currentCharacter.GetCharacterId()}");
    }
    private void CompleteCleanup()
    {
        // Flag��� ���� ��� ���� ����
        ClearFinalPath();
        ClearFlags();

        // Goal ���̶���Ʈ�� ���� (�߰�)
        if (touchInputManager != null)
        {
            touchInputManager.ClearSelection(); // �̰͸� �߰��ϸ� ��
        }

        // ���� ����
        currentCharacter = null;
        currentPath.Clear();
    }

    /// <summary>
    /// �ϼ��� ��� ���� (���� ���� �̵���)
    /// </summary>
    private void SaveCompletedPath()
    {
        // TODO: ���߿� GameManager�� ���� �ý��ۿ��� ����
        Debug.Log($"Saved path for {currentCharacter.GetCharacterId()}: {string.Join(" -> ", currentPath)}");
    }

    /// <summary>
    /// ĳ���͸� �Ϸ� ���·� ����
    /// </summary>
    private void SetCharacterCompleted(GamePiece character)
    {
        character.SetCompleted(true);
    }
    // === ���� �޼���� (���� �ּ�ȭ) ===

    /// <summary>
    /// Flag ����
    /// </summary>
    private void CreateFlag(Vector3Int gridPosition)
    {
        if (flagPrefab == null) return;

        Vector3 worldPos = gridVisualizer.GridToWorldPosition(gridPosition);
        GameObject flag = Instantiate(flagPrefab, worldPos, Quaternion.identity);
        flag.transform.localScale = Vector3.one * flagScale;
        flag.name = $"Flag_{pathFlags.Count}";

        pathFlags.Add(flag);
    }

    /// <summary>
    /// ���� ��� ǥ��
    /// </summary>
    private void ShowFinalPath()
    {
        if (finalPathLine == null || currentPath.Count < 2) return;

        finalPathLine.gameObject.SetActive(true);
        finalPathLine.positionCount = currentPath.Count;

        for (int i = 0; i < currentPath.Count; i++)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(currentPath[i]);
            finalPathLine.SetPosition(i, worldPos);
        }
    }

    /// <summary>
    /// ���� ��� ���� ����
    /// </summary>
    private void CreateFinalPathLineRenderer()
    {
        GameObject finalLineObj = new GameObject("FinalPath");
        finalLineObj.transform.SetParent(transform);

        finalPathLine = finalLineObj.AddComponent<LineRenderer>();
        finalPathLine.material = pathLineMaterial;
        finalPathLine.startColor = selectedPathColor;
        finalPathLine.endColor = selectedPathColor;
        finalPathLine.startWidth = lineWidth * 1.5f;
        finalPathLine.endWidth = lineWidth * 1.5f;
        finalPathLine.sortingOrder = 20;
        finalPathLine.useWorldSpace = true;
        finalPathLine.gameObject.SetActive(false);
    }

    /// <summary>
    /// ��� �ð��� ��� ����
    /// </summary>
    private void ClearAllVisuals()
    {
        ClearAvailablePathLines();
        ClearFlags();
        ClearFinalPath();
    }

    /// <summary>
    /// ������ ��� ���ε� ����
    /// </summary>
    private void ClearAvailablePathLines()
    {
        foreach (var line in availablePathLines)
        {
            if (line != null)
                DestroyImmediate(line.gameObject);
        }
        availablePathLines.Clear();
    }

    /// <summary>
    /// Flag�� ����
    /// </summary>
    private void ClearFlags()
    {
        foreach (var flag in pathFlags)
        {
            if (flag != null)
                DestroyImmediate(flag.gameObject);
        }
        pathFlags.Clear();
    }

    /// <summary>
    /// ���� ��� ���� ����
    /// </summary>
    private void ClearFinalPath()
    {
        if (finalPathLine != null)
            finalPathLine.gameObject.SetActive(false);
    }

    /// <summary>
    /// ���� ��� ��ȯ
    /// </summary>
    public List<Vector3Int> GetCurrentPath()
    {
        return new List<Vector3Int>(currentPath);
    }

    /// <summary>
    /// ��� ���� ������ Ȯ��
    /// </summary>
    public bool IsSelectingPath()
    {
        return currentCharacter != null;
    }

    /// <summary>
    /// ���� ���õ� ĳ���� ��ȯ
    /// </summary>
    public GamePiece GetCurrentCharacter()
    {
        return currentCharacter;
    }
}