using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 간 충돌을 감지하는 시스템
/// 경로 기반으로 실제 충돌 가능성을 미리 계산합니다
/// </summary>
public class CollisionDetector : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float timeStep = 0.1f; // 시뮬레이션 시간 단위
    [SerializeField] private bool enableDebugMode = true;

    // 참조
    private LevelLoader levelLoader;
    private MovementSimulator movementSimulator;

    void Awake()
    {
        levelLoader = FindFirstObjectByType<LevelLoader>();
        movementSimulator = GetComponent<MovementSimulator>();

        if (movementSimulator == null)
            movementSimulator = gameObject.AddComponent<MovementSimulator>();
    }

    /// <summary>
    /// 충돌 결과 데이터
    /// </summary>
    [System.Serializable]
    public struct CollisionResult
    {
        public bool hasCollision;
        public Vector3Int collisionPosition;
        public float collisionTime;
        public List<string> involvedCharacters;
        public string collisionType; // "head-on", "same-tile", "crossing"

        public CollisionResult(bool collision, Vector3Int position = default, float time = 0f,
                              List<string> characters = null, string type = "unknown")
        {
            hasCollision = collision;
            collisionPosition = position;
            collisionTime = time;
            involvedCharacters = characters ?? new List<string>();
            collisionType = type;
        }
    }

    /// <summary>
    /// 모든 캐릭터의 경로를 검사하여 충돌 가능성을 확인
    /// </summary>
    public CollisionResult CheckAllCollisions(Dictionary<string, List<Vector3Int>> characterPaths)
    {
        if (characterPaths == null || characterPaths.Count < 2)
            return new CollisionResult(false);

        // 각 캐릭터의 시간별 위치 시뮬레이션
        var characterTimelines = new Dictionary<string, List<Vector3>>();
        float maxTime = 0f;

        foreach (var kvp in characterPaths)
        {
            string characterId = kvp.Key;
            List<Vector3Int> path = kvp.Value;

            if (path == null || path.Count < 2) continue;

            var timeline = movementSimulator.SimulateMovement(path, timeStep);
            characterTimelines[characterId] = timeline;

            float pathTime = (timeline.Count - 1) * timeStep;
            if (pathTime > maxTime) maxTime = pathTime;
        }

        // 시간별로 충돌 검사
        return CheckTimelineCollisions(characterTimelines, maxTime);
    }

    /// <summary>
    /// 두 캐릭터 간의 충돌만 검사 (빠른 검사용)
    /// </summary>
    public CollisionResult CheckTwoCharacterCollision(string characterId1, List<Vector3Int> path1,
                                                      string characterId2, List<Vector3Int> path2)
    {
        var paths = new Dictionary<string, List<Vector3Int>>
        {
            { characterId1, path1 },
            { characterId2, path2 }
        };

        return CheckAllCollisions(paths);
    }

    /// <summary>
    /// 시간별 위치 데이터를 바탕으로 충돌 검사
    /// </summary>
    private CollisionResult CheckTimelineCollisions(Dictionary<string, List<Vector3>> timelines, float maxTime)
    {
        int maxTimeSteps = Mathf.CeilToInt(maxTime / timeStep) + 1;

        for (int timeIndex = 0; timeIndex < maxTimeSteps; timeIndex++)
        {
            float currentTime = timeIndex * timeStep;
            var positionsAtTime = new Dictionary<string, Vector3>();

            // 현재 시간에서 각 캐릭터의 위치 계산
            foreach (var kvp in timelines)
            {
                string characterId = kvp.Key;
                List<Vector3> timeline = kvp.Value;

                if (timeIndex < timeline.Count)
                {
                    positionsAtTime[characterId] = timeline[timeIndex];
                }
                else if (timeline.Count > 0)
                {
                    // 경로 완료 후에는 마지막 위치에 머물러 있음
                    positionsAtTime[characterId] = timeline[timeline.Count - 1];
                }
            }

            // 현재 시간에서 위치 충돌 검사
            var collision = CheckPositionCollisions(positionsAtTime, currentTime);
            if (collision.hasCollision)
                return collision;
        }

        return new CollisionResult(false);
    }

    /// <summary>
    /// 특정 시간에서 캐릭터들의 위치 충돌 검사
    /// </summary>
    private CollisionResult CheckPositionCollisions(Dictionary<string, Vector3> positions, float time)
    {
        var characterIds = new List<string>(positions.Keys);

        for (int i = 0; i < characterIds.Count; i++)
        {
            for (int j = i + 1; j < characterIds.Count; j++)
            {
                string char1 = characterIds[i];
                string char2 = characterIds[j];

                Vector3 pos1 = positions[char1];
                Vector3 pos2 = positions[char2];

                // 실제 충돌 판단 (거리 기반)
                float distance = Vector3.Distance(pos1, pos2);
                if (distance < 0.3f) // 충돌 임계값
                {
                    Vector3Int gridCollisionPos = new Vector3Int(
                        Mathf.RoundToInt((pos1.x + pos2.x) / 2f),
                        Mathf.RoundToInt((pos1.y + pos2.y) / 2f),
                        0
                    );

                    var involvedCharacters = new List<string> { char1, char2 };
                    string collisionType = DetermineCollisionType(pos1, pos2);

                    if (enableDebugMode)
                    {
                        Debug.Log($"Collision detected at time {time:F1}s between {char1} and {char2}");
                        Debug.Log($"Positions: {char1}({pos1}) vs {char2}({pos2}), Distance: {distance:F2}");
                        Debug.Log($"Collision Type: {collisionType}");
                    }

                    return new CollisionResult(
                        true,
                        gridCollisionPos,
                        time,
                        involvedCharacters,
                        collisionType
                    );
                }
            }
        }

        return new CollisionResult(false);
    }

    /// <summary>
    /// 충돌 타입 결정 (정면충돌, 교차, 동일타일점유 등)
    /// </summary>
    private string DetermineCollisionType(Vector3 pos1, Vector3 pos2)
    {
        float distance = Vector3.Distance(pos1, pos2);

        if (distance < 0.1f)
            return "same-tile";
        else if (distance < 0.5f)
            return "close-proximity";
        else
            return "crossing";
    }

    /// <summary>
    /// 특정 캐릭터의 경로가 다른 캐릭터들과 충돌하는지 검사
    /// </summary>
    public CollisionResult CheckSingleCharacterPath(string characterId, List<Vector3Int> newPath)
    {
        if (levelLoader == null) return new CollisionResult(false);

        var allCharacters = levelLoader.GetSpawnedCharacters();
        var allPaths = new Dictionary<string, List<Vector3Int>>();

        // 다른 캐릭터들의 현재 경로 수집 (실제 구현 시 경로 저장소에서 가져와야 함)
        foreach (var character in allCharacters)
        {
            if (character.GetCharacterId() == characterId)
            {
                allPaths[characterId] = newPath;
            }
            else
            {
                // TODO: 다른 캐릭터들의 설정된 경로를 가져오는 로직 필요
                // 현재는 시작점만 포함하는 기본 경로로 설정
                var basicPath = new List<Vector3Int> { character.GetCurrentGridPosition() };
                allPaths[character.GetCharacterId()] = basicPath;
            }
        }

        return CheckAllCollisions(allPaths);
    }

    /// <summary>
    /// 충돌 결과를 사용자에게 표시하기 위한 메시지 생성
    /// </summary>
    public string GetCollisionMessage(CollisionResult result)
    {
        if (!result.hasCollision)
            return "No collision detected.";

        string characterList = string.Join(" and ", result.involvedCharacters);
        return $"Collision between {characterList} at {result.collisionPosition} " +
               $"(Time: {result.collisionTime:F1}s, Type: {result.collisionType})";
    }

    /// <summary>
    /// 디버그용: 충돌 시뮬레이션 결과를 시각화
    /// </summary>
    public void DebugDrawCollisionPreview(CollisionResult result)
    {
        if (!enableDebugMode || !result.hasCollision) return;

        // Scene 뷰에서 충돌 지점 표시
        Debug.DrawRay(
            new Vector3(result.collisionPosition.x, result.collisionPosition.y, 0),
            Vector3.up * 0.5f,
            Color.red,
            2f
        );

        Debug.Log(GetCollisionMessage(result));
    }

    /// <summary>
    /// 충돌 감지 설정
    /// </summary>
    public void SetCollisionSettings(float newTimeStep, bool debugMode)
    {
        timeStep = Mathf.Clamp(newTimeStep, 0.05f, 1f);
        enableDebugMode = debugMode;
    }

#if UNITY_EDITOR
    [ContextMenu("Test Collision Detection")]
    public void TestCollisionDetection()
    {
        // 테스트용 경로 생성
        var testPaths = new Dictionary<string, List<Vector3Int>>
        {
            { "A", new List<Vector3Int> { new Vector3Int(0, 0, 0), new Vector3Int(1, 0, 0), new Vector3Int(2, 0, 0) } },
            { "B", new List<Vector3Int> { new Vector3Int(2, 2, 0), new Vector3Int(2, 1, 0), new Vector3Int(2, 0, 0) } }
        };

        var result = CheckAllCollisions(testPaths);
        Debug.Log($"Test Result: {GetCollisionMessage(result)}");
        DebugDrawCollisionPreview(result);
    }
#endif
}