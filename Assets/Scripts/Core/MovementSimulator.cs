using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 이동을 시뮬레이션하여 시간별 위치를 계산하는 시스템
/// 충돌 감지를 위한 정확한 위치 추적을 제공합니다
/// </summary>
public class MovementSimulator : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float defaultMoveSpeed = 2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Simulation Settings")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float pauseTimeAtWaypoint = 0.1f; // 경로점에서 잠시 정지 시간

    // 참조
    private GridVisualizer gridVisualizer;
    private LevelData currentLevelData;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
    }

    /// <summary>
    /// 레벨 데이터 설정
    /// </summary>
    public void SetLevelData(LevelData levelData)
    {
        currentLevelData = levelData;
        if (currentLevelData != null)
            defaultMoveSpeed = currentLevelData.moveSpeed;
    }

    /// <summary>
    /// 경로를 따라 시간별 위치를 시뮬레이션
    /// </summary>
    public List<Vector3> SimulateMovement(List<Vector3Int> gridPath, float timeStep, float? customSpeed = null)
    {
        if (gridPath == null || gridPath.Count < 2)
            return new List<Vector3>();

        float moveSpeed = customSpeed ?? defaultMoveSpeed;
        var timeline = new List<Vector3>();

        // 그리드 좌표를 월드 좌표로 변환
        var worldPath = ConvertToWorldPath(gridPath);

        // 각 구간별 이동 시뮬레이션
        Vector3 currentPosition = worldPath[0];
        timeline.Add(currentPosition);

        float totalTime = 0f;

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 startPos = worldPath[i];
            Vector3 endPos = worldPath[i + 1];

            // 구간별 이동 시뮬레이션
            var segmentTimeline = SimulateSegmentMovement(startPos, endPos, moveSpeed, timeStep, totalTime);

            // 첫 번째 위치는 중복이므로 제외 (이전 구간의 마지막 위치)
            for (int j = 1; j < segmentTimeline.Count; j++)
            {
                timeline.Add(segmentTimeline[j]);
                totalTime += timeStep;
            }

            // 경로점에서 잠시 정지 (마지막 구간이 아닌 경우)
            if (i < worldPath.Count - 2 && pauseTimeAtWaypoint > 0)
            {
                int pauseSteps = Mathf.RoundToInt(pauseTimeAtWaypoint / timeStep);
                for (int pause = 0; pause < pauseSteps; pause++)
                {
                    timeline.Add(endPos);
                    totalTime += timeStep;
                }
            }
        }

        return timeline;
    }

    /// <summary>
    /// 두 지점 간의 이동을 시간별로 시뮬레이션
    /// </summary>
    private List<Vector3> SimulateSegmentMovement(Vector3 startPos, Vector3 endPos, float speed, float timeStep, float startTime)
    {
        var segmentTimeline = new List<Vector3>();

        float distance = Vector3.Distance(startPos, endPos);
        float totalMoveTime = distance / speed;

        if (totalMoveTime <= 0)
        {
            segmentTimeline.Add(startPos);
            segmentTimeline.Add(endPos);
            return segmentTimeline;
        }

        int steps = Mathf.CeilToInt(totalMoveTime / timeStep) + 1;

        for (int step = 0; step < steps; step++)
        {
            float t = Mathf.Clamp01((step * timeStep) / totalMoveTime);

            Vector3 position;
            if (useSmoothing)
            {
                // 애니메이션 커브 적용
                float smoothT = movementCurve.Evaluate(t);
                position = Vector3.Lerp(startPos, endPos, smoothT);
            }
            else
            {
                // 선형 보간
                position = Vector3.Lerp(startPos, endPos, t);
            }

            segmentTimeline.Add(position);
        }

        // 마지막 위치가 정확히 목표점이 되도록 보장
        if (segmentTimeline.Count > 0)
            segmentTimeline[segmentTimeline.Count - 1] = endPos;

        return segmentTimeline;
    }

    /// <summary>
    /// 그리드 경로를 월드 좌표 경로로 변환
    /// </summary>
    private List<Vector3> ConvertToWorldPath(List<Vector3Int> gridPath)
    {
        var worldPath = new List<Vector3>();

        foreach (var gridPos in gridPath)
        {
            Vector3 worldPos;
            if (gridVisualizer != null)
            {
                worldPos = gridVisualizer.GridToWorldPosition(gridPos);
            }
            else
            {
                // GridVisualizer가 없는 경우 기본 변환
                worldPos = new Vector3(gridPos.x, gridPos.y, 0);
            }
            worldPath.Add(worldPos);
        }

        return worldPath;
    }

    /// <summary>
    /// 특정 시간에서의 캐릭터 위치 계산 (단일 쿼리용)
    /// </summary>
    public Vector3 GetPositionAtTime(List<Vector3Int> gridPath, float targetTime, float? customSpeed = null)
    {
        var timeline = SimulateMovement(gridPath, 0.1f, customSpeed);

        if (timeline.Count == 0)
            return Vector3.zero;

        int timeIndex = Mathf.RoundToInt(targetTime / 0.1f);

        if (timeIndex >= timeline.Count)
            return timeline[timeline.Count - 1]; // 경로 완료 후에는 마지막 위치

        return timeline[Mathf.Max(0, timeIndex)];
    }

    /// <summary>
    /// 경로의 총 이동 시간 계산
    /// </summary>
    public float CalculateTotalMoveTime(List<Vector3Int> gridPath, float? customSpeed = null)
    {
        if (gridPath == null || gridPath.Count < 2)
            return 0f;

        float moveSpeed = customSpeed ?? defaultMoveSpeed;
        var worldPath = ConvertToWorldPath(gridPath);
        float totalTime = 0f;

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            float distance = Vector3.Distance(worldPath[i], worldPath[i + 1]);
            totalTime += distance / moveSpeed;

            // 경로점에서의 정지 시간 추가 (마지막 구간이 아닌 경우)
            if (i < worldPath.Count - 2)
                totalTime += pauseTimeAtWaypoint;
        }

        return totalTime;
    }

    /// <summary>
    /// 두 캐릭터의 이동 타임라인을 비교하여 가장 가까운 거리의 시점 찾기
    /// </summary>
    public float FindClosestApproachTime(List<Vector3Int> path1, List<Vector3Int> path2, float timeStep = 0.1f)
    {
        var timeline1 = SimulateMovement(path1, timeStep);
        var timeline2 = SimulateMovement(path2, timeStep);

        float closestTime = 0f;
        float minDistance = float.MaxValue;

        int maxSteps = Mathf.Max(timeline1.Count, timeline2.Count);

        for (int i = 0; i < maxSteps; i++)
        {
            Vector3 pos1 = i < timeline1.Count ? timeline1[i] : timeline1[timeline1.Count - 1];
            Vector3 pos2 = i < timeline2.Count ? timeline2[i] : timeline2[timeline2.Count - 1];

            float distance = Vector3.Distance(pos1, pos2);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestTime = i * timeStep;
            }
        }

        return closestTime;
    }

    /// <summary>
    /// 경로의 각 구간별 속도를 다르게 설정하여 시뮬레이션 (고급 기능)
    /// </summary>
    public List<Vector3> SimulateMovementWithVariableSpeed(List<Vector3Int> gridPath, List<float> segmentSpeeds, float timeStep)
    {
        if (gridPath == null || gridPath.Count < 2 || segmentSpeeds == null || segmentSpeeds.Count != gridPath.Count - 1)
            return SimulateMovement(gridPath, timeStep);

        var timeline = new List<Vector3>();
        var worldPath = ConvertToWorldPath(gridPath);

        Vector3 currentPosition = worldPath[0];
        timeline.Add(currentPosition);
        float totalTime = 0f;

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 startPos = worldPath[i];
            Vector3 endPos = worldPath[i + 1];
            float segmentSpeed = segmentSpeeds[i];

            var segmentTimeline = SimulateSegmentMovement(startPos, endPos, segmentSpeed, timeStep, totalTime);

            for (int j = 1; j < segmentTimeline.Count; j++)
            {
                timeline.Add(segmentTimeline[j]);
                totalTime += timeStep;
            }
        }

        return timeline;
    }

    /// <summary>
    /// 디버그용: 시뮬레이션 결과 시각화
    /// </summary>
    public void DebugDrawMovementPreview(List<Vector3Int> gridPath, float previewDuration = 2f)
    {
        if (gridPath == null || gridPath.Count < 2) return;

        var timeline = SimulateMovement(gridPath, 0.1f);

        // 경로 라인 그리기
        var worldPath = ConvertToWorldPath(gridPath);
        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Debug.DrawLine(worldPath[i], worldPath[i + 1], Color.green, previewDuration);
        }

        // 시작점과 끝점 표시
        if (worldPath.Count > 0)
        {
            Debug.DrawRay(worldPath[0], Vector3.up * 0.5f, Color.blue, previewDuration);
            Debug.DrawRay(worldPath[worldPath.Count - 1], Vector3.up * 0.5f, Color.red, previewDuration);
        }

        Debug.Log($"Movement simulation: {timeline.Count} time steps, Total time: {CalculateTotalMoveTime(gridPath):F2}s");
    }

#if UNITY_EDITOR
    [ContextMenu("Test Movement Simulation")]
    public void TestMovementSimulation()
    {
        var testPath = new List<Vector3Int>
        {
            new Vector3Int(0, 0, 0),
            new Vector3Int(3, 0, 0),
            new Vector3Int(3, 3, 0)
        };

        Debug.Log("Testing movement simulation...");
        DebugDrawMovementPreview(testPath, 5f);

        float totalTime = CalculateTotalMoveTime(testPath);
        Debug.Log($"Total movement time: {totalTime:F2} seconds");
    }
#endif
}