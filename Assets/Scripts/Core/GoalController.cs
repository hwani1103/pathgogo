using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// �������� �⺻ ���۰� ���¸� �����ϴ� ��Ʈ�ѷ�
/// </summary>
public class GoalController : MonoBehaviour
{
    [Header("Goal Info")]
    [SerializeField] private int goalIndex;
    [SerializeField] private GoalType goalType = GoalType.Individual;
    [SerializeField] private Color goalColor = Color.white;

    [Header("Position")]
    [SerializeField] private Vector3Int gridPosition;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    // �Ҵ�� ĳ���͵�
    private List<string> assignedCharacterIds = new List<string>();
    private bool isOccupied = false;

    // ����
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // ������Ʈ �ڵ� �Ҵ�
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
    }

    void Start()
    {
        SetupGoal();
        UpdateVisual();
    }

    /// <summary>
    /// ������ �ʱ� ����
    /// </summary>
    public void Initialize(int index, Vector3Int position, GoalType type, Color color, List<string> assignedCharacters = null)
    {
        goalIndex = index;
        gridPosition = position;
        goalType = type;
        goalColor = color;

        if (assignedCharacters != null)
        {
            assignedCharacterIds = new List<string>(assignedCharacters);
        }

        SetupGoal();
        UpdateVisual();
    }

    /// <summary>
    /// ������ �⺻ ���� ����
    /// </summary>
    private void SetupGoal()
    {
        gameObject.name = $"Goal_{goalIndex}_{goalType}";

        // �׸��� ��ǥ�� ���� ��ǥ�� ��ȯ�Ͽ� ��ġ ����
        if (gridVisualizer != null)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(gridPosition);
            transform.position = worldPos;
        }

        // ��������Ʈ ������ ����
        if (spriteRenderer != null)
        {
            spriteRenderer.color = goalColor;
            spriteRenderer.sortingOrder = 5; // Ÿ�� ��, ĳ���� �Ʒ�

            // ��� Goal �⺻ ũ��� 1.0f�� ����
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// �ð��� ǥ�� ������Ʈ
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // ���� ���¿� ���� ���� ����
        if (isOccupied)
        {
            spriteRenderer.color = goalColor * 0.7f; // ��Ӱ�
        }
        else
        {
            spriteRenderer.color = goalColor;
        }

        // ������ Ÿ�Կ� ���� �ð��� ȿ��
        UpdateTypeVisual();
    }

    /// <summary>
    /// ������ Ÿ�Կ� ���� �ð��� ȿ��
    /// </summary>
    private void UpdateTypeVisual()
    {
        // ���߿� ��ƼŬ ȿ���� �ִϸ��̼� �߰� ����
        switch (goalType)
        {
            case GoalType.Individual:
                // ���� �������� �⺻ ǥ��
                break;
            case GoalType.Shared:
                // ���� �������� �ణ�� �޽� ȿ�� (���߿� ����)
                break;
            case GoalType.Single:
                // ���� �������� �� �ε巯���� (���߿� ����)
                break;
        }
    }

    /// <summary>
    /// Ư�� ĳ���Ͱ� �� �������� ����� �� �ִ��� Ȯ��
    /// </summary>
    public bool CanUseGoal(string characterId)
    {
        switch (goalType)
        {
            case GoalType.Individual:
                // ���� ������: �Ҵ�� ĳ���͸� ��� ����
                return assignedCharacterIds.Contains(characterId) && !isOccupied;

            case GoalType.Shared:
                // ���� ������: �Ҵ�� ĳ���͵��� ���� ���
                return assignedCharacterIds.Contains(characterId);

            case GoalType.Single:
                // ���� ������: ��� ĳ���Ͱ� ��� ����
                return true;
        }

        return false;
    }

    /// <summary>
    /// ĳ���͸� �������� �Ҵ�
    /// </summary>
    public void AssignCharacter(string characterId)
    {
        if (!assignedCharacterIds.Contains(characterId))
        {
            assignedCharacterIds.Add(characterId);
            Debug.Log($"Character {characterId} assigned to Goal {goalIndex}");
        }
    }

    /// <summary>
    /// ������ ���� ���� ����
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        UpdateVisual();
    }

    /// <summary>
    /// ���� �׸��� ��ġ ��ȯ
    /// </summary>
    public Vector3Int GetGridPosition()
    {
        return gridPosition;
    }

    /// <summary>
    /// ������ �ε��� ��ȯ
    /// </summary>
    public int GetGoalIndex()
    {
        return goalIndex;
    }

    /// <summary>
    /// ������ Ÿ�� ��ȯ
    /// </summary>
    public GoalType GetGoalType()
    {
        return goalType;
    }

    /// <summary>
    /// �Ҵ�� ĳ���� ��� ��ȯ
    /// </summary>
    public List<string> GetAssignedCharacters()
    {
        return new List<string>(assignedCharacterIds);
    }

    /// <summary>
    /// ����� ���� ���
    /// </summary>
    public void LogGoalInfo()
    {
        Debug.Log($"Goal {goalIndex}: Position({gridPosition}), Type({goalType}), Occupied({isOccupied})");
        Debug.Log($"Assigned Characters: {string.Join(", ", assignedCharacterIds)}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // �����Ϳ��� ���ý� ������ ���� ǥ��
        Gizmos.color = goalColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);

        // ������ ���� ǥ��
        string info = $"Goal {goalIndex}\n({gridPosition.x},{gridPosition.y})\n{goalType}";
        if (assignedCharacterIds.Count > 0)
        {
            info += $"\nAssigned: {string.Join(",", assignedCharacterIds)}";
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, info);
    }
#endif
}