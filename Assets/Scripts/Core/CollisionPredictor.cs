using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// ĳ���� �̵� �� �浹�� ���� ����ϴ� �ý���
/// </summary>
public class CollisionPredictor : MonoBehaviour
{
    [System.Serializable]
    public struct CollisionEvent
    {
        public float time;              // �浹 �߻� �ð�
        public Vector3Int position;     // �浹 �߻� ��ġ
        public List<string> characterIds; // �浹�ϴ� ĳ���͵�

        public CollisionEvent(float t, Vector3Int pos, List<string> ids)
        {
            time = t;
            position = pos;
            characterIds = new List<string>(ids);
        }
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    // ����
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
    }

    /// <summary>
    /// ��� ĳ������ ��θ� �м��Ͽ� �浹 �̺�Ʈ���� ���
    /// </summary>
    public List<CollisionEvent> PredictCollisions(Dictionary<string, List<Vector3Int>> characterPaths)
    {
        Debug.Log($"PredictCollisions called with {characterPaths.Count} characters");

        var collisionEvents = new List<CollisionEvent>();
        var characterTimelines = new Dictionary<string, List<TimePosition>>();

        // 1�ܰ�: �� ĳ������ �ð��� ��ġ ���
        foreach (var kvp in characterPaths)
        {
            string characterId = kvp.Key;
            List<Vector3Int> path = kvp.Value;

            Debug.Log($"Processing path for {characterId}: {string.Join(" -> ", path)}");

            var timeline = CalculateTimeline(path);
            characterTimelines[characterId] = timeline;
        }

        // 2�ܰ�: ��� �ð� �������� �浹 �˻�
        var allTimePoints = GetAllTimePoints(characterTimelines);
        Debug.Log($"Total time points to check: {allTimePoints.Count}");

        foreach (float time in allTimePoints)
        {
            var positionsAtTime = GetPositionsAtTime(characterTimelines, time);
            Debug.Log($"At time {time:F2}: {string.Join(", ", positionsAtTime.Select(kvp => $"{kvp.Key}={kvp.Value}"))}");

            var collision = CheckCollisionAtTime(positionsAtTime, time);

            if (collision.HasValue)
            {
                collisionEvents.Add(collision.Value);
            }
        }

        return collisionEvents;
    }

    /// <summary>
    /// ��θ� �ð��� ��ġ ������ ��ȯ
    /// </summary>
    private List<TimePosition> CalculateTimeline(List<Vector3Int> path)
    {
        Debug.Log($"CalculateTimeline called with path: {string.Join(" -> ", path)}");

        var timeline = new List<TimePosition>();

        if (path.Count == 0)
        {
            Debug.LogWarning("Empty path provided");
            return timeline;
        }

        float currentTime = 0f;
        Vector3Int currentPos = path[0];

        timeline.Add(new TimePosition(currentTime, currentPos));
        Debug.Log($"Added timeline point: Time={currentTime}, Pos={currentPos}");

        for (int i = 1; i < path.Count; i++)
        {
            Vector3Int nextPos = path[i];
            float distance = Vector3Int.Distance(currentPos, nextPos);
            float segmentTime = distance / moveSpeed;

            currentTime += segmentTime;
            timeline.Add(new TimePosition(currentTime, nextPos));

            Debug.Log($"Added timeline point: Time={currentTime:F2}, Pos={nextPos}, Distance={distance}, SegmentTime={segmentTime:F2}");

            currentPos = nextPos;
        }

        Debug.Log($"Timeline complete: {timeline.Count} points, total time: {currentTime:F2}s");
        return timeline;
    }

