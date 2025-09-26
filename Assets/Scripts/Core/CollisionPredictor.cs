using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 캐릭터 이동 전 충돌을 사전 계산하는 시스템
/// </summary>
public class CollisionPredictor : MonoBehaviour
{
    [System.Serializable]
    public struct CollisionEvent
    {
        public float time;              // 충돌 발생 시간
        public Vector3Int position;     // 충돌 발생 위치
        public List<string> characterIds; // 충돌하는 캐릭터들

        public CollisionEvent(float t, Vector3Int pos, List<string> ids)
        {
            time = t;
            position = pos;
            characterIds = new List<string>(ids);
        }
    }

    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;

    // 참조
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
    }

    /// <summary>
    /// 모든 캐릭터의 경로를 분석하여 충돌 이벤트들을 계산
    /// </summary>
    public List<CollisionEvent> PredictCollisions(Dictionary<string, List<Vector3Int>> characterPaths)
    {
        Debug.Log($"PredictCollisions called with {characterPaths.Count} characters");

        var collisionEvents = new List<CollisionEvent>();
        var characterTimelines = new Dictionary<string, List<TimePosition>>();

        // 1단계: 각 캐릭터의 시간별 위치 계산
        foreach (var kvp in characterPaths)
        {
            string characterId = kvp.Key;
            List<Vector3Int> path = kvp.Value;

            Debug.Log($"Processing path for {characterId}: {string.Join(" -> ", path)}");

            var timeline = CalculateTimeline(path);
            characterTimelines[characterId] = timeline;
        }

        // 2단계: 모든 시간 지점에서 충돌 검사
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
    /// 경로를 시간별 위치 정보로 변환
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
    /// 특정 시간에 특정 위치에 있는 캐릭터 찾기
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
                    return prevPoint.position; // 이동 중에는 출발점 좌표
                }
                else
                {
                    Debug.Log($"Arrived, returning end position: {nextPoint.position}");
                    return nextPoint.position; // 도착했으면 도착점 좌표
                }
            }
        }

        Debug.Log($"After last point, returning {timeline[timeline.Count - 1].position}");
        return timeline[timeline.Count - 1].position;
    }

    /// <summary>
    /// 모든 캐릭터 타임라인에서 의미있는 시간 지점들 추출
    /// </summary>
    private HashSet<float> GetAllTimePoints(Dictionary<string, List<TimePosition>> timelines)
{
    var timePoints = new HashSet<float>();
    
    // 기존 타임라인 지점들 추가
    foreach (var timeline in timelines.Values)
    {
        foreach (var point in timeline)
        {
            timePoints.Add(point.time);
        }
    }
    
    // 최대 시간 계산
    float maxTime = 0f;
    foreach (var timeline in timelines.Values)
    {
        if (timeline.Count > 0)
        {
            float lastTime = timeline[timeline.Count - 1].time;
            if (lastTime > maxTime) maxTime = lastTime;
        }
    }
    
    // 0.2초 간격으로 중간 지점들 추가
    for (float t = 0f; t <= maxTime; t += 0.2f)
    {
        timePoints.Add(t);
    }
    
    return timePoints;
}
    /// <summary>
    /// 특정 시간에 모든 캐릭터의 위치 계산
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
    /// 특정 시간에 충돌 검사
    /// </summary>
    private CollisionEvent? CheckCollisionAtTime(Dictionary<string, Vector3Int> positions, float time)
    {
        var positionGroups = new Dictionary<Vector3Int, List<string>>();

        // 같은 위치에 있는 캐릭터들 그룹화
        foreach (var kvp in positions)
        {
            string characterId = kvp.Key;
            Vector3Int position = kvp.Value;

            if (!positionGroups.ContainsKey(position))
                positionGroups[position] = new List<string>();

            positionGroups[position].Add(characterId);
        }

        // 2명 이상이 같은 위치에 있으면 충돌
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
    /// 시간별 위치 정보 구조체
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
    /// 디버그: 충돌 예측 결과 출력
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