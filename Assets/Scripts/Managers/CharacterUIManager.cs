using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// ���õ� ĳ������ ������ UI�� ǥ���ϴ� �ý���
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

    // ���� ǥ�� ���� ĳ����
    private GamePiece currentCharacter;

    // ����
    private TouchInputManager touchInputManager;
    private PathSelectionManager pathSelectionManager;

    // UI �ִϸ��̼ǿ�
    private CanvasGroup panelCanvasGroup;
    private bool isPanelVisible = false;

    void Awake()
    {
        touchInputManager = FindFirstObjectByType<TouchInputManager>();
        pathSelectionManager = FindFirstObjectByType<PathSelectionManager>();

        // CanvasGroup ������Ʈ Ȯ��/�߰�
        if (characterInfoPanel != null)
        {
            panelCanvasGroup = characterInfoPanel.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = characterInfoPanel.AddComponent<CanvasGroup>();

            // �ʱ⿡�� ����
            characterInfoPanel.SetActive(false);
            panelCanvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        // �ʱ� UI ���� ����
        HideCharacterInfo();
    }

    void Update()
    {
        // ���õ� ĳ���� ���� �ǽð� ������Ʈ
        UpdateCharacterInfo();

        // �г� ���̵� �ִϸ��̼�
        UpdatePanelAnimation();
    }

    /// <summary>
    /// ĳ���� ���� �ǽð� ������Ʈ
    /// </summary>
    /// <summary>
    /// ĳ���� ���� �ǽð� ������Ʈ
    /// </summary>
    private void UpdateCharacterInfo()
    {
        if (touchInputManager == null) return;

        GamePiece selectedCharacter = touchInputManager.GetSelectedCharacter();

        // ���õ� ĳ���Ͱ� ����Ǿ��� ��
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

        // ���� ĳ���Ͱ� �Ϸ�Ǿ����� UI �����
        if (currentCharacter != null && currentCharacter.IsCompleted())
        {
            HideCharacterInfo();
            currentCharacter = null;
            return;
        }

        // ���õ� ĳ���Ͱ� ���� �� �ǽð� ������Ʈ
        if (currentCharacter != null && isPanelVisible)
        {
            UpdateSelectionCounts();
        }
    }

    /// <summary>
    /// ĳ���� ���� ǥ��
    /// </summary>
    private void ShowCharacterInfo()
    {
        if (!showPanel || characterInfoPanel == null || currentCharacter == null) return;

        // �⺻ ���� ����
        if (characterIdText != null)
            characterIdText.text = $"Character: {currentCharacter.GetCharacterId()}";

        if (maxSelectionsText != null)
            maxSelectionsText.text = $"Max Selections: {currentCharacter.GetRemainingSelections()}"; // �ʱⰪ

        // �г� ǥ��
        characterInfoPanel.SetActive(true);
        isPanelVisible = true;

        UpdateSelectionCounts();
    }

    /// <summary>
    /// ���� Ƚ�� ���� ������Ʈ
    /// </summary>
    /// <summary>
    /// ���� Ƚ�� ���� ������Ʈ
    /// </summary>
    private void UpdateSelectionCounts()
    {
        if (currentCharacter == null) return;

        // ���� ���� ���� Ƚ�� ���
        int usedSelections = 0;
        if (pathSelectionManager != null && pathSelectionManager.IsSelectingPath())
        {
            var currentPath = pathSelectionManager.GetCurrentPath();
            if (currentPath != null && currentPath.Count > 1)
            {
                usedSelections = currentPath.Count - 1; // ������ ����
            }
        }

        int maxSelections = currentCharacter.GetMaxSelections();
        int remainingSelections = maxSelections - usedSelections;

        // UI ������Ʈ
        if (maxSelectionsText != null)
            maxSelectionsText.text = $"Max Selections: {maxSelections}";

        if (remainingSelectionsText != null)
            remainingSelectionsText.text = $"Remaining: {remainingSelections}/{maxSelections}";
    }

    /// <summary>
    /// ĳ���� ���� �����
    /// </summary>
    private void HideCharacterInfo()
    {
        isPanelVisible = false;

        if (characterInfoPanel != null)
        {
            // ���̵� �ƿ� �ִϸ��̼� �� ��Ȱ��ȭ
            Invoke(nameof(DeactivatePanel), 1f / panelFadeSpeed);
        }
    }

    /// <summary>
    /// �г� ��Ȱ��ȭ
    /// </summary>
    private void DeactivatePanel()
    {
        if (characterInfoPanel != null && !isPanelVisible)
            characterInfoPanel.SetActive(false);
    }

    /// <summary>
    /// �г� ���̵� �ִϸ��̼�
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
    /// ĳ���� ����� ��ġ�ϴ� UI ���� ����
    /// </summary>
    private void ApplyCharacterColor()
    {
        if (currentCharacter == null) return;

        // ĳ������ ���� ��������
        var characterRenderer = currentCharacter.GetComponent<SpriteRenderer>();
        if (characterRenderer != null)
        {
            Color characterColor = characterRenderer.color;

            // �ؽ�Ʈ ���� ����
            if (characterIdText != null)
                characterIdText.color = characterColor;
        }
    }

    /// <summary>
    /// UI ���� ����
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
    /// �������� UI ���� ������Ʈ
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