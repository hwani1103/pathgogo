using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��� ������ ����ϴ� �ý���
/// Ÿ�� ���� ����, ��ֹ�, ��� ��ȿ���� �˻��մϴ�
/// </summary>
public class PathValidator : MonoBehaviour
{
    // ����
    private GridVisualizer gridVisualizer;
    private LevelLoader levelLoader;
    private LevelData currentLevelData;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        levelLoader = FindFirstObjectByType<LevelLoader>();
    }

    void Start()
    {
        // LevelLoader���� ���� ���� ������ ��������
        if (levelLoader != null)
        {
            // LevelLoader�� CurrentLevelData ������Ƽ �߰� �ʿ�
            // currentLevelData = levelLoader.GetCurrentLevelData();
        }
    }

    /// <summary>
    /// ���� ������ ���� (LevelLoader���� ȣ��)
    /// </summary>
    public void SetLevelData(LevelData levelData)
    {
        currentLevelData = levelData;
    }

    /// <summary>
    /// Ư�� ��ġ�� �̵� �������� Ȯ��
    /// </summary>
    public bool IsValidPosition(Vector3Int position)
    {
        // �׸��� ���� Ȯ��
        if (gridVisualizer != null && !gridVisualizer.IsValidGridPosition(position))
            return false;

        // Ÿ�� ���� Ȯ��
        if (!HasTileAt(position))
            return false;

        return true;
    }

    /// <summary>
    /// Ư�� ��ġ�� Ÿ���� �ִ��� Ȯ��
    /// </summary>
    public bool HasTileAt(Vector3Int position)
    {
        if (currentLevelData == null)
        {
            Debug.LogWarning("LevelData is not set in PathValidator");
            return false;
        }

        return currentLevelData.HasTileAt(position);
    }

    /// <summary>
    /// Ư�� ��ġ�� ��ֹ��� �ִ��� Ȯ��
    /// </summary>
    public bool HasObstacleAt(Vector3Int position, CharacterController excludeCharacter = null)
    {
        if (levelLoader == null) return false;

        // �ٸ� ĳ���Ͱ� �ִ��� Ȯ��
        var characterAtPosition = levelLoader.GetCharacterAt(position);
        if (characterAtPosition != null && characterAtPosition != excludeCharacter)
            return true;

        // Ÿ���� ������ ��ֹ��� ����
        if (!HasTileAt(position))
            return true;

        return false;
    }

    /// <summary>
    /// �� ��ġ ������ ���� ��ο� ��ֹ��� �ִ��� Ȯ��
    /// </summary>
    public bool HasObstacleInPath(Vector3Int fromPos, Vector3Int toPos, CharacterController excludeCharacter = null)
    {
        // ���������� �ƴ� ��� ��ȿ
        if (fromPos.x != toPos.x && fromPos.y != toPos.y)
            return true;

        Vector3Int diff = toPos - fromPos;
        Vector3Int direction = new Vector3Int(
            diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1),
            diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1),
            0
        );

        Vector3Int currentPos = fromPos + direction;

        // ��λ��� ��� Ÿ�� �˻�
        while (currentPos != toPos)
        {
            if (HasObstacleAt(currentPos, excludeCharacter))
                return true;
            currentPos += direction;
        }

        // ��ǥ ��ġ�� �˻�
        return HasObstacleAt(toPos, excludeCharacter);
    }

    /// <summary>
    /// ĳ���Ͱ� Ư�� �������� �̵� ������ �ִ� �Ÿ� ���
    /// </summary>
    public List<Vector3Int> GetValidPositionsInDirection(Vector3Int startPos, Vector3Int direction, CharacterController character, int maxDistance = 10)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        Vector3Int currentPos = startPos;

        for (int i = 1; i <= maxDistance; i++)
        {
            Vector3Int nextPos = currentPos + direction * i;

            // ��ȿ���� ���� ��ġ�� �ߴ�
            if (!IsValidPosition(nextPos))
                break;

            // ��ֹ��� ������ �ߴ�
            if (HasObstacleAt(nextPos, character))
                break;

            validPositions.Add(nextPos);

            // �������� �����ϸ� �ߴ�
            if (IsGoalPosition(nextPos, character))
                break;
        }

        return validPositions;
    }

    /// <summary>
    /// Ư�� ��ġ�� ĳ������ ���������� Ȯ��
    /// </summary>
    public bool IsGoalPosition(Vector3Int position, CharacterController character)
    {
        if (levelLoader == null || character == null) return false;

        var availableGoals = GetAvailableGoalsForCharacter(character);
        foreach (var goal in availableGoals)
        {
            if (goal.GetGridPosition() == position)
                return true;
        }
        return false;
    }

    /// <summary>
    /// ĳ���Ͱ� ����� �� �ִ� �������� ��ȯ
    /// </summary>
    public List<GoalController> GetAvailableGoalsForCharacter(CharacterController character)
    {
        List<GoalController> availableGoals = new List<GoalController>();

        if (character == null || levelLoader == null) return availableGoals;

        var allGoals = levelLoader.GetSpawnedGoals();
        string characterId = character.GetCharacterId();

        foreach (var goal in allGoals)
        {
            if (goal.CanUseGoal(characterId))
            {
                availableGoals.Add(goal);
            }
        }

        return availableGoals;
    }

    /// <summary>
    /// ������-1 ���ÿ��� �������� �����Ÿ��� �ִ� ��ġ�� ���͸�
    /// </summary>
    public List<Vector3Int> FilterLastSelectionPositions(List<Vector3Int> positions, CharacterController character)
    {
        List<Vector3Int> filteredPositions = new List<Vector3Int>();

        var availableGoals = GetAvailableGoalsForCharacter(character);

        foreach (Vector3Int pos in positions)
        {
            foreach (var goal in availableGoals)
            {
                Vector3Int goalPos = goal.GetGridPosition();
                // �����Ÿ��� �ִ��� Ȯ�� (���� �Ǵ� ����)
                if (pos.x == goalPos.x || pos.y == goalPos.y)
                {
                    // �ش� ��ġ���� ���������� ��ο� ��ֹ��� ������ Ȯ��
                    if (!HasObstacleInPath(pos, goalPos, character))
                    {
                        filteredPositions.Add(pos);
                        break;
                    }
                }
            }
        }

        return filteredPositions;
    }

    /// <summary>
    /// ��ü ����� ��ȿ�� �˻�
    /// </summary>
    public bool ValidateCompletePath(List<Vector3Int> path, CharacterController character)
    {
        if (path == null || path.Count < 2 || character == null)
            return false;

        // �� ������ ��ȿ�� �˻�
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3Int fromPos = path[i];
            Vector3Int toPos = path[i + 1];

            // ���� �̵����� Ȯ��
            if (fromPos.x != toPos.x && fromPos.y != toPos.y)
            {
                Debug.LogWarning($"Invalid path: diagonal movement from {fromPos} to {toPos}");
                return false;
            }

            // ��λ� ��ֹ��� �ִ��� Ȯ��
            if (HasObstacleInPath(fromPos, toPos, character))
            {
                Debug.LogWarning($"Invalid path: obstacle in path from {fromPos} to {toPos}");
                return false;
            }
        }

        // ������ ��ġ�� ���������� Ȯ��
        Vector3Int finalPos = path[path.Count - 1];
        if (!IsGoalPosition(finalPos, character))
        {
            Debug.LogWarning($"Invalid path: final position {finalPos} is not a valid goal for character {character.GetCharacterId()}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// ����׿�: ��� ���� ���
    /// </summary>
    public void DebugLogPath(List<Vector3Int> path, string pathName = "Path")
    {
        if (path == null || path.Count == 0)
        {
            Debug.Log($"{pathName}: Empty or null");
            return;
        }

        string pathString = string.Join(" -> ", path);
        Debug.Log($"{pathName}: {pathString} (Length: {path.Count})");

        // �� ������ ��ȿ���� üũ
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3Int from = path[i];
            Vector3Int to = path[i + 1];
            bool hasObstacle = HasObstacleInPath(from, to);
            Debug.Log($"  Segment {i}: {from} -> {to}, HasObstacle: {hasObstacle}");
        }
    }
}