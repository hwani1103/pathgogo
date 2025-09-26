using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMover : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private AnimationCurve movementCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private bool isMoving = false;
    private List<Vector3Int> currentPath;
    private Coroutine movementCoroutine;

    private GridVisualizer gridVisualizer;
    private GamePiece gamePiece;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        gamePiece = GetComponent<GamePiece>();
    }

    public void StartMovement(List<Vector3Int> path)
    {
        if (isMoving)
        {
            StopMovement();
        }

        currentPath = new List<Vector3Int>(path);
        movementCoroutine = StartCoroutine(MoveAlongPath());
    }

    public void StopMovement()
    {
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        isMoving = false;
    }

    private IEnumerator MoveAlongPath()
    {
        if (currentPath == null || currentPath.Count < 2)
        {
            Debug.LogWarning($"Invalid path for character {gamePiece.GetCharacterId()}");
            yield break;
        }

        isMoving = true;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3Int fromPos = currentPath[i];
            Vector3Int toPos = currentPath[i + 1];

            yield return StartCoroutine(MoveToPosition(fromPos, toPos));
        }

        isMoving = false;
        OnMovementComplete();
    }

    private IEnumerator MoveToPosition(Vector3Int fromGrid, Vector3Int toGrid)
    {
        Vector3 fromWorld = gridVisualizer.GridToWorldPosition(fromGrid);
        Vector3 toWorld = gridVisualizer.GridToWorldPosition(toGrid);

        float distance = Vector3.Distance(fromWorld, toWorld);
        float duration = distance / moveSpeed;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;

            float curveProgress = movementCurve.Evaluate(progress);

            Vector3 currentPosition = Vector3.Lerp(fromWorld, toWorld, curveProgress);
            transform.position = currentPosition;

            yield return null;
        }

        transform.position = toWorld;
    }

    public Vector3Int GetPositionAtTime(float targetTime)
    {
        if (currentPath == null || currentPath.Count == 0)
            return gamePiece.GetCurrentGridPosition();

        float currentTime = 0f;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3Int fromPos = currentPath[i];
            Vector3Int toPos = currentPath[i + 1];

            float distance = Vector3Int.Distance(fromPos, toPos);
            float segmentDuration = distance / moveSpeed;

            if (targetTime <= currentTime + segmentDuration)
            {
                float segmentProgress = (targetTime - currentTime) / segmentDuration;

                if (segmentProgress < 1f)
                {
                    return fromPos;
                }
                else
                {
                    return toPos;
                }
            }

            currentTime += segmentDuration;
        }

        return currentPath[currentPath.Count - 1];
    }

    public bool IsMoving()
    {
        return isMoving;
    }

    public List<Vector3Int> GetCurrentPath()
    {
        return currentPath != null ? new List<Vector3Int>(currentPath) : new List<Vector3Int>();
    }

    public void SetMoveSpeed(float speed)
    {
        moveSpeed = speed;
    }

    private void OnMovementComplete()
    {
        Debug.Log($"Character {gamePiece.GetCharacterId()} completed movement");

        Vector3Int finalPosition = currentPath[currentPath.Count - 1];
    }

    public void TeleportToPosition(Vector3Int gridPosition)
    {
        StopMovement();
        Vector3 worldPosition = gridVisualizer.GridToWorldPosition(gridPosition);
        transform.position = worldPosition;
    }

    public void DebugDrawPath()
    {
        if (currentPath == null || currentPath.Count < 2) return;

        for (int i = 0; i < currentPath.Count - 1; i++)
        {
            Vector3 from = gridVisualizer.GridToWorldPosition(currentPath[i]);
            Vector3 to = gridVisualizer.GridToWorldPosition(currentPath[i + 1]);

            Debug.DrawLine(from, to, Color.cyan, 2f);
        }
    }
}