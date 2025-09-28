using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ������ ��� ������ �����ϴ� �ý���
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

    // ���� ���õ� ĳ����
    private GamePiece currentCharacter;

    // ��� ����
    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private List<GameObject> pathFlags = new List<GameObject>();
    private List<LineRenderer> availablePathLines = new List<LineRenderer>();
    private LineRenderer finalPathLine;

    // �ϼ��� ��ε��� �����ϴ� ��ųʸ�
    private Dictionary<string, List<Vector3Int>> completedPaths = new Dictionary<string, List<Vector3Int>>();

    // �ý��� ����
    private PathValidator pathValidator;
    private GridVisualizer gridVisualizer;
    private LevelLoader levelLoader;

    // �ߺ� �Է� ����
    private Vector3Int lastSelectedPosition = Vector3Int.zero;
    private float lastSelectionTime = 0f;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        levelLoader = FindFirstObjectByType<LevelLoader>();
        pathValidator = GetComponent<PathValidator>();

        if (pathValidator == null)
            pathValidator = gameObject.AddComponent<PathValidator>();

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
        currentPath.Add(character.GetCurrentGridPosition());
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
        // �ߺ� ȣ�� ����
        if (gridPosition == lastSelectedPosition && Time.time - lastSelectionTime < 0.1f)
            return;

        lastSelectedPosition = gridPosition;
        lastSelectionTime = Time.time;

        if (currentCharacter == null) return;

        Vector3Int currentPos = currentPath.Count > 0 ? currentPath[currentPath.Count - 1] : currentCharacter.GetCurrentGridPosition();

        // ������ ���õ� ��ġ�� Redo ����
        if (CanUndoAtPosition(gridPosition))
        {
            UndoLastSelection();
            return;
        }

        // ���ο� ��ġ ���� ����
        if (!IsValidSelection(currentPos, gridPosition))
        {
            ShowInvalidSelectionFeedback(gridPosition);
            return;
        }

        // ��ο� �߰�
        currentPath.Add(gridPosition);
        CreateFlag(gridPosition);

        // �Ϸ� ���� Ȯ��
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);
        if (remainingSelections <= 0 || pathValidator.IsGoalPosition(gridPosition, currentCharacter))
        {
            CompletePath();
        }
        else
        {
            ShowAvailablePaths();
        }
    }

    /// <summary>
    /// ������ ���� ���
    /// </summary>
    public void UndoLastSelection()
    {
        if (currentPath.Count <= 1 || IsPathCompleted()) return;

        // ������ ���� ����
        currentPath.RemoveAt(currentPath.Count - 1);

        // ������ Flag ����
        if (pathFlags.Count > 0)
        {
            GameObject lastFlag = pathFlags[pathFlags.Count - 1];
            if (lastFlag != null)
                DestroyImmediate(lastFlag);
            pathFlags.RemoveAt(pathFlags.Count - 1);
        }

        ClearFinalPath();
        ShowAvailablePaths();

        // �ǵ�� Ȯ�� ����
        TriggerUndoFeedback();
    }

    /// <summary>
    /// �ش� ��ġ���� Undo�� �������� Ȯ�� (������ ��ġ��)
    /// </summary>
    private bool CanUndoAtPosition(Vector3Int position)
    {
        if (currentPath.Count < 2 || IsPathCompleted()) return false;
        Vector3Int lastPosition = currentPath[currentPath.Count - 1];
        return position == lastPosition;
    }

    /// <summary>
    /// ��ΰ� �ϼ��Ǿ����� Ȯ��
    /// </summary>
    private bool IsPathCompleted()
    {
        if (currentPath.Count < 2 || currentCharacter == null) return false;
        Vector3Int lastPosition = currentPath[currentPath.Count - 1];
        return pathValidator.IsGoalPosition(lastPosition, currentCharacter);
    }

    /// <summary>
    /// ��ȿ�� �������� Ȯ��
    /// </summary>
    private bool IsValidSelection(Vector3Int fromPos, Vector3Int toPos)
    {
        if (fromPos.x != toPos.x && fromPos.y != toPos.y) return false;
        if (fromPos == toPos) return false;

        if (!pathValidator.IsValidPosition(toPos) ||
            pathValidator.HasObstacleInPath(fromPos, toPos, currentCharacter))
            return false;

        Vector3Int direction = new Vector3Int(
            toPos.x > fromPos.x ? 1 : (toPos.x < fromPos.x ? -1 : 0),
            toPos.y > fromPos.y ? 1 : (toPos.y < fromPos.y ? -1 : 0),
            0
        );

        var validPositions = pathValidator.GetValidPositionsInDirection(fromPos, direction, currentCharacter);
        if (!validPositions.Contains(toPos))
            return false;

        int remaining = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        // ������ ������ Goal��
        if (remaining == 1)
            return pathValidator.IsGoalPosition(toPos, currentCharacter);

        // ������-1 ������ Goal�� ������
        if (remaining == 2)
        {
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);
            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                if ((toPos.x == goalPos.x || toPos.y == goalPos.y) &&
                    !pathValidator.HasObstacleInPath(toPos, goalPos, currentCharacter))
                    return true;
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// ������ ��� ǥ��
    /// </summary>
    private void ShowAvailablePaths()
    {
        ClearAvailablePathLines();

        if (currentCharacter == null || currentPath.Count == 0) return;

        Vector3Int currentPos = currentPath[currentPath.Count - 1];
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        if (remainingSelections <= 0) return;

        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (Vector3Int direction in directions)
        {
            ShowDirectionPath(currentPos, direction, remainingSelections);
        }
    }

    /// <summary>
    /// Ư�� ���� ��� ǥ��
    /// </summary>
    private void ShowDirectionPath(Vector3Int startPos, Vector3Int direction, int remainingSelections)
    {
        List<Vector3Int> pathPositions = pathValidator.GetValidPositionsInDirection(startPos, direction, currentCharacter);

        if (remainingSelections == 1)
        {
            List<Vector3Int> goalOnlyPositions = new List<Vector3Int>();
            var availableGoals = pathValidator.GetAvailableGoalsForCharacter(currentCharacter);

            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                if (pathPositions.Contains(goalPos))
                    goalOnlyPositions.Add(goalPos);
            }
            pathPositions = goalOnlyPositions;
        }
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

        Vector3 startWorldPos = gridVisualizer.GridToWorldPosition(startPos);
        line.SetPosition(0, startWorldPos);

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
        SetCharacterCompleted(currentCharacter);
        SaveCompletedPathToMemory();

        // Goal ũ�� ����
        if (levelLoader != null)
        {
            var allGoals = levelLoader.GetSpawnedGoals();
            foreach (var goal in allGoals)
            {
                if (goal.CanUseGoal(currentCharacter.GetCharacterId()))
                    goal.transform.localScale = Vector3.one;
            }
        }

        currentCharacter = null;
        currentPath.Clear();

        Invoke(nameof(VisualOnlyCleanup), 1f);
    }

    /// <summary>
    /// �ϼ��� ��θ� �޸𸮿� ����
    /// </summary>
    private void SaveCompletedPathToMemory()
    {
        if (currentCharacter == null || currentPath.Count < 2) return;

        string characterId = currentCharacter.GetCharacterId();
        completedPaths[characterId] = new List<Vector3Int>(currentPath);

        var gameManager = FindFirstObjectByType<MovementGameManager>();
        if (gameManager != null)
            gameManager.OnPathCompleted();
    }

    /// <summary>
    /// �߸��� ���ÿ� ���� �ǵ��
    /// </summary>
    private void ShowInvalidSelectionFeedback(Vector3Int gridPosition)
    {
        if (Application.isMobilePlatform)
            Handheld.Vibrate();

        StartCoroutine(ShowInvalidPositionEffect(gridPosition));
    }

    /// <summary>
    /// Undo �ǵ�� (Ȯ�� ����)
    /// </summary>
    private void TriggerUndoFeedback()
    {
        // ����
        if (Application.isMobilePlatform)
            Handheld.Vibrate();

        // ȿ���� Ȯ�� ����
        // AudioManager.PlayUndoSound();

        // �ð� ȿ�� Ȯ�� ����
        // EffectManager.PlayUndoEffect();
    }

    /// <summary>
    /// �߸��� ��ġ ���� �� �ð� ȿ��
    /// </summary>
    private System.Collections.IEnumerator ShowInvalidPositionEffect(Vector3Int gridPosition)
    {
        if (gridVisualizer == null) yield break;

        GameObject tempMarker = new GameObject("InvalidMarker");
        tempMarker.transform.position = gridVisualizer.GridToWorldPosition(gridPosition);

        var renderer = tempMarker.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSimpleSquareSprite();
        renderer.color = invalidPathColor;
        renderer.sortingOrder = 25;

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
    /// ������ ���簢�� ��������Ʈ ����
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

    private void SetCharacterCompleted(GamePiece character) => character.SetCompleted(true);
    private void VisualOnlyCleanup() { ClearFinalPath(); ClearFlags(); }

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

    // ���� �޼����
    private void ClearAllVisuals() { ClearAvailablePathLines(); ClearFlags(); ClearFinalPath(); }

    private void ClearAvailablePathLines()
    {
        foreach (var line in availablePathLines)
        {
            if (line != null) DestroyImmediate(line.gameObject);
        }
        availablePathLines.Clear();
    }

    private void ClearFlags()
    {
        foreach (var flag in pathFlags)
        {
            if (flag != null) DestroyImmediate(flag.gameObject);
        }
        pathFlags.Clear();
    }

    private void ClearFinalPath()
    {
        if (finalPathLine != null) finalPathLine.gameObject.SetActive(false);
    }

    // ���� �޼����
    public List<Vector3Int> GetCurrentPath() => new List<Vector3Int>(currentPath);
    public bool IsSelectingPath() => currentCharacter != null && currentPath.Count > 0;
    public GamePiece GetCurrentCharacter() => currentCharacter;
    public Dictionary<string, List<Vector3Int>> GetCompletedPaths() => new Dictionary<string, List<Vector3Int>>(completedPaths);
    public void ClearAllCompletedPaths() => completedPaths.Clear();

    public List<Vector3Int> GetCompletedPath(string characterId)
    {
        if (completedPaths.ContainsKey(characterId))
            return new List<Vector3Int>(completedPaths[characterId]);
        return null;
    }
}