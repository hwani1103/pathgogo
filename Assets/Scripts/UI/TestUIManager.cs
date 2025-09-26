using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TestUIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private TextMeshProUGUI pathInfoText;

    [Header("Game State Display")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject gameClearPanel;
    [SerializeField] private GameObject allLevelsCompletePanel;
    [SerializeField] private TextMeshProUGUI gameOverText;

    private MovementGameManager gameManager;
    private PathSelectionManager pathSelectionManager;
    private LevelLoader levelLoader;

    void Awake()
    {
        gameManager = FindFirstObjectByType<MovementGameManager>();
        pathSelectionManager = FindFirstObjectByType<PathSelectionManager>();
        levelLoader = FindFirstObjectByType<LevelLoader>();

        SetupUI();
    }

    void Start()
    {
        UpdateUI();
    }

    void Update()
    {
        UpdateUI();
    }

    private void SetupUI()
    {
        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.RemoveAllListeners();
            nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            nextLevelButton.gameObject.SetActive(false);

            var buttonText = nextLevelButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = "Next Level";
            }
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
            restartButton.gameObject.SetActive(false);
        }

        HideAllPanels();
    }

    private void UpdateUI()
    {
        UpdateStatusText();
        UpdatePathInfoText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null || gameManager == null) return;

        string status = "Status: ";

        if (gameManager.IsGameInProgress())
        {
            status += "Game In Progress";
        }
        else if (gameManager.AreAllPathsComplete())
        {
            status += "Ready to Start";
        }
        else
        {
            status += "Setting Paths...";
        }

        statusText.text = status;
    }

    private void UpdatePathInfoText()
    {
        if (pathInfoText == null || levelLoader == null) return;

        var characters = levelLoader.GetSpawnedCharacters();
        int completedCount = 0;

        foreach (var character in characters)
        {
            if (character.IsCompleted())
                completedCount++;
        }

        pathInfoText.text = $"Completed Paths: {completedCount}/{characters.Count}";
    }

    private void OnNextLevelClicked()
    {
        if (levelLoader != null)
        {
            levelLoader.LoadNextLevel();
        }

        HideAllPanels();
    }

    private void OnRestartClicked()
    {
        if (levelLoader != null)
        {
            levelLoader.RestartCurrentLevel();
        }

        HideAllPanels();
    }

    public void ShowGameOverUI(string message)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (gameOverText != null)
        {
            gameOverText.text = $"Game Over!\n{message}";
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }
    }

    public void ShowLevelClearedUI()
    {
        if (gameClearPanel != null)
        {
            gameClearPanel.SetActive(true);
        }

        if (nextLevelButton != null)
        {
            nextLevelButton.gameObject.SetActive(true);
        }
    }

    public void ShowAllLevelsCompleted()
    {
        if (allLevelsCompletePanel != null)
        {
            allLevelsCompletePanel.SetActive(true);
        }
    }

    private void HideAllPanels()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (gameClearPanel != null)
            gameClearPanel.SetActive(false);
        if (allLevelsCompletePanel != null)
            allLevelsCompletePanel.SetActive(false);

        if (nextLevelButton != null)
            nextLevelButton.gameObject.SetActive(false);
        if (restartButton != null)
            restartButton.gameObject.SetActive(false);
    }
}