using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ������ ��� ������ �����ϴ� �ý���
/// PathValidator, CollisionDetector, MovementSimulator�� ����
/// �ϼ��� ��θ� �����Ͽ� MovementGameManager�� ����
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
    //[SerializeField] private bool showCollisionWarnings = true;

    // ���� ���õ� ĳ����
    private GamePiece currentCharacter;

    // ��� ����
    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private List<GameObject> pathFlags = new List<GameObject>();
    private List<LineRenderer> availablePathLines = new List<LineRenderer>();
    private LineRenderer finalPathLine;

    // �ϼ��� ��ε��� �����ϴ� ��ųʸ� (���� �߰�)
    private Dictionary<string, List<Vector3Int>> completedPaths = new Dictionary<string, List<Vector3Int>>();

    // �ý��� ����
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;
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

        if (gridVisualizer == null)
            Debug.LogError("GridVisualizer not found!");
        if (levelLoader == null)
            Debug.LogError("LevelLoader not found!");
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
    /// Ư�� ��ġ ���� ó��
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
    /// ��ȿ�� �������� Ȯ��
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

        // ���� ǥ�õ� ��� ���� ���� �������� ���� ����
        Vector3Int direction = new Vector3Int(
            toPos.x > fromPos.x ? 1 : (toPos.x < fromPos.x ? -1 : 0),
            toPos.y > fromPos.y ? 1 : (toPos.y < fromPos.y ? -1 : 0),
            0
        );

        var validPositions = pathValidator.GetValidPositionsInDirection(fromPos, direction, currentCharacter);
        if (!validPositions.Contains(toPos))
            return false;

        int remaining = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        // ������ ������ ���� �ݵ�� Goal�̾�� ��
        if (remaining == 1)
        {
            return pathValidator.IsGoalPosition(toPos, currentCharacter);
        }

        // ������-1 ������ ���� Goal�� ������ �־�� ��
        if (remaining == 2)
        {
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);
            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                if ((toPos.x == goalPos.x || toPos.y == goalPos.y) &&
                    !pathValidator.HasObstacleInPath(toPos, goalPos, currentCharacter))
                {
                    return true;
                }
            }
            return false;
        }

        // ������-2 ������ �� �߰� ����
        if (remaining == 3)
        {
            // �ش� ��ġ���� �� �� �� �̵����� �� Goal�� �������� ��ȿ�� ��ġ�� �ִ��� Ȯ��
            bool canReachGoalLine = false;
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);

            Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

            foreach (var dir in directions)
            {
                var nextPositions = pathValidator.GetValidPositionsInDirection(toPos, dir, currentCharacter);

                foreach (var nextPos in nextPositions)
                {
                    // ���� ��ġ�� Goal�� ������ �ִ��� Ȯ��
                    foreach (var goal in availableGoals)
                    {
                        Vector3Int goalPos = goal.GetGridPosition();
                        if ((nextPos.x == goalPos.x || nextPos.y == goalPos.y) &&
                            !pathValidator.HasObstacleInPath(nextPos, goalPos, currentCharacter))
                        {
                            canReachGoalLine = true;
                            break;
                        }
                    }
                    if (canReachGoalLine) break;
                }
                if (canReachGoalLine) break;
            }

            if (!canReachGoalLine) return false;
        }

        return true;
    }

    /// <summary>
    /// ���� ��ġ���� ������ ��� ��� ǥ��
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
    /// Ư�� �������� ������ ��� ǥ��
    /// </summary>
    private void ShowDirectionPath(Vector3Int startPos, Vector3Int direction, int remainingSelections)
    {
        // PathValidator�� ����Ͽ� �ش� ������ ��ȿ�� ��ġ�� ��������
        List<Vector3Int> pathPositions = pathValidator.GetValidPositionsInDirection(startPos, direction, currentCharacter);

        // ������ ���� (remainingSelections == 1): Goal�� ǥ��
        if (remainingSelections == 1)
        {
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
        }
        // ������-1 ���� (remainingSelections == 2): Goal�� �������� ����
        else if (remainingSelections == 2)
        {
            pathPositions = pathValidator.FilterLastSelectionPositions(pathPositions, currentCharacter);
        }

        if (pathPositions.Count > 0)
        {
            CreateAvailablePathLine(startPos, pathPositions);
        }
    }

    /// <summary>
    /// ������ ��� Line Renderer ����
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

        ClearAvailablePathLines();
        ShowFinalPath();
        SetCharacterCompleted(currentCharacter); // ���� �Ϸ� ����
        SaveCompletedPathToMemory(); // ��� ���� �� �˸�

        // Goal ũ�� ����
        if (levelLoader != null)
        {
            var allGoals = levelLoader.GetSpawnedGoals();
            foreach (var goal in allGoals)
            {
                if (goal.CanUseGoal(currentCharacter.GetCharacterId()))
                {
                    goal.transform.localScale = Vector3.one;
                }
            }
        }

        // ���� ���� ������ ��������
        currentCharacter = null;
        currentPath.Clear();

        // �ð��� ������ 1�� ��
        Invoke(nameof(VisualOnlyCleanup), 1f);
    }

    /// <summary>
    /// �ϼ��� ��θ� �޸𸮿� ���� (�̵� �ý��ۿ��� ���)
    /// </summary>
    private void SaveCompletedPathToMemory()
    {
        if (currentCharacter == null || currentPath.Count < 2) return;

        string characterId = currentCharacter.GetCharacterId();
        completedPaths[characterId] = new List<Vector3Int>(currentPath);

        Debug.Log($"Saved path for {characterId}: {string.Join(" -> ", currentPath)}");

        // MovementGameManager���� ��� �ϼ� �˸�
        NotifyPathCompleted();
    }

    /// <summary>
    /// MovementGameManager���� ��� �ϼ� �˸�
    /// </summary>
    private void NotifyPathCompleted()
    {

        var gameManager = FindFirstObjectByType<MovementGameManager>();


        if (gameManager != null)
        {
            gameManager.OnPathCompleted();
        }
        else
        {
        }
    }

    /// <summary>
    /// ��� �ϼ��� ��� ��ȯ
    /// </summary>
    public Dictionary<string, List<Vector3Int>> GetCompletedPaths()
    {
        return new Dictionary<string, List<Vector3Int>>(completedPaths);
    }

    /// <summary>
    /// Ư�� ĳ������ �ϼ��� ��� ��ȯ
    /// </summary>
    public List<Vector3Int> GetCompletedPath(string characterId)
    {
        if (completedPaths.ContainsKey(characterId))
            return new List<Vector3Int>(completedPaths[characterId]);
        return null;
    }

    /// <summary>
    /// ��� ��� �ʱ�ȭ
    /// </summary>
    public void ClearAllCompletedPaths()
    {
        completedPaths.Clear();
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
    /// ĳ���͸� �Ϸ� ���·� ����
    /// </summary>
    private void SetCharacterCompleted(GamePiece character)
    {
        character.SetCompleted(true);
    }

    private void VisualOnlyCleanup()
    {
        ClearFinalPath();
        ClearFlags();
    }

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
        return currentCharacter != null && currentPath.Count > 0;
    }

    /// <summary>
    /// ���� ���õ� ĳ���� ��ȯ
    /// </summary>
    public GamePiece GetCurrentCharacter()
    {
        return currentCharacter;
    }
}