using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ���� �̵��� �ùķ��̼��Ͽ� �ð��� ��ġ�� ����ϴ� �ý���
/// �浹 ������ ���� ��Ȯ�� ��ġ ������ �����մϴ�
/// </summary>
public class MovementSimulator : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float defaultMoveSpeed = 2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Simulation Settings")]
    [SerializeField] private bool useSmoothing = true;
    [SerializeField] private float pauseTimeAtWaypoint = 0.1f; // ��������� ��� ���� �ð�

    // ����
    private GridVisualizer gridVisualizer;
    private LevelData currentLevelData;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
    }

    /// <summary>
    /// ���� ������ ����
    /// </summary>
    public void SetLevelData(LevelData levelData)
    {
        currentLevelData = levelData;
        if (currentLevelData != null)
            defaultMoveSpeed = currentLevelData.moveSpeed;
    }

    /// <summary>
    /// ��θ� ���� �ð��� ��ġ�� �ùķ��̼�
    /// </summary>
    public List<Vector3> SimulateMovement(List<Vector3Int> gridPath, float timeStep, float? customSpeed = null)
    {
        if (gridPath == null || gridPath.Count < 2)
            return new List<Vector3>();

        float moveSpeed = customSpeed ?? defaultMoveSpeed;
        var timeline = new List<Vector3>();

        // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ
        var worldPath = ConvertToWorldPath(gridPath);

        // �� ������ �̵� �ùķ��̼�
        Vector3 currentPosition = worldPath[0];
        timeline.Add(currentPosition);

        float totalTime = 0f;

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 startPos = worldPath[i];
            Vector3 endPos = worldPath[i + 1];

            // ������ �̵� �ùķ��̼�
            var segmentTimeline = SimulateSegmentMovement(startPos, endPos, moveSpeed, timeStep, totalTime);

            // ù ��° ��ġ�� �ߺ��̹Ƿ� ���� (���� ������ ������ ��ġ)
            for (int j = 1; j < segmentTimeline.Count; j++)
            {
                timeline.Add(segmentTimeline[j]);
                totalTime += timeStep;
            }

            // ��������� ��� ���� (������ ������ �ƴ� ���)
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
    /// �� ���� ���� �̵��� �ð����� �ùķ��̼�
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
                // �ִϸ��̼� Ŀ�� ����
                float smoothT = movementCurve.Evaluate(t);
                position = Vector3.Lerp(startPos, endPos, smoothT);
            }
            else
            {
                // ���� ����
                position = Vector3.Lerp(startPos, endPos, t);
            }

            segmentTimeline.Add(position);
        }

        // ������ ��ġ�� ��Ȯ�� ��ǥ���� �ǵ��� ����
        if (segmentTimeline.Count > 0)
            segmentTimeline[segmentTimeline.Count - 1] = endPos;

        return segmentTimeline;
    }

    /// <summary>
    /// �׸��� ��θ� ���� ��ǥ ��η� ��ȯ
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
                // GridVisualizer�� ���� ��� �⺻ ��ȯ
                worldPos = new Vector3(gridPos.x, gridPos.y, 0);
            }
            worldPath.Add(worldPos);
        }

        return worldPath;
    }

    /// <summary>
    /// Ư�� �ð������� ĳ���� ��ġ ��� (���� ������)
    /// </summary>
    public Vector3 GetPositionAtTime(List<Vector3Int> gridPath, float targetTime, float? customSpeed = null)
    {
        var timeline = SimulateMovement(gridPath, 0.1f, customSpeed);

        if (timeline.Count == 0)
            return Vector3.zero;

        int timeIndex = Mathf.RoundToInt(targetTime / 0.1f);

        if (timeIndex >= timeline.Count)
            return timeline[timeline.Count - 1]; // ��� �Ϸ� �Ŀ��� ������ ��ġ

        return timeline[Mathf.Max(0, timeIndex)];
    }

    /// <summary>
    /// ����� �� �̵� �ð� ���
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

            // ����������� ���� �ð� �߰� (������ ������ �ƴ� ���)
            if (i < worldPath.Count - 2)
                totalTime += pauseTimeAtWaypoint;
        }

        return totalTime;
    }

    /// <summary>
    /// �� ĳ������ �̵� Ÿ�Ӷ����� ���Ͽ� ���� ����� �Ÿ��� ���� ã��
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
    /// ����� �� ������ �ӵ��� �ٸ��� �����Ͽ� �ùķ��̼� (��� ���)
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
    /// ����׿�: �ùķ��̼� ��� �ð�ȭ
    /// </summary>
    public void DebugDrawMovementPreview(List<Vector3Int> gridPath, float previewDuration = 2f)
    {
        if (gridPath == null || gridPath.Count < 2) return;

        var timeline = SimulateMovement(gridPath, 0.1f);

        // ��� ���� �׸���
        var worldPath = ConvertToWorldPath(gridPath);
        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Debug.DrawLine(worldPath[i], worldPath[i + 1], Color.green, previewDuration);
        }

        // �������� ���� ǥ��
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