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
    private CharacterController currentCharacter;

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
    public void StartPathSelection(CharacterController character)
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

        // PathValidator�� ����� ����
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
            // ��� �ϼ� - �浹 �˻� ����
            if (ValidateCompletePathWithCollision())
            {
                CompletePath();
            }
            else
            {
                // �浹 �߻� - ����ڿ��� �˸�
                ShowCollisionWarning();
                // ������ ������ ������� ���� (�ɼ�)
                // UndoLastSelection();
            }
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
        // �������� ������ �ִ��� Ȯ��
        if (fromPos.x != toPos.x && fromPos.y != toPos.y)
        {
            Debug.Log("Invalid: Not cardinal direction");
            return false;
        }

        // ���� ��ġ�� �ƴ��� Ȯ��
        if (fromPos == toPos)
        {
            Debug.Log("Invalid: Same position");
            return false;
        }

        // PathValidator�� ���� ����
        return pathValidator.IsValidPosition(toPos) &&
               !pathValidator.HasObstacleInPath(fromPos, toPos, currentCharacter);
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
        // PathValidator�� ����Ͽ� �ش� ������ ��ȿ�� ��ġ�� ��������
        List<Vector3Int> pathPositions = pathValidator.GetValidPositionsInDirection(
            startPos, direction, currentCharacter, 10
        );

        // ������-1 ���� ���� Ȯ��
        if (remainingSelections == 1)
        {
            pathPositions = pathValidator.FilterLastSelectionPositions(pathPositions, currentCharacter);
        }

        if (pathPositions.Count > 0)
        {
            CreateAvailablePathLine(startPos, pathPositions);
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
        // ������ ��� ���ε� �����
        ClearAvailablePathLines();

        // ���� ��� ǥ��
        ShowFinalPath();

        // MovementSimulator�� ����� �̸����� (�ɼ�)
        if (movementSimulator != null)
        {
            movementSimulator.DebugDrawMovementPreview(currentPath, 1f);
        }

        // 1�� �� ����
        Invoke(nameof(ClearFinalPath), 1f);

        pathValidator.DebugLogPath(currentPath, $"Completed path for {currentCharacter.GetCharacterId()}");
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
    public CharacterController GetCurrentCharacter()
    {
        return currentCharacter;
    }
}