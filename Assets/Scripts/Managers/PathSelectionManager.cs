using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터의 경로 선택을 관리하는 시스템
/// 새로운 아키텍처: PathValidator, CollisionDetector, MovementSimulator와 연동
/// </summary>
public class PathSelectionManager : MonoBehaviour
{
    [Header("Visual Settings")]
    [SerializeField] private Material pathLineMaterial;
    [SerializeField] private Color availablePathColor = Color.yellow;
    [SerializeField] private Color selectedPathColor = Color.green;
    [SerializeField] private Color invalidPathColor = Color.red;
    [SerializeField] private float lineWidth = 0.1f;

    [Header("Flag Settings")]
    [SerializeField] private GameObject flagPrefab;
    [SerializeField] private float flagScale = 0.5f;

    [Header("Collision Feedback")]
    [SerializeField] private bool showCollisionWarnings = true;
    [SerializeField] private Color collisionWarningColor = Color.red;

    // 현재 선택된 캐릭터
    private CharacterController currentCharacter;

    // 경로 관리
    private List<Vector3Int> currentPath = new List<Vector3Int>();
    private List<GameObject> pathFlags = new List<GameObject>();
    private List<LineRenderer> availablePathLines = new List<LineRenderer>();
    private LineRenderer finalPathLine;

    // 새로운 아키텍처 참조
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;

    // 기존 참조
    private GridVisualizer gridVisualizer;
    private LevelLoader levelLoader;
    private TouchInputManager touchInputManager;

    void Awake()
    {
        // 기존 참조
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        levelLoader = FindFirstObjectByType<LevelLoader>();
        touchInputManager = FindFirstObjectByType<TouchInputManager>();

        // 새 시스템 참조 (같은 GameObject에 있다고 가정)
        pathValidator = GetComponent<PathValidator>();
        collisionDetector = GetComponent<CollisionDetector>();
        movementSimulator = GetComponent<MovementSimulator>();

        // 없으면 자동 생성
        if (pathValidator == null)
            pathValidator = gameObject.AddComponent<PathValidator>();
        if (collisionDetector == null)
            collisionDetector = gameObject.AddComponent<CollisionDetector>();
        if (movementSimulator == null)
            movementSimulator = gameObject.AddComponent<MovementSimulator>();

        // 최종 경로 Line Renderer 생성
        CreateFinalPathLineRenderer();
    }

    /// <summary>
    /// 캐릭터 선택 시 경로 선택 모드 시작
    /// </summary>
    public void StartPathSelection(CharacterController character)
    {
        if (character == null) return;

        currentCharacter = character;
        currentPath.Clear();

        // 캐릭터 시작 위치를 경로에 추가
        currentPath.Add(character.GetCurrentGridPosition());

        // 가능한 경로들 표시
        ShowAvailablePaths();
    }

    /// <summary>
    /// 경로 선택 모드 종료
    /// </summary>
    public void EndPathSelection()
    {
        currentCharacter = null;
        currentPath.Clear();

        ClearAllVisuals();
    }

    /// <summary>
    /// 특정 위치 선택 처리 (좌표 변환 수정됨)
    /// </summary>
    public void SelectPosition(Vector3Int gridPosition)
    {
        if (currentCharacter == null) return;

        Vector3Int currentPos = currentPath.Count > 0 ? currentPath[currentPath.Count - 1] : currentCharacter.GetCurrentGridPosition();

        // PathValidator를 사용한 검증
        if (!IsValidSelection(currentPos, gridPosition))
        {
            ShowInvalidSelectionFeedback(gridPosition);
            return;
        }

        // 경로에 추가
        currentPath.Add(gridPosition);

        // Flag 생성
        CreateFlag(gridPosition);

        // 남은 선택 횟수 확인
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        if (remainingSelections <= 0 || pathValidator.IsGoalPosition(gridPosition, currentCharacter))
        {
            // 경로 완성 - 충돌 검사 수행
            if (ValidateCompletePathWithCollision())
            {
                CompletePath();
            }
            else
            {
                // 충돌 발생 - 사용자에게 알림
                ShowCollisionWarning();
                // 마지막 선택을 취소할지 결정 (옵션)
                // UndoLastSelection();
            }
        }
        else
        {
            // 다음 선택을 위한 가능한 경로 업데이트
            ShowAvailablePaths();
        }
    }

    /// <summary>
    /// 유효한 선택인지 확인 (PathValidator 사용)
    /// </summary>
    private bool IsValidSelection(Vector3Int fromPos, Vector3Int toPos)
    {
        // 동서남북 직선상에 있는지 확인
        if (fromPos.x != toPos.x && fromPos.y != toPos.y)
        {
            Debug.Log("Invalid: Not cardinal direction");
            return false;
        }

        // 같은 위치가 아닌지 확인
        if (fromPos == toPos)
        {
            Debug.Log("Invalid: Same position");
            return false;
        }

        // PathValidator를 통한 검증
        return pathValidator.IsValidPosition(toPos) &&
               !pathValidator.HasObstacleInPath(fromPos, toPos, currentCharacter);
    }

