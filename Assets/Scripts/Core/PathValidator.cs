using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경로 검증을 담당하는 시스템
/// 타일 존재 여부, 장애물, 경로 유효성을 검사합니다
/// </summary>
public class PathValidator : MonoBehaviour
{
    // 참조
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
        // LevelLoader에서 현재 레벨 데이터 가져오기
        if (levelLoader != null)
        {
            // LevelLoader에 CurrentLevelData 프로퍼티 추가 필요
            // currentLevelData = levelLoader.GetCurrentLevelData();
        }
    }

    /// <summary>
    /// 레벨 데이터 설정 (LevelLoader에서 호출)
    /// </summary>
    public void SetLevelData(LevelData levelData)
    {
        currentLevelData = levelData;
    }

    /// <summary>
    /// 특정 위치가 이동 가능한지 확인
    /// </summary>
    public bool IsValidPosition(Vector3Int position)
    {
        // 그리드 범위 확인
        if (gridVisualizer != null && !gridVisualizer.IsValidGridPosition(position))
            return false;

        // 타일 존재 확인
        if (!HasTileAt(position))
            return false;

        return true;
    }

    /// <summary>
    /// 특정 위치에 타일이 있는지 확인
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
    /// 특정 위치에 장애물이 있는지 확인
    /// </summary>
    public bool HasObstacleAt(Vector3Int position, CharacterController excludeCharacter = null)
    {
        if (levelLoader == null) return false;

        // 다른 캐릭터가 있는지 확인
        var characterAtPosition = levelLoader.GetCharacterAt(position);
        if (characterAtPosition != null && characterAtPosition != excludeCharacter)
            return true;

        // 타일이 없으면 장애물로 간주
        if (!HasTileAt(position))
            return true;

        return false;
    }

    /// <summary>
    /// 두 위치 사이의 직선 경로에 장애물이 있는지 확인
    /// </summary>
    public bool HasObstacleInPath(Vector3Int fromPos, Vector3Int toPos, CharacterController excludeCharacter = null)
    {
        // 동서남북이 아닌 경우 무효
        if (fromPos.x != toPos.x && fromPos.y != toPos.y)
            return true;

        Vector3Int diff = toPos - fromPos;
        Vector3Int direction = new Vector3Int(
            diff.x == 0 ? 0 : (diff.x > 0 ? 1 : -1),
            diff.y == 0 ? 0 : (diff.y > 0 ? 1 : -1),
            0
        );

        Vector3Int currentPos = fromPos + direction;

        // 경로상의 모든 타일 검사
        while (currentPos != toPos)
        {
            if (HasObstacleAt(currentPos, excludeCharacter))
                return true;
            currentPos += direction;
        }

        // 목표 위치도 검사
        return HasObstacleAt(toPos, excludeCharacter);
    }

    /// <summary>
    /// 캐릭터가 특정 방향으로 이동 가능한 최대 거리 계산
    /// </summary>
    public List<Vector3Int> GetValidPositionsInDirection(Vector3Int startPos, Vector3Int direction, CharacterController character, int maxDistance = 10)
    {
        List<Vector3Int> validPositions = new List<Vector3Int>();
        Vector3Int currentPos = startPos;

        for (int i = 1; i <= maxDistance; i++)
        {
            Vector3Int nextPos = currentPos + direction * i;

            // 유효하지 않은 위치면 중단
            if (!IsValidPosition(nextPos))
                break;

            // 장애물이 있으면 중단
            if (HasObstacleAt(nextPos, character))
                break;

            validPositions.Add(nextPos);

            // 목적지에 도달하면 중단
            if (IsGoalPosition(nextPos, character))
                break;
        }

        return validPositions;
    }

    /// <summary>
    /// 특정 위치가 캐릭터의 목적지인지 확인
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
    /// 캐릭터가 사용할 수 있는 목적지들 반환
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
    /// 마지막-1 선택에서 목적지와 직선거리상에 있는 위치만 필터링
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
                // 직선거리상에 있는지 확인 (수평 또는 수직)
                if (pos.x == goalPos.x || pos.y == goalPos.y)
                {
                    // 해당 위치에서 목적지까지 경로에 장애물이 없는지 확인
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
    /// 전체 경로의 유효성 검사
    /// </summary>
    public bool ValidateCompletePath(List<Vector3Int> path, CharacterController character)
    {
        if (path == null || path.Count < 2 || character == null)
            return false;

        // 각 구간별 유효성 검사
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3Int fromPos = path[i];
            Vector3Int toPos = path[i + 1];

            // 직선 이동인지 확인
            if (fromPos.x != toPos.x && fromPos.y != toPos.y)
            {
                Debug.LogWarning($"Invalid path: diagonal movement from {fromPos} to {toPos}");
                return false;
            }

            // 경로상에 장애물이 있는지 확인
            if (HasObstacleInPath(fromPos, toPos, character))
            {
                Debug.LogWarning($"Invalid path: obstacle in path from {fromPos} to {toPos}");
                return false;
            }
        }

        // 마지막 위치가 목적지인지 확인
        Vector3Int finalPos = path[path.Count - 1];
        if (!IsGoalPosition(finalPos, character))
        {
            Debug.LogWarning($"Invalid path: final position {finalPos} is not a valid goal for character {character.GetCharacterId()}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 디버그용: 경로 정보 출력
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

        // 각 구간의 유효성도 체크
        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3Int from = path[i];
            Vector3Int to = path[i + 1];
            bool hasObstacle = HasObstacleInPath(from, to);
            Debug.Log($"  Segment {i}: {from} -> {to}, HasObstacle: {hasObstacle}");
        }
    }
}