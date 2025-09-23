using UnityEngine;

/// <summary>
/// ĳ������ �⺻ ���۰� ���¸� �����ϴ� ��Ʈ�ѷ�
/// </summary>
public class CharacterController : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private int maxSelections = 3;
    [SerializeField] private Color characterColor = Color.red;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector3Int currentGridPosition;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool isSelected = false;

    [Header("Completion State")]
    [SerializeField] private bool isCompleted = false;
    // ���� ����
    private bool isMoving = false;
    private int remainingSelections;

    // ����
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // ������Ʈ �ڵ� �Ҵ�
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        remainingSelections = maxSelections;
    }

    void Start()
    {
        // �ʱ� ����
        SetupCharacter();
        UpdateVisual();
    }

    /// <summary>
    /// ĳ���� �ʱ� ����
    /// </summary>
    public void Initialize(string id, Vector3Int startPos, int maxSel, Color color, float speed = 2f)
    {
        characterId = id;
        currentGridPosition = startPos;
        maxSelections = maxSel;
        remainingSelections = maxSel;
        characterColor = color;
        moveSpeed = speed;

        SetupCharacter();
        UpdateVisual();
    }
    /// <summary>
    /// ��� �ϼ� ���� ����
    /// </summary>
    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
        UpdateCompletedVisual();
    }

    /// <summary>
    /// �ϼ� ���� Ȯ��
    /// </summary>
    public bool IsCompleted()
    {
        return isCompleted;
    }


    /// <summary>
    /// �ϼ��� ĳ������ �ð��� ǥ�� ������Ʈ
    /// </summary>
    /// <summary>
    /// �ϼ��� ĳ������ �ð��� ǥ�� ������Ʈ
    /// </summary>
    private void UpdateCompletedVisual()
    {
        if (spriteRenderer == null) return;

        if (isCompleted)
        {
            // �������ϰ� ����
            Color color = spriteRenderer.color;
            color.a = 0.5f;
            spriteRenderer.color = color;
        }
        else
        {
            // ���� ������ ����
            Color color = spriteRenderer.color;
            color.a = 1f;
            spriteRenderer.color = color;
        }
    }
    /// <summary>
    /// ĳ���� �⺻ ���� ����
    /// </summary>
    private void SetupCharacter()
    {
        gameObject.name = $"Character_{characterId}";

        // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�Ͽ� ��ġ ����
        if (gridVisualizer != null)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(currentGridPosition);
            transform.position = worldPos;
        }

        // ��������Ʈ ������ ����
        if (spriteRenderer != null)
        {
            spriteRenderer.color = characterColor;
            spriteRenderer.sortingOrder = 10; // Ÿ�Ϻ��� ���� ǥ��
        }
    }

    /// <summary>
    /// �ð��� ǥ�� ������Ʈ (���� ���� ��)
    /// </summary>
    /// <summary>
    /// �ð��� ǥ�� ������Ʈ (���� ���� ��)
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // �Ϸ�� ĳ���ʹ� ������ ���� ����
        if (isCompleted)
        {
            Color color = characterColor;
            color.a = 0.5f;
            spriteRenderer.color = color;
            transform.localScale = Vector3.one; // ũ�� ��ȭ ����
            return;
        }

        // ���õ� ������ �� �� ��� ǥ��
        if (isSelected)
        {
            spriteRenderer.color = characterColor * 1.3f;
            transform.localScale = Vector3.one * 1.1f;
        }
        else
        {
            spriteRenderer.color = characterColor;
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// ĳ���� ����/����
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }

    /// <summary>
    /// ���� ��ġ���� Ư�� �������� �̵� �������� Ȯ��
    /// </summary>
    public bool CanMoveToDirection(Vector3Int direction)
    {
        if (remainingSelections <= 0) return false;
        if (isMoving) return false;

        Vector3Int targetPosition = currentGridPosition + direction;

        // �׸��� ���� Ȯ��
        if (gridVisualizer != null && !gridVisualizer.IsValidGridPosition(targetPosition))
            return false;

        return true;
    }

    /// <summary>
    /// Ư�� ��ġ�� �̵� �������� Ȯ��
    /// </summary>
    public bool CanMoveToPosition(Vector3Int targetPosition)
    {
        if (remainingSelections <= 0) return false;
        if (isMoving) return false;

        // �׸��� ���� Ȯ��
        if (gridVisualizer != null && !gridVisualizer.IsValidGridPosition(targetPosition))
            return false;

        return true;
    }

    /// <summary>
    /// ���� ���� Ƚ�� ��ȯ
    /// </summary>
    public int GetRemainingSelections()
    {
        return remainingSelections;
    }

    /// <summary>
    /// ���� �׸��� ��ġ ��ȯ
    /// </summary>
    public Vector3Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }

    /// <summary>
    /// ĳ���� ID ��ȯ
    /// </summary>
    public string GetCharacterId()
    {
        return characterId;
    }

    /// <summary>
    /// �̵� ������ Ȯ��
    /// </summary>
    /// 
    /// /// <summary>
    /// ���� �ִ� ���� Ƚ�� ��ȯ
    /// </summary>
    public int GetMaxSelections()
    {
        return maxSelections;
    }
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// ����� ���� ���
    /// </summary>
    public void LogCharacterInfo()
    {
        Debug.Log($"Character {characterId}: Position({currentGridPosition}), Remaining({remainingSelections}), Moving({isMoving})");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // �����Ϳ��� ���ý� ���� ��ġ�� ����� ǥ��
        Gizmos.color = characterColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // �׸��� ��ġ ���� ǥ��
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            $"{characterId}\n({currentGridPosition.x},{currentGridPosition.y})\nSel: {remainingSelections}");
    }
#endif
}