    /// <summary>
    /// 현재 위치에서 가능한 모든 경로 표시 (PathValidator 사용)
    /// </summary>
    private void ShowAvailablePaths()
    {
        // 기존 라인들 정리
        ClearAvailablePathLines();

        if (currentCharacter == null || currentPath.Count == 0) return;

        Vector3Int currentPos = currentPath[currentPath.Count - 1];
        int remainingSelections = currentCharacter.GetRemainingSelections() - (currentPath.Count - 1);

        if (remainingSelections <= 0) return;

        // 동서남북 방향 확인
        Vector3Int[] directions = { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

        foreach (Vector3Int direction in directions)
        {
            ShowDirectionPath(currentPos, direction, remainingSelections);
        }
    }

    /// <summary>
    /// 특정 방향으로 가능한 경로 표시 (PathValidator 사용)
    /// </summary>
    private void ShowDirectionPath(Vector3Int startPos, Vector3Int direction, int remainingSelections)
    {
        // PathValidator를 사용하여 해당 방향의 유효한 위치들 가져오기
        List<Vector3Int> pathPositions = pathValidator.GetValidPositionsInDirection(
            startPos, direction, currentCharacter, 10
        );

        // 마지막-1 선택 제약 확인
        if (remainingSelections == 1)
        {
            pathPositions = pathValidator.FilterLastSelectionPositions(pathPositions, currentCharacter);
        }

        if (pathPositions.Count > 0)
        {
            CreateAvailablePathLine(startPos, pathPositions);
        }
    }

    /// <summary>
    /// 완성된 경로의 충돌 검사
    /// </summary>
    private bool ValidateCompletePathWithCollision()
    {
        if (currentPath.Count < 2 || collisionDetector == null) return true;

        // 경로 자체의 유효성 검사
        if (!pathValidator.ValidateCompletePath(currentPath, currentCharacter))
            return false;

        // 충돌 검사 수행
        var collisionResult = collisionDetector.CheckSingleCharacterPath(
            currentCharacter.GetCharacterId(),
            currentPath
        );

        if (collisionResult.hasCollision)
        {
            Debug.LogWarning($"Collision detected: {collisionDetector.GetCollisionMessage(collisionResult)}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 잘못된 선택에 대한 피드백 표시
    /// </summary>
    private void ShowInvalidSelectionFeedback(Vector3Int gridPosition)
    {
        // 진동 또는 사운드 효과 (모바일)
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }

        // 시각적 피드백
        StartCoroutine(ShowInvalidPositionEffect(gridPosition));

        Debug.Log($"Invalid selection at {gridPosition}");
    }

    /// <summary>
    /// 충돌 경고 표시
    /// </summary>
    private void ShowCollisionWarning()
    {
        if (!showCollisionWarnings) return;

        // UI 메시지나 시각적 피드백 표시
        Debug.LogWarning("Path would cause collision with other characters!");

        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }
    }

    /// <summary>
    /// 잘못된 위치 선택 시 시각 효과
    /// </summary>
    private System.Collections.IEnumerator ShowInvalidPositionEffect(Vector3Int gridPosition)
    {
        if (gridVisualizer == null) yield break;

        // 빨간색 임시 마커 생성
        GameObject tempMarker = new GameObject("InvalidMarker");
        tempMarker.transform.position = gridVisualizer.GridToWorldPosition(gridPosition);

        var renderer = tempMarker.AddComponent<SpriteRenderer>();
        renderer.sprite = CreateSimpleSquareSprite();
        renderer.color = invalidPathColor;
        renderer.sortingOrder = 25;

        // 크기 애니메이션
        Vector3 originalScale = Vector3.one * 0.5f;
        Vector3 targetScale = Vector3.one * 1.2f;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            tempMarker.transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
            renderer.color = Color.Lerp(invalidPathColor, Color.clear, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(tempMarker);
    }

    /// <summary>
    /// 간단한 정사각형 스프라이트 생성 (임시 마커용)
    /// </summary>
    private Sprite CreateSimpleSquareSprite()
    {
        Texture2D texture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, 32, 32), Vector2.one * 0.5f);
    }

    /// <summary>
    /// 가능한 경로 Line Renderer 생성 (기존 코드 유지하되 색상 로직 개선)
    /// </summary>
    private void CreateAvailablePathLine(Vector3Int startPos, List<Vector3Int> pathPositions)
    {
        GameObject lineObj = new GameObject("AvailablePath");
        LineRenderer line = lineObj.AddComponent<LineRenderer>();

        line.material = pathLineMaterial;
        line.startColor = availablePathColor;
        line.endColor = availablePathColor;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.positionCount = pathPositions.Count + 1;
        line.sortingOrder = 15;
        line.useWorldSpace = true;

        // 시작점 설정
        Vector3 startWorldPos = gridVisualizer.GridToWorldPosition(startPos);
        line.SetPosition(0, startWorldPos);

        // 경로 점들 설정
        for (int i = 0; i < pathPositions.Count; i++)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(pathPositions[i]);
            line.SetPosition(i + 1, worldPos);
        }

        availablePathLines.Add(line);
    }

    /// <summary>
    /// 경로 완성 처리
    /// </summary>
    private void CompletePath()
    {
        // 가능한 경로 라인들 숨기기
        ClearAvailablePathLines();

        // 최종 경로 표시
        ShowFinalPath();

        // MovementSimulator를 사용한 미리보기 (옵션)
        if (movementSimulator != null)
        {
            movementSimulator.DebugDrawMovementPreview(currentPath, 1f);
        }

        // 1초 후 정리
        Invoke(nameof(ClearFinalPath), 1f);

        pathValidator.DebugLogPath(currentPath, $"Completed path for {currentCharacter.GetCharacterId()}");
    }

    // === 기존 메서드들 (수정 최소화) ===

    /// <summary>
    /// Flag 생성
    /// </summary>
    private void CreateFlag(Vector3Int gridPosition)
    {
        if (flagPrefab == null) return;

        Vector3 worldPos = gridVisualizer.GridToWorldPosition(gridPosition);
        GameObject flag = Instantiate(flagPrefab, worldPos, Quaternion.identity);
        flag.transform.localScale = Vector3.one * flagScale;
        flag.name = $"Flag_{pathFlags.Count}";

        pathFlags.Add(flag);
    }

    /// <summary>
    /// 최종 경로 표시
    /// </summary>
    private void ShowFinalPath()
    {
        if (finalPathLine == null || currentPath.Count < 2) return;

        finalPathLine.gameObject.SetActive(true);
        finalPathLine.positionCount = currentPath.Count;

        for (int i = 0; i < currentPath.Count; i++)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(currentPath[i]);
            finalPathLine.SetPosition(i, worldPos);
        }
    }

    /// <summary>
    /// 최종 경로 라인 생성
    /// </summary>
    private void CreateFinalPathLineRenderer()
    {
        GameObject finalLineObj = new GameObject("FinalPath");
        finalLineObj.transform.SetParent(transform);

        finalPathLine = finalLineObj.AddComponent<LineRenderer>();
        finalPathLine.material = pathLineMaterial;
        finalPathLine.startColor = selectedPathColor;
        finalPathLine.endColor = selectedPathColor;
        finalPathLine.startWidth = lineWidth * 1.5f;
        finalPathLine.endWidth = lineWidth * 1.5f;
        finalPathLine.sortingOrder = 20;
        finalPathLine.useWorldSpace = true;
        finalPathLine.gameObject.SetActive(false);
    }

    /// <summary>
    /// 모든 시각적 요소 정리
    /// </summary>
    private void ClearAllVisuals()
    {
        ClearAvailablePathLines();
        ClearFlags();
        ClearFinalPath();
    }

    /// <summary>
    /// 가능한 경로 라인들 정리
    /// </summary>
    private void ClearAvailablePathLines()
    {
        foreach (var line in availablePathLines)
        {
            if (line != null)
                DestroyImmediate(line.gameObject);
        }
        availablePathLines.Clear();
    }

    /// <summary>
    /// Flag들 정리
    /// </summary>
    private void ClearFlags()
    {
        foreach (var flag in pathFlags)
        {
            if (flag != null)
                DestroyImmediate(flag.gameObject);
        }
        pathFlags.Clear();
    }

    /// <summary>
    /// 최종 경로 라인 정리
    /// </summary>
    private void ClearFinalPath()
    {
        if (finalPathLine != null)
            finalPathLine.gameObject.SetActive(false);
    }

    /// <summary>
    /// 현재 경로 반환
    /// </summary>
    public List<Vector3Int> GetCurrentPath()
    {
        return new List<Vector3Int>(currentPath);
    }

    /// <summary>
    /// 경로 선택 중인지 확인
    /// </summary>
    public bool IsSelectingPath()
    {
        return currentCharacter != null;
    }

    /// <summary>
    /// 현재 선택된 캐릭터 반환
    /// </summary>
    public CharacterController GetCurrentCharacter()
    {
        return currentCharacter;
    }
}