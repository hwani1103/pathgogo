using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 터치 입력을 처리하고 캐릭터 선택을 관리하는 시스템
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
    private CharacterController selectedCharacter;

    // 참조
    private LevelLoader levelLoader;
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // 컴포넌트 참조 설정
        inputActions = new PlayerInputActions();
        mainCamera = Camera.main;
        levelLoader = FindFirstObjectByType<LevelLoader>();
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        if (mainCamera == null)
        {
            Debug.LogError("Main Camera not found!");
        }
    }

    void OnEnable()
    {
        inputActions.Enable();
        inputActions.Player.TouchPress.performed += OnTouchPressed;
        // 기존 Touch 이벤트들은 모두 제거
    }

    void OnDisable()
    {
        inputActions.Player.TouchPress.performed -= OnTouchPressed;
        inputActions.Disable();
        // 기존 Touch 이벤트들은 모두 제거
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
    /// 터치 입력 처리
    /// </summary>
    private void ProcessTouch(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        // 스크린 좌표를 월드 좌표로 변환
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPosition.z = 0; // 2D 게임이므로 Z는 0

        if (showTouchPosition)
        {
            Debug.Log($"Touch at screen: {screenPosition}, world: {worldPosition}");
        }

        // 그리드 좌표로 변환
        Vector3Int gridPosition = Vector3Int.zero;
        if (gridVisualizer != null)
        {
            gridPosition = gridVisualizer.WorldToGridPosition(worldPosition);
            if (showDebugInfo)
            {
                Debug.Log($"Grid position: {gridPosition}");
            }
        }

        // 터치한 위치에서 캐릭터 찾기
        CharacterController touchedCharacter = FindCharacterAtPosition(worldPosition, gridPosition);

        if (touchedCharacter != null)
        {
            SelectCharacter(touchedCharacter);
        }
        else
        {
            // 캐릭터가 선택된 상태에서 빈 공간을 터치한 경우
            if (selectedCharacter != null)
            {
                // 나중에 경로 선택 로직이 여기에 들어갈 예정
                if (showDebugInfo)
                {
                    Debug.Log($"Empty space touched while character {selectedCharacter.GetCharacterId()} is selected");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log("No character found at touch position");
                }
            }
        }
    }

    /// <summary>
    /// 특정 위치에서 캐릭터 찾기
    /// </summary>
    private CharacterController FindCharacterAtPosition(Vector3 worldPosition, Vector3Int gridPosition)
    {
        if (levelLoader == null) return null;

        // 방법 1: 그리드 좌표로 정확히 찾기
        CharacterController characterAtGrid = levelLoader.GetCharacterAt(gridPosition);
        if (characterAtGrid != null)
        {
            return characterAtGrid;
        }

        // 방법 2: 터치 반경 내에서 찾기 (더 관대한 터치)
        var allCharacters = levelLoader.GetSpawnedCharacters();
        foreach (var character in allCharacters)
        {
            float distance = Vector3.Distance(worldPosition, character.transform.position);
            if (distance <= touchRadius)
            {
                return character;
            }
        }

        return null;
    }

    /// <summary>
    /// 캐릭터 선택 처리
    /// </summary>
    private void SelectCharacter(CharacterController character)
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

        // 선택된 캐릭터의 가능한 이동 경로 표시 (나중에 구현)
        ShowAvailablePaths();
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

        // 경로 표시 숨기기 (나중에 구현)
        HideAvailablePaths();
        ClearGoalHighlight();
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
            goal.transform.localScale = Vector3.one;
        }
    }
    /// <summary>
    /// 선택된 캐릭터의 가능한 이동 경로 표시 (미구현)
    /// </summary>
    private void ShowAvailablePaths()
    {
        if (selectedCharacter == null) return;

        // 나중에 Line Renderer로 가능한 경로들을 표시할 예정
        if (showDebugInfo)
        {
            Debug.Log($"Showing available paths for {selectedCharacter.GetCharacterId()}");
            Debug.Log($"Remaining selections: {selectedCharacter.GetRemainingSelections()}");
        }
    }

    /// <summary>
    /// 경로 표시 숨기기 (미구현)
    /// </summary>
    private void HideAvailablePaths()
    {
        // 나중에 Line Renderer 정리 로직이 들어갈 예정
        if (showDebugInfo)
        {
            Debug.Log("Hiding available paths");
        }
    }

    /// <summary>
    /// 현재 선택된 캐릭터 반환
    /// </summary>
    public CharacterController GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    /// <summary>
    /// 선택된 캐릭터의 목표 Goal 강조
    /// </summary>
    private void HighlightAssignedGoal()
    {
        if (selectedCharacter == null || levelLoader == null) return;

        // 모든 Goal 하이라이트 해제
        var allGoals = levelLoader.GetSpawnedGoals();
        foreach (var goal in allGoals)
        {
            goal.transform.localScale = Vector3.one;
        }

        // 선택된 캐릭터가 사용할 수 있는 Goal들 하이라이트
        foreach (var goal in allGoals)
        {
            if (goal.CanUseGoal(selectedCharacter.GetCharacterId()))
            {
                goal.transform.localScale = Vector3.one * 1.2f;
            }
        }
    }
    /// <summary>
    /// 특정 캐릭터 강제 선택
    /// </summary>
    public void ForceSelectCharacter(CharacterController character)
    {
        if (character != null)
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (showTouchPosition && mainCamera != null)
        {
            // 터치 반경 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
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
#endif
}