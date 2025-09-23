using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ��ġ �Է��� ó���ϰ� ĳ���� ������ �����ϴ� �ý���
/// ��ǥ ��ȯ ������ GridVisualizer�� ���յǵ��� ������
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
    private PathSelectionManager pathSelectionManager;

    void Awake()
    {
        // ������Ʈ ���� ����
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
    /// ��ġ �Է� ó�� (��ǥ ��ȯ ���� ����)
    /// </summary>
    private void ProcessTouch(Vector2 screenPosition)
    {
        if (mainCamera == null || gridVisualizer == null) return;

        // ��ũ�� ��ǥ�� ���� ��ǥ�� ��ȯ
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -mainCamera.transform.position.z));
        worldPosition.z = 0; // 2D �����̹Ƿ� Z�� 0

        if (showTouchPosition)
        {
            Debug.Log($"Touch at screen: {screenPosition}, world: {worldPosition}");
        }

        // GridVisualizer�� ����� ��Ȯ�� ��ǥ ��ȯ
        Vector3Int gridPosition = gridVisualizer.WorldToGridPosition(worldPosition);

        if (showDebugInfo)
        {
            Debug.Log($"World: {worldPosition} �� Grid: {gridPosition}");
            Debug.Log($"Grid valid: {gridVisualizer.IsValidGridPosition(gridPosition)}");
        }

        // ��ġ�� ��ġ���� ĳ���� ã��
        CharacterController touchedCharacter = FindCharacterAtPosition(worldPosition, gridPosition);

        if (touchedCharacter != null)
        {
            // ĳ���� ���� ���ɼ� �˻�
            if (CanSelectCharacter(touchedCharacter))
            {
                SelectCharacter(touchedCharacter);
            }
            else
            {
                ShowCharacterSelectionBlocked(touchedCharacter);
            }
        }
        else
        {
            // ĳ���Ͱ� ���õ� ���¿��� �� ������ ��ġ�� ���
            if (selectedCharacter != null && pathSelectionManager != null && pathSelectionManager.IsSelectingPath())
            {
                // ��� ���� ó�� (PathSelectionManager�� ����)
                pathSelectionManager.SelectPosition(gridPosition);
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.Log($"No character found at touch position. Grid position: {gridPosition}");
                }
            }
        }
    }

    /// <summary>
    /// Ư�� ��ġ���� ĳ���� ã�� (���� ��ȭ)
    /// </summary>
    private CharacterController FindCharacterAtPosition(Vector3 worldPosition, Vector3Int gridPosition)
    {
        if (levelLoader == null) return null;

        // ��� 1: �׸��� ��ǥ�� ��Ȯ�� ã�� (�켱����)
        CharacterController characterAtGrid = levelLoader.GetCharacterAt(gridPosition);
        if (characterAtGrid != null)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Found character {characterAtGrid.GetCharacterId()} at exact grid position {gridPosition}");
            }
            return characterAtGrid;
        }

        // ��� 2: ��ġ �ݰ� ������ ã�� (������ ��ġ)
        var allCharacters = levelLoader.GetSpawnedCharacters();
        CharacterController closestCharacter = null;
        float closestDistance = float.MaxValue;

        foreach (var character in allCharacters)
        {
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
    /// ĳ���� ���� ���� ���� Ȯ��
    /// </summary>
    private bool CanSelectCharacter(CharacterController character)
    {
        if (character == null) return false;

        // �̵� ���� ĳ���ʹ� ���� �Ұ�
        if (character.IsMoving())
        {
            Debug.Log($"Character {character.GetCharacterId()} is currently moving and cannot be selected");
            return false;
        }

        // ���� ���� Ƚ���� ������ ���� �Ұ�
        if (character.GetRemainingSelections() <= 0)
        {
            Debug.Log($"Character {character.GetCharacterId()} has no remaining selections");
            return false;
        }

        // �ٸ� ĳ���Ͱ� ��� ���� ���� ���� ���� �˻�
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
    /// ĳ���� ������ ���ܵǾ��� ���� �ǵ��
    /// </summary>
    private void ShowCharacterSelectionBlocked(CharacterController character)
    {
        if (Application.isMobilePlatform)
        {
            Handheld.Vibrate();
        }

        Debug.Log($"Character {character.GetCharacterId()} selection blocked");

        // �ð��� �ǵ�� (��: ĳ���� �ֺ��� ���� �׵θ� ȿ��)
        StartCoroutine(ShowBlockedSelectionEffect(character));
    }

    /// <summary>
    /// ���� ���� �ð� ȿ��
    /// </summary>
    private System.Collections.IEnumerator ShowBlockedSelectionEffect(CharacterController character)
    {
        if (character == null) yield break;

        // ���� ���� ���
        var spriteRenderer = character.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null) yield break;

        Color originalColor = spriteRenderer.color;
        Color blockedColor = Color.red;

        // ���� ������ ȿ��
        float duration = 0.5f;
        int blinkCount = 3;

        for (int i = 0; i < blinkCount; i++)
        {
            // ����������
            spriteRenderer.color = blockedColor;
            yield return new WaitForSeconds(duration / (blinkCount * 2));

            // ���� ������
            spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(duration / (blinkCount * 2));
        }
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

        // PathSelectionManager���� ��� ���� ���� ��û
        if (pathSelectionManager != null)
        {
            pathSelectionManager.StartPathSelection(selectedCharacter);
        }

        // ���õ� ĳ������ ��ǥ Goal ����
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

        // PathSelectionManager���� ��� ���� ���� ��û
        if (pathSelectionManager != null)
        {
            pathSelectionManager.EndPathSelection();
        }

        // Goal ���̶���Ʈ ����
        ClearGoalHighlight();
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
    /// ���� ���õ� ĳ���� ��ȯ
    /// </summary>
    public CharacterController GetSelectedCharacter()
    {
        return selectedCharacter;
    }

    /// <summary>
    /// Ư�� ĳ���� ���� ����
    /// </summary>
    public void ForceSelectCharacter(CharacterController character)
    {
        if (character != null && CanSelectCharacter(character))
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

    /// <summary>
    /// �Է� �ý��� Ȱ��/��Ȱ��ȭ (���� ���� ������)
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
            // ��ġ �ݰ� ǥ��
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, touchRadius);
        }

        // ���õ� ĳ���� �ֺ��� ����� ǥ��
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