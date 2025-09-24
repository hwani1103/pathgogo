using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 선택된 캐릭터의 정보를 UI로 표시하는 시스템
/// </summary>
public class CharacterUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private TextMeshProUGUI characterIdText;
    [SerializeField] private TextMeshProUGUI maxSelectionsText;
    [SerializeField] private TextMeshProUGUI remainingSelectionsText;

    [Header("UI Settings")]
    [SerializeField] private bool showPanel = true;
    [SerializeField] private float panelFadeSpeed = 2f;

    // 현재 표시 중인 캐릭터
    private GamePiece currentCharacter;

    // 참조
    private TouchInputManager touchInputManager;
    private PathSelectionManager pathSelectionManager;

    // UI 애니메이션용
    private CanvasGroup panelCanvasGroup;
    private bool isPanelVisible = false;

    void Awake()
    {
        touchInputManager = FindFirstObjectByType<TouchInputManager>();
        pathSelectionManager = FindFirstObjectByType<PathSelectionManager>();

        // CanvasGroup 컴포넌트 확인/추가
        if (characterInfoPanel != null)
        {
            panelCanvasGroup = characterInfoPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = characterInfoPanel.AddComponent<CanvasGroup>();

            // 초기에는 숨김
            characterInfoPanel.SetActive(false);
            panelCanvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        // 초기 UI 상태 설정
        HideCharacterInfo();
    }

    void Update()
    {
        // 선택된 캐릭터 정보 실시간 업데이트
        UpdateCharacterInfo();

        // 패널 페이드 애니메이션
        UpdatePanelAnimation();
    }

    /// <summary>
    /// 캐릭터 정보 실시간 업데이트
    /// </summary>
    /// <summary>
    /// 캐릭터 정보 실시간 업데이트
    /// </summary>
    private void UpdateCharacterInfo()
    {
        if (touchInputManager == null) return;

        GamePiece selectedCharacter = touchInputManager.GetSelectedCharacter();

        // 선택된 캐릭터가 변경되었을 때
        if (currentCharacter != selectedCharacter)
        {
            currentCharacter = selectedCharacter;

            if (currentCharacter != null)
            {
                ShowCharacterInfo();
            }
            else
            {
                HideCharacterInfo();
            }
        }

        // 현재 캐릭터가 완료되었으면 UI 숨기기
        if (currentCharacter != null && currentCharacter.IsCompleted())
        {
            HideCharacterInfo();
            currentCharacter = null;
            return;
        }

        // 선택된 캐릭터가 있을 때 실시간 업데이트
        if (currentCharacter != null && isPanelVisible)
        {
            UpdateSelectionCounts();
        }
    }

    /// <summary>
    /// 캐릭터 정보 표시
    /// </summary>
    private void ShowCharacterInfo()
    {
        if (!showPanel || characterInfoPanel == null || currentCharacter == null) return;

        // 기본 정보 설정
        if (characterIdText != null)
            characterIdText.text = $"Character: {currentCharacter.GetCharacterId()}";

        if (maxSelectionsText != null)
            maxSelectionsText.text = $"Max Selections: {currentCharacter.GetRemainingSelections()}"; // 초기값

        // 패널 표시
        characterInfoPanel.SetActive(true);
        isPanelVisible = true;

        UpdateSelectionCounts();
    }

    /// <summary>
    /// 선택 횟수 정보 업데이트
    /// </summary>
    /// <summary>
    /// 선택 횟수 정보 업데이트
    /// </summary>
    private void UpdateSelectionCounts()
    {
        if (currentCharacter == null) return;

        // 현재 사용된 선택 횟수 계산
        int usedSelections = 0;
        if (pathSelectionManager != null && pathSelectionManager.IsSelectingPath())
        {
            var currentPath = pathSelectionManager.GetCurrentPath();
            if (currentPath != null && currentPath.Count > 1)
            {
                usedSelections = currentPath.Count - 1; // 시작점 제외
            }
        }

        int maxSelections = currentCharacter.GetMaxSelections();
        int remainingSelections = maxSelections - usedSelections;

        // UI 업데이트
        if (maxSelectionsText != null)
            maxSelectionsText.text = $"Max Selections: {maxSelections}";

        if (remainingSelectionsText != null)
            remainingSelectionsText.text = $"Remaining: {remainingSelections}/{maxSelections}";
    }

    /// <summary>
    /// 캐릭터 정보 숨기기
    /// </summary>
    private void HideCharacterInfo()
    {
        isPanelVisible = false;

        if (characterInfoPanel != null)
        {
            // 페이드 아웃 애니메이션 후 비활성화
            Invoke(nameof(DeactivatePanel), 1f / panelFadeSpeed);
        }
    }

    /// <summary>
    /// 패널 비활성화
    /// </summary>
    private void DeactivatePanel()
    {
        if (characterInfoPanel != null && !isPanelVisible)
            characterInfoPanel.SetActive(false);
    }

    /// <summary>
    /// 패널 페이드 애니메이션
    /// </summary>
    private void UpdatePanelAnimation()
    {
        if (panelCanvasGroup == null) return;

        float targetAlpha = isPanelVisible ? 1f : 0f;
        float currentAlpha = panelCanvasGroup.alpha;

        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
        {
            panelCanvasGroup.alpha = Mathf.MoveTowards(currentAlpha, targetAlpha, panelFadeSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// 캐릭터 색상과 일치하는 UI 색상 적용
    /// </summary>
    private void ApplyCharacterColor()
    {
        if (currentCharacter == null) return;

        // 캐릭터의 색상 가져오기
        var characterRenderer = currentCharacter.GetComponent<SpriteRenderer>();
        if (characterRenderer != null)
        {
            Color characterColor = characterRenderer.color;

            // 텍스트 색상 적용
            if (characterIdText != null)
                characterIdText.color = characterColor;
        }
    }

    /// <summary>
    /// UI 설정 변경
    /// </summary>
    public void SetUISettings(bool showUI, float fadeSpeed = 2f)
    {
        showPanel = showUI;
        panelFadeSpeed = fadeSpeed;

        if (!showUI)
        {
            HideCharacterInfo();
        }
    }

    /// <summary>
    /// 수동으로 UI 강제 업데이트
    /// </summary>
    public void ForceUpdateUI()
    {
        UpdateCharacterInfo();
    }

#if UNITY_EDITOR
    [ContextMenu("Test Show Character Info")]
    public void TestShowCharacterInfo()
    {
        if (touchInputManager != null)
        {
            var selectedCharacter = touchInputManager.GetSelectedCharacter();
            if (selectedCharacter != null)
            {
                ShowCharacterInfo();
            }
            else
            {
                Debug.Log("No character selected for testing");
            }
        }
    }

    [ContextMenu("Test Hide Character Info")]
    public void TestHideCharacterInfo()
    {
        HideCharacterInfo();
    }
#endif
}