    /// <summary>
    /// Ư�� �ð��� Ư�� ��ġ�� �ִ� ĳ���� ã��
    /// </summary>
    private Vector3Int GetPositionAtTime(List<TimePosition> timeline, float targetTime)
    {
        Debug.Log($"GetPositionAtTime called: targetTime={targetTime:F2}");

        if (timeline.Count == 0) return Vector3Int.zero;
        if (targetTime <= timeline[0].time)
        {
            Debug.Log($"Before first point, returning {timeline[0].position}");
            return timeline[0].position;
        }

        for (int i = 1; i < timeline.Count; i++)
        {
            if (targetTime <= timeline[i].time)
            {
                var prevPoint = timeline[i - 1];
                var nextPoint = timeline[i];

                float segmentProgress = (targetTime - prevPoint.time) / (nextPoint.time - prevPoint.time);

                Debug.Log($"Between points: {prevPoint.position} (t={prevPoint.time:F2}) -> {nextPoint.position} (t={nextPoint.time:F2})");
                Debug.Log($"Segment progress: {segmentProgress:F2}");

                if (segmentProgress < 1f)
                {
                    Debug.Log($"Moving, returning start position: {prevPoint.position}");
                    return prevPoint.position; // �̵� �߿��� ����� ��ǥ
                }
                else
                {
                    Debug.Log($"Arrived, returning end position: {nextPoint.position}");
                    return nextPoint.position; // ���������� ������ ��ǥ
                }
            }
        }

        Debug.Log($"After last point, returning {timeline[timeline.Count - 1].position}");
        return timeline[timeline.Count - 1].position;
    }

    /// <summary>
    /// ��� ĳ���� Ÿ�Ӷ��ο��� �ǹ��ִ� �ð� ������ ����
    /// </summary>
    private HashSet<float> GetAllTimePoints(Dictionary<string, List<TimePosition>> timelines)
{
    var timePoints = new HashSet<float>();
    
    // ���� Ÿ�Ӷ��� ������ �߰�
    foreach (var timeline in timelines.Values)
    {
        foreach (var point in timeline)
        {
            timePoints.Add(point.time);
        }
    }
    
    // �ִ� �ð� ���
    float maxTime = 0f;
    foreach (var timeline in timelines.Values)
    {
        if (timeline.Count > 0)
        {
            float lastTime = timeline[timeline.Count - 1].time;
            if (lastTime > maxTime) maxTime = lastTime;
        }
    }
    
    // 0.2�� �������� �߰� ������ �߰�
    for (float t = 0f; t <= maxTime; t += 0.2f)
    {
        timePoints.Add(t);
    }
    
    return timePoints;
}
    /// <summary>
    /// Ư�� �ð��� ��� ĳ������ ��ġ ���
    /// </summary>
    private Dictionary<string, Vector3Int> GetPositionsAtTime(
        Dictionary<string, List<TimePosition>> timelines, float time)
    {
        var positions = new Dictionary<string, Vector3Int>();

        foreach (var kvp in timelines)
        {
            string characterId = kvp.Key;
            var timeline = kvp.Value;

            Vector3Int position = GetPositionAtTime(timeline, time);
            positions[characterId] = position;
        }

        return positions;
    }

    /// <summary>
    /// Ư�� �ð��� �浹 �˻�
    /// </summary>
    private CollisionEvent? CheckCollisionAtTime(Dictionary<string, Vector3Int> positions, float time)
    {
        var positionGroups = new Dictionary<Vector3Int, List<string>>();

        // ���� ��ġ�� �ִ� ĳ���͵� �׷�ȭ
        foreach (var kvp in positions)
        {
            string characterId = kvp.Key;
            Vector3Int position = kvp.Value;

            if (!positionGroups.ContainsKey(position))
                positionGroups[position] = new List<string>();

            positionGroups[position].Add(characterId);
        }

        // 2�� �̻��� ���� ��ġ�� ������ �浹
        foreach (var kvp in positionGroups)
        {
            if (kvp.Value.Count > 1)
            {
                return new CollisionEvent(time, kvp.Key, kvp.Value);
            }
        }

        return null;
    }

    /// <summary>
    /// �ð��� ��ġ ���� ����ü
    /// </summary>
    [System.Serializable]
    private struct TimePosition
    {
        public float time;
        public Vector3Int position;

        public TimePosition(float t, Vector3Int pos)
        {
            time = t;
            position = pos;
        }
    }

    /// <summary>
    /// �����: �浹 ���� ��� ���
    /// </summary>
    public void DebugPrintCollisions(List<CollisionEvent> collisions)
    {
        if (collisions.Count == 0)
        {
            Debug.Log("No collisions predicted!");
            return;
        }

        Debug.Log($"Predicted {collisions.Count} collision events:");
        foreach (var collision in collisions)
        {
            Debug.Log($"Time {collision.time:F2}s at {collision.position}: " +
                     $"{string.Join(", ", collision.characterIds)}");
        }
    }
}