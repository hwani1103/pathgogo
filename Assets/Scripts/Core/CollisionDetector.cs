using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ���� �� �浹�� �����ϴ� �ý���
/// ��� ������� ���� �浹 ���ɼ��� �̸� ����մϴ�
/// </summary>
public class CollisionDetector : MonoBehaviour
{
    [Header("Collision Settings")]
    [SerializeField] private float timeStep = 0.1f; // �ùķ��̼� �ð� ����
    [SerializeField] private bool enableDebugMode = true;

    // ����
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
    /// �浹 ��� ������
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
    /// ��� ĳ������ ��θ� �˻��Ͽ� �浹 ���ɼ��� Ȯ��
    /// </summary>
    public CollisionResult CheckAllCollisions(Dictionary<string, List<Vector3Int>> characterPaths)
    {
        if (characterPaths == null || characterPaths.Count < 2)
            return new CollisionResult(false);

        // �� ĳ������ �ð��� ��ġ �ùķ��̼�
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

        // �ð����� �浹 �˻�
        return CheckTimelineCollisions(characterTimelines, maxTime);
    }

    /// <summary>
    /// �� ĳ���� ���� �浹�� �˻� (���� �˻��)
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
    /// �ð��� ��ġ �����͸� �������� �浹 �˻�
    /// </summary>
    private CollisionResult CheckTimelineCollisions(Dictionary<string, List<Vector3>> timelines, float maxTime)
    {
        int maxTimeSteps = Mathf.CeilToInt(maxTime / timeStep) + 1;

        for (int timeIndex = 0; timeIndex < maxTimeSteps; timeIndex++)
        {
            float currentTime = timeIndex * timeStep;
            var positionsAtTime = new Dictionary<string, Vector3>();

            // ���� �ð����� �� ĳ������ ��ġ ���
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
                    // ��� �Ϸ� �Ŀ��� ������ ��ġ�� �ӹ��� ����
                    positionsAtTime[characterId] = timeline[timeline.Count - 1];
                }
            }

            // ���� �ð����� ��ġ �浹 �˻�
            var collision = CheckPositionCollisions(positionsAtTime, currentTime);
            if (collision.hasCollision)
                return collision;
        }

        return new CollisionResult(false);
    }

    /// <summary>
    /// Ư�� �ð����� ĳ���͵��� ��ġ �浹 �˻�
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

                // ���� �浹 �Ǵ� (�Ÿ� ���)
                float distance = Vector3.Distance(pos1, pos2);
                if (distance < 0.3f) // �浹 �Ӱ谪
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
    /// �浹 Ÿ�� ���� (�����浹, ����, ����Ÿ������ ��)
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
    /// Ư�� ĳ������ ��ΰ� �ٸ� ĳ���͵�� �浹�ϴ��� �˻�
    /// </summary>
    public CollisionResult CheckSingleCharacterPath(string characterId, List<Vector3Int> newPath)
    {
        if (levelLoader == null) return new CollisionResult(false);

        var allCharacters = levelLoader.GetSpawnedCharacters();
        var allPaths = new Dictionary<string, List<Vector3Int>>();

        // �ٸ� ĳ���͵��� ���� ��� ���� (���� ���� �� ��� ����ҿ��� �����;� ��)
        foreach (var character in allCharacters)
        {
            if (character.GetCharacterId() == characterId)
            {
                allPaths[characterId] = newPath;
            }
            else
            {
                // TODO: �ٸ� ĳ���͵��� ������ ��θ� �������� ���� �ʿ�
                // ����� �������� �����ϴ� �⺻ ��η� ����
                var basicPath = new List<Vector3Int> { character.GetCurrentGridPosition() };
                allPaths[character.GetCharacterId()] = basicPath;
            }
        }

        return CheckAllCollisions(allPaths);
    }

    /// <summary>
    /// �浹 ����� ����ڿ��� ǥ���ϱ� ���� �޽��� ����
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
    /// ����׿�: �浹 �ùķ��̼� ����� �ð�ȭ
    /// </summary>
    public void DebugDrawCollisionPreview(CollisionResult result)
    {
        if (!enableDebugMode || !result.hasCollision) return;

        // Scene �信�� �浹 ���� ǥ��
        Debug.DrawRay(
            new Vector3(result.collisionPosition.x, result.collisionPosition.y, 0),
            Vector3.up * 0.5f,
            Color.red,
            2f
        );

        Debug.Log(GetCollisionMessage(result));
    }

    /// <summary>
    /// �浹 ���� ����
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
        // �׽�Ʈ�� ��� ����
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