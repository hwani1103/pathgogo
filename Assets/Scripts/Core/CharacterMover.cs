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

    // 캐시된 참조들
    private GridVisualizer gridVisualizer;
    private GamePiece gamePiece;
    private MovementGameManager cachedGameManager;
    private LevelLoader cachedLevelLoader;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        gamePiece = GetComponent<GamePiece>();

        // 캐시 초기화
        cachedGameManager = FindFirstObjectByType<MovementGameManager>();
        cachedLevelLoader = FindFirstObjectByType<LevelLoader>();
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

            // 매 프레임 충돌 체크 (수동 감지)
            CheckCollisionWithOtherCharacters();

            yield return null;
        }

        transform.position = toWorld;
    }

    /// <summary>
    /// 수동 충돌 감지 시스템
    /// </summary>
    private void CheckCollisionWithOtherCharacters()
    {
        if (cachedGameManager == null || !cachedGameManager.IsGameInProgress())
            return;

        if (cachedLevelLoader == null || gamePiece == null)
            return;

        var myPos = transform.position;
        var allCharacters = cachedLevelLoader.GetSpawnedCharacters();

        foreach (var otherCharacter in allCharacters)
        {
            if (otherCharacter == gamePiece) continue;

            // 상대방도 움직이고 있는지 확인
            var otherMover = otherCharacter.GetComponent<CharacterMover>();
            if (otherMover == null || !otherMover.IsMoving()) continue;

            var otherPos = otherCharacter.transform.position;
            float distance = Vector3.Distance(myPos, otherPos);

            // 충돌 임계값 체크
            if (distance < 0.4f)
            {
                Vector3 collisionPoint = (myPos + otherPos) * 0.5f;
                cachedGameManager.OnCharacterCollision(
                    gamePiece.GetCharacterId(),
                    otherCharacter.GetCharacterId(),
                    collisionPoint
                );
                return;
            }
        }
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