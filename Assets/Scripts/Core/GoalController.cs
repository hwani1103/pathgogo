using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 목적지의 기본 동작과 상태를 관리하는 컨트롤러
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

    // 할당된 캐릭터들
    private List<string> assignedCharacterIds = new List<string>();
    private bool isOccupied = false;

    // 참조
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // 컴포넌트 자동 할당
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
    /// 목적지 초기 설정
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
    /// 목적지 기본 설정 적용
    /// </summary>
    private void SetupGoal()
    {
        gameObject.name = $"Goal_{goalIndex}_{goalType}";

        // 그리드 좌표를 월드 좌표로 변환하여 위치 설정
        if (gridVisualizer != null)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(gridPosition);
            transform.position = worldPos;
        }

        // 스프라이트 렌더러 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.color = goalColor;
            spriteRenderer.sortingOrder = 5; // 타일 위, 캐릭터 아래

            // 모든 Goal 기본 크기는 1.0f로 통일
            transform.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 시각적 표현 업데이트
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // 점유 상태에 따라 색상 조정
        if (isOccupied)
        {
            spriteRenderer.color = goalColor * 0.7f; // 어둡게
        }
        else
        {
            spriteRenderer.color = goalColor;
        }

        // 목적지 타입에 따른 시각적 효과
        UpdateTypeVisual();
    }

    /// <summary>
    /// 목적지 타입에 따른 시각적 효과
    /// </summary>
    private void UpdateTypeVisual()
    {
        // 나중에 파티클 효과나 애니메이션 추가 가능
        switch (goalType)
        {
            case GoalType.Individual:
                // 개별 목적지는 기본 표시
                break;
            case GoalType.Shared:
                // 공유 목적지는 약간의 펄스 효과 (나중에 구현)
                break;
            case GoalType.Single:
                // 단일 목적지는 더 두드러지게 (나중에 구현)
                break;
        }
    }

    /// <summary>
    /// 특정 캐릭터가 이 목적지를 사용할 수 있는지 확인
    /// </summary>
    public bool CanUseGoal(string characterId)
    {
        switch (goalType)
        {
            case GoalType.Individual:
                // 개별 목적지: 할당된 캐릭터만 사용 가능
                return assignedCharacterIds.Contains(characterId) && !isOccupied;

            case GoalType.Shared:
                // 공유 목적지: 할당된 캐릭터들이 공유 사용
                return assignedCharacterIds.Contains(characterId);

            case GoalType.Single:
                // 단일 목적지: 모든 캐릭터가 사용 가능
                return true;
        }

        return false;
    }

    /// <summary>
    /// 캐릭터를 목적지에 할당
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
    /// 목적지 점유 상태 설정
    /// </summary>
    public void SetOccupied(bool occupied)
    {
        isOccupied = occupied;
        UpdateVisual();
    }

    /// <summary>
    /// 현재 그리드 위치 반환
    /// </summary>
    public Vector3Int GetGridPosition()
    {
        return gridPosition;
    }

    /// <summary>
    /// 목적지 인덱스 반환
    /// </summary>
    public int GetGoalIndex()
    {
        return goalIndex;
    }

    /// <summary>
    /// 목적지 타입 반환
    /// </summary>
    public GoalType GetGoalType()
    {
        return goalType;
    }

    /// <summary>
    /// 할당된 캐릭터 목록 반환
    /// </summary>
    public List<string> GetAssignedCharacters()
    {
        return new List<string>(assignedCharacterIds);
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void LogGoalInfo()
    {
        Debug.Log($"Goal {goalIndex}: Position({gridPosition}), Type({goalType}), Occupied({isOccupied})");
        Debug.Log($"Assigned Characters: {string.Join(", ", assignedCharacterIds)}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 에디터에서 선택시 목적지 정보 표시
        Gizmos.color = goalColor;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.9f);

        // 목적지 정보 표시
        string info = $"Goal {goalIndex}\n({gridPosition.x},{gridPosition.y})\n{goalType}";
        if (assignedCharacterIds.Count > 0)
        {
            info += $"\nAssigned: {string.Join(",", assignedCharacterIds)}";
        }

        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.2f, info);
    }
#endif
}