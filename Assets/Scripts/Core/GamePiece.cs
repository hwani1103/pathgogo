using UnityEngine;

/// <summary>
/// 캐릭터의 기본 동작과 상태를 관리하는 컨트롤러
/// </summary>
public class GamePiece : MonoBehaviour
{
    [Header("Character Info")]
    [SerializeField] private string characterId;
    [SerializeField] private int maxSelections = 3;
    [SerializeField] private Color characterColor = Color.red;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Vector3Int currentGridPosition;
    [SerializeField] private Vector3Int startGridPosition; // 시작 위치 저장

    [Header("Visual")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool isSelected = false;

    [Header("Completion State")]
    [SerializeField] private bool isCompleted = false;

  

    // 상태 관리
    private bool isMoving = false;
    private int remainingSelections;

    // 참조
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        // 컴포넌트 자동 할당
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        remainingSelections = maxSelections;
    }

    void Start()
    {
        // 초기 설정
        SetupCharacter();
        UpdateVisual();
    }

    /// <summary>
    /// 캐릭터 초기 설정
    /// </summary>
    public void Initialize(string id, Vector3Int startPos, int maxSel, Color color, float speed = 2f)
    {
        characterId = id;
        currentGridPosition = startPos;
        startGridPosition = startPos; // 시작 위치 저장
        maxSelections = maxSel;
        remainingSelections = maxSel;
        characterColor = color;
        moveSpeed = speed;

        SetupCharacter();
        UpdateVisual();
    }

    /// <summary>
    /// 경로 완성 상태 설정
    /// </summary>
    public void SetCompleted(bool completed)
    {
        isCompleted = completed;
        UpdateVisual();
    }

    /// <summary>
    /// 완성 상태 확인
    /// </summary>
    public bool IsCompleted()
    {
        return isCompleted;
    }
    /// <summary>
    /// 시작 위치 반환
    /// </summary>
    public Vector3Int GetStartGridPosition()
    {
        return startGridPosition;
    }

    /// <summary>
    /// 현재 위치 업데이트 (이동 완료 시 호출)
    /// </summary>
    public void UpdateGridPosition(Vector3Int newPosition)
    {
        currentGridPosition = newPosition;
    }
    /// <summary>
    /// 캐릭터 기본 설정 적용
    /// </summary>
    private void SetupCharacter()
    {
        gameObject.name = $"Character_{characterId}";

        // 그리드 좌표를 월드 좌표로 변환하여 위치 설정
        if (gridVisualizer != null)
        {
            Vector3 worldPos = gridVisualizer.GridToWorldPosition(currentGridPosition);
            transform.position = worldPos;
        }

        // 스프라이트 렌더러 설정
        if (spriteRenderer != null)
        {
            spriteRenderer.color = characterColor;
            spriteRenderer.sortingOrder = 10; // 타일보다 위에 표시
        }
    }

    /// <summary>
    /// 시각적 표현 업데이트 (선택 상태 등)
    /// </summary>
    /// <summary>
    /// 시각적 표현 업데이트 (선택 상태 등)
    /// </summary>
    private void UpdateVisual()
    {
        if (spriteRenderer == null) return;

        // 완료된 캐릭터는 반투명 상태 유지
        if (isCompleted)
        {
            Color color = characterColor;
            color.a = 0.5f;
            spriteRenderer.color = color;
            transform.localScale = Vector3.one; // 크기 변화 없음
            return;
        }

        // 선택된 상태일 때 더 밝게 표시
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
    /// 캐릭터 선택/해제
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateVisual();
    }


    /// <summary>
    /// 남은 선택 횟수 반환
    /// </summary>
    public int GetRemainingSelections()
    {
        return remainingSelections;
    }

    /// <summary>
    /// 현재 그리드 위치 반환
    /// </summary>
    public Vector3Int GetCurrentGridPosition()
    {
        return currentGridPosition;
    }

    /// <summary>
    /// 캐릭터 ID 반환
    /// </summary>
    public string GetCharacterId()
    {
        return characterId;
    }

    /// <summary>
    /// 이동 중인지 확인
    /// </summary>
    /// 
    /// /// <summary>
    /// 원래 최대 선택 횟수 반환
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
    /// 디버그 정보 출력
    /// </summary>
    public void LogCharacterInfo()
    {
        Debug.Log($"Character {characterId}: Position({currentGridPosition}), Remaining({remainingSelections}), Moving({isMoving})");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // 에디터에서 선택시 현재 위치에 기즈모 표시
        Gizmos.color = characterColor;
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // 그리드 위치 정보 표시
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f,
            $"{characterId}\n({currentGridPosition.x},{currentGridPosition.y})\nSel: {remainingSelections}");
    }
#endif
}