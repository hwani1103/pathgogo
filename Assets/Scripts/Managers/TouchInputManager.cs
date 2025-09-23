using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ��ġ �Է��� ó���ϰ� ĳ���� ������ �����ϴ� �ý���
/// </summary>
public class TouchInputManager : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private float touchRadius = 0.5f;
    [SerializeField] private LayerMask touchLayerMask = -1;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private bool showTouchPosition = true;

    // �Է� �ý���
    private PlayerInputActions inputActions;
    private Camera mainCamera;

    // ���õ� ĳ����
    private CharacterController selectedCharacter;

    // ����
    private LevelLoader levelLoader;
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // ������Ʈ ���� ����
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
        // ���� Touch �̺�Ʈ���� ��� ����
    }

    void OnDisable()
    {
        inputActions.Player.TouchPress.performed -= OnTouchPressed;
        inputActions.Disable();
        // ���� Touch �̺�Ʈ���� ��� ����
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
    /// ��ġ �Է� ó��
    /// </summary>
    private void ProcessTouch(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        // ��ũ�� ��ǥ�� ���� ��ǥ�� ��ȯ
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, mainCamera.nearClipPlane));
        worldPosition.z = 0; // 2D �����̹Ƿ� Z�� 0

        if (showTouchPosition)
        {
            Debug.Log($"Touch at screen: {screenPosition}, world: {worldPosition}");
        }

        // �׸��� ��ǥ�� ��ȯ
        Vector3Int gridPosition = Vector3Int.zero;
        if (gridVisualizer != null)
        {
            gridPosition = gridVisualizer.WorldToGridPosition(worldPosition);
            if (showDebugInfo)
            {
                Debug.Log($"Grid position: {gridPosition}");
            }
        }

        // ��ġ�� ��ġ���� ĳ���� ã��
        CharacterController touchedCharacter = FindCharacterAtPosition(worldPosition, gridPosition);

        if (touchedCharacter != null)
        {
            SelectCharacter(touchedCharacter);
        }
        else
        {
            // ĳ���Ͱ� ���õ� ���¿��� �� ������ ��ġ�� ���
            if (selectedCharacter != null)
            {
                // ���߿� ��� ���� ������ ���⿡ �� ����
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
    /// Ư�� ��ġ���� ĳ���� ã��
    /// </summary>
    private CharacterController FindCharacterAtPosition(Vector3 worldPosition, Vector3Int gridPosition)
    {
        if (levelLoader == null) return null;

        // ��� 1: �׸��� ��ǥ�� ��Ȯ�� ã��
        CharacterController characterAtGrid = levelLoader.GetCharacterAt(gridPosition);
        if (characterAtGrid != null)
        {
            return characterAtGrid;
        }

        // ��� 2: ��ġ �ݰ� ������ ã�� (�� ������ ��ġ)
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
    /// ĳ���� ���� ó��
    /// </summary>
    private void SelectCharacter(CharacterController character)
    {
        // �̹� ���õ� ĳ���Ϳ� ���ٸ� ���� ����
        if (selectedCharacter == character)
        {
            DeselectCharacter();
            return;
        }

        // ���� ���� ����
        if (selectedCharacter != null)
        {
            selectedCharacter.SetSelected(false);
        }

        // �� ĳ���� ����
        selectedCharacter = character;
        selectedCharacter.SetSelected(true);

        if (showDebugInfo)
        {
            Debug.Log($"Character {selectedCharacter.GetCharacterId()} selected");
            selectedCharacter.LogCharacterInfo();
        }

        // ���õ� ĳ������ ������ �̵� ��� ǥ�� (���߿� ����)
        ShowAvailablePaths();
        HighlightAssignedGoal();
    }

    /// <summary>
    /// ĳ���� ���� ����
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

        // ��� ǥ�� ����� (���߿� ����)
        HideAvailablePaths();
        ClearGoalHighlight();
    }
    /// <summary>
    /// ��� Goal ���̶���Ʈ ����
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
    /// ���õ� ĳ������ ������ �̵� ��� ǥ�� (�̱���)
    /// </summary>
    private void ShowAvailablePaths()
    {
        if (selectedCharacter == null) return;

        // ���߿� Line Renderer�� ������ ��ε��� ǥ���� ����
        if (showDebugInfo)
        {
            Debug.Log($"Showing available paths for {selectedCharacter.GetCharacterId()}");
            Debug.Log($"Remaining selections: {selectedCharacter.GetRemainingSelections()}");
        }
    }

    /// <summary>
    /// ��� ǥ�� ����� (�̱���)
    /// </summary>
    private void HideAvailablePaths()
    {
        // ���߿� Line Renderer ���� ������ �� ����
        if (showDebugInfo)
        {
            Debug.Log("Hiding available paths");
        }
    }

    /// <summary>
    /// ���� ���õ� ĳ���� ��ȯ
    /// </summary>
    public CharacterController GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    /// <summary>
    /// ���õ� ĳ������ ��ǥ Goal ����
    /// </summary>
    private void HighlightAssignedGoal()
    {
        if (selectedCharacter == null || levelLoader == null) return;

        // ��� Goal ���̶���Ʈ ����
        var allGoals = levelLoader.GetSpawnedGoals();
        foreach (var goal in allGoals)
        {
            goal.transform.localScale = Vector3.one;
        }

        // ���õ� ĳ���Ͱ� ����� �� �ִ� Goal�� ���̶���Ʈ
        foreach (var goal in allGoals)
        {
            if (goal.CanUseGoal(selectedCharacter.GetCharacterId()))
            {
                goal.transform.localScale = Vector3.one * 1.2f;
            }
        }
    }
    /// <summary>
    /// Ư�� ĳ���� ���� ����
    /// </summary>
    public void ForceSelectCharacter(CharacterController character)
    {
        if (character != null)
        {
            SelectCharacter(character);
        }
    }

    /// <summary>
    /// ��� ���� ����
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
            // ��ġ �ݰ� ǥ��
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