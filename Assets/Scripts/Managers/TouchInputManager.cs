using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 터치 입력을 처리하고 캐릭터 선택을 관리하는 시스템
/// 좌표 변환 로직이 GridVisualizer와 통합되도록 수정됨
/// </summary>
public class TouchInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float touchRadius = 0.5f;
    [SerializeField] private LayerMask touchLayerMask = -1;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showTouchPosition = true;

    // 입력 시스템
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    // 선택된 캐릭터
    private GamePiece selectedCharacter;

    // 참조
    private LevelLoader levelLoader;
    private GridVisualizer gridVisualizer;
    private PathSelectionManager pathSelectionManager;

    void Awake()
    {
        // 컴포넌트 참조 설정
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
        levelLoader = FindFirstObjectByType<LevelLoader>();
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        pathSelectionManager = FindFirstObjectByType<PathSelectionManager>();

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }

        if (gridVisualizer == null)
        {
            Debug.LogError("GridVisualizer not found! TouchInputManager needs GridVisualizer for coordinate conversion.");
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.TouchPress.performed += OnTouchPressed;
    }

    void OnDisable()
    {
        inputActions.Player.TouchPress.performed -= OnTouchPressed;
        inputActions.Disable();
    }

    private void OnTouchPressed(InputAction.CallbackContext context)
    {
        Vector2 touchPosition = inputActions.Player.Touch.ReadValue<Vector2>();
        ProcessTouch(touchPosition);
    }

    void OnDestroy()
    {
        inputActions?.Dispose();
    }

    /// <summary>
    /// 터치 입력 처리 (좌표 변환 로직 수정)
    /// </summary>
    private void ProcessTouch(Vector2 screenPosition)
    {
        if (mainCamera == null || gridVisualizer == null)
        {
            Debug.LogError("Required components not found!");
            return;
        }

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        worldPosition.z = 0;

        Vector3Int gridPosition = gridVisualizer.WorldToGridPosition(worldPosition);
        GamePiece touchedCharacter = FindCharacterAtPosition(worldPosition, gridPosition);


        if(touchedCharacter != null)
{
            if (selectedCharacter == touchedCharacter)
            {
                DeselectCharacter();
                return;
            }

            // 경로 선택 중이고 완료되지 않은 캐릭터가 선택되어 있을 때만 차단
            if (selectedCharacter != null &&
                !selectedCharacter.IsCompleted() &&
                pathSelectionManager != null &&
                pathSelectionManager.IsSelectingPath())
            {
                return; // 차단
            }

            SelectCharacter(touchedCharacter);
        }
        else
        {
            if (selectedCharacter != null && pathSelectionManager != null && pathSelectionManager.IsSelectingPath())
            {
                pathSelectionManager.SelectPosition(gridPosition);
            }
        }
    }
    /// <summary>
    /// 특정 위치에서 캐릭터 찾기 (검증 강화)
    /// </summary>
    private GamePiece FindCharacterAtPosition(Vector3 worldPosition, Vector3Int gridPosition)
    {
        if (levelLoader == null) return null;

        // 방법 1: 그리드 좌표로 정확히 찾기 (우선순위)
        GamePiece characterAtGrid = levelLoader.GetCharacterAt(gridPosition);
        if (characterAtGrid != null && !characterAtGrid.IsCompleted()) // 완료된 캐릭터는 선택 불가
        {
            if (showDebugInfo)
            {
                Debug.Log($"Found character {characterAtGrid.GetCharacterId()} at exact grid position {gridPosition}");
            }
            return characterAtGrid;
        }

        // 방법 2: 터치 반경 내에서 찾기 (관대한 터치)
        var allCharacters = levelLoader.GetSpawnedCharacters();
        GamePiece closestCharacter = null;
        float closestDistance = float.MaxValue;

        foreach (var character in allCharacters)
        {
            if (character.IsCompleted()) continue; // 완료된 캐릭터는 제외

            float distance = Vector3.Distance(worldPosition, character.transform.position);
            if (distance <= touchRadius && distance < closestDistance)
            {
                closestCharacter = character;
                closestDistance = distance;
            }
        }

        if (closestCharacter != null && showDebugInfo)
        {
            Debug.Log($"Found character {closestCharacter.GetCharacterId()} within touch radius. Distance: {closestDistance:F2}");
        }

        return closestCharacter;
    }
    /// <summary>
    /// 캐릭터 선택 가능 여부 확인
    /// </summary>
    private bool CanSelectCharacter(GamePiece character)
    {
        if (character == null) return false;

        // 이동 중인 캐릭터는 선택 불가
        if (character.IsMoving())
        {
            Debug.Log($"Character {character.GetCharacterId()} is currently moving and cannot be selected");
            return false;
        }

        // 남은 선택 횟수가 없으면 선택 불가
        if (character.GetRemainingSelections() <= 0)
        {
            Debug.Log($"Character {character.GetCharacterId()} has no remaining selections");
            return false;
        }

        // 다른 캐릭터가 경로 선택 중일 때의 제약 검사
        if (pathSelectionManager != null && pathSelectionManager.IsSelectingPath())
        {
            var currentSelectingCharacter = pathSelectionManager.GetCurrentCharacter();
            if (currentSelectingCharacter != null && currentSelectingCharacter != character)
            {
                Debug.Log($"Cannot select character {character.GetCharacterId()} while {currentSelectingCharacter.GetCharacterId()} is selecting path");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 캐릭터 선택이 차단되었을 때의 피드백
    /// </summary>
    private void ShowCharacterSelectionBlocked(GamePiece character)
    {
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }

        Debug.Log($"Character {character.GetCharacterId()} selection blocked");

        // 시각적 피드백 (예: 캐릭터 주변에 빨간 테두리 효과)
        StartCoroutine(ShowBlockedSelectionEffect(character));
    }

    /// <summary>
    /// 선택 차단 시각 효과
    /// </summary>
    private System.Collections.IEnumerator ShowBlockedSelectionEffect(GamePiece character)
    {
        if (character == null) yield break;

        // 기존 색상 백업
        var spriteRenderer = character.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Color blockedColor = Color.red;

        // 색상 깜빡임 효과
        float duration = 0.5f;
        int blinkCount = 3;

        for (int i = 0; i < blinkCount; i++)
        {
            // 빨간색으로
            spriteRenderer.color = blockedColor;
            yield return new WaitForSeconds(duration / (blinkCount * 2));

            // 원래 색으로
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(duration / (blinkCount * 2));
        }
    }

    /// <summary>
    /// 캐릭터 선택 처리
    /// </summary>
    private void SelectCharacter(GamePiece character)
    {
        // 이미 선택된 캐릭터와 같다면 선택 해제
        if (selectedCharacter == character)
        {
            DeselectCharacter();
            return;
        }

        // 기존 선택 해제
        if (selectedCharacter != null)
        {
            selectedCharacter.SetSelected(false);
        }

        // 새 캐릭터 선택
        selectedCharacter = character;
        selectedCharacter.SetSelected(true);

        if (showDebugInfo)
        {
            Debug.Log($"Character {selectedCharacter.GetCharacterId()} selected");
            selectedCharacter.LogCharacterInfo();
        }

        // PathSelectionManager에게 경로 선택 시작 요청
        if (pathSelectionManager != null)
        {
            pathSelectionManager.StartPathSelection(selectedCharacter);
        }

        // 선택된 캐릭터의 목표 Goal 강조
        HighlightAssignedGoal();
    }

    /// <summary>
    /// 캐릭터 선택 해제
    /// </summary>
    private void DeselectCharacter()
    {
        if (selectedCharacter != null)
        {
            selectedCharacter.SetSelected(false);

            if (showDebugInfo)
            {
                Debug.Log($"Character {selectedCharacter.GetCharacterId()} deselected");
            }

            selectedCharacter = null;
        }

        if (pathSelectionManager != null)
        {
            pathSelectionManager.EndPathSelection();
        }

        ClearGoalHighlight();

    }

    /// <summary>
    /// 선택된 캐릭터의 목표 Goal 강조
    /// </summary>
    private void HighlightAssignedGoal()
    {
        if (selectedCharacter == null || levelLoader == null) return;

        // 모든 Goal을 기본 크기(1.0f)로 복원
        var allGoals = levelLoader.GetSpawnedGoals();
        foreach (var goal in allGoals)
        {
            goal.transform.localScale = Vector3.one;
        }

        // 선택된 캐릭터가 사용할 수 있는 Goal만 크게 만들기
        foreach (var goal in allGoals)
        {
            if (goal.CanUseGoal(selectedCharacter.GetCharacterId()))
            {
                goal.transform.localScale = Vector3.one * 1.2f;
            }
        }
    }

    /// <summary>
    /// 모든 Goal 하이라이트 해제
    /// </summary>
    private void ClearGoalHighlight()
    {
        if (levelLoader == null) return;

        var allGoals = levelLoader.GetSpawnedGoals();
        foreach (var goal in allGoals)
        {
            goal.transform.localScale = Vector3.one; // 모든 Goal을 기본 크기로
        }
    }

    /// <summary>
    /// 현재 선택된 캐릭터 반환
    /// </summary>
    public GamePiece GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    /// <summary>
    /// 특정 캐릭터 강제 선택
    /// </summary>
    public void ForceSelectCharacter(GamePiece character)
    {
        if (character != null && CanSelectCharacter(character))
        {
            SelectCharacter(character);
        }
    }

    /// <summary>
    /// 모든 선택 해제
    /// </summary>
    public void ClearSelection()
    {
        DeselectCharacter();
    }

    /// <summary>
    /// 입력 시스템 활성/비활성화 (게임 상태 관리용)
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        if (enabled)
            inputActions?.Enable();
        else
            inputActions?.Disable();
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (showTouchPosition && mainCamera != null)
        {
            // 터치 반경 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }

        // 선택된 캐릭터 주변에 기즈모 표시
        if (selectedCharacter != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(selectedCharacter.transform.position, touchRadius);
        }
    }

    [ContextMenu("Test Character Selection")]
    public void TestCharacterSelection()
    {
        if (levelLoader != null)
        {
            var characters = levelLoader.GetSpawnedCharacters();
            if (characters.Count > 0)
            {
                SelectCharacter(characters[0]);
            }
        }
    }

    [ContextMenu("Clear All Selections")]
    public void TestClearSelection()
    {
        ClearSelection();
    }
}
#endif