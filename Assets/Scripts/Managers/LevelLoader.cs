using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LevelData�� �о Scene�� ĳ���Ϳ� �������� ��ġ�ϴ� �ý���
/// ���ο� ��Ű��ó���� ������ ���� ������
/// </summary>
public class LevelLoader : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private LevelData currentLevelData;

    [Header("Prefabs")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Runtime Objects")]
    [SerializeField] private Transform charactersParent;
    [SerializeField] private Transform goalsParent;

    // ��Ÿ�ӿ� ������ ������Ʈ��
    private List<GamePiece> spawnedCharacters = new List<GamePiece>();
    private List<GoalController> spawnedGoals = new List<GoalController>();

    // ����
    private GridVisualizer gridVisualizer;

    // ���ο� �ý��۵鿡 ���� ����
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        // ���ο� �ý��۵� ã��
        pathValidator = FindFirstObjectByType<PathValidator>();
        collisionDetector = FindFirstObjectByType<CollisionDetector>();
        movementSimulator = FindFirstObjectByType<MovementSimulator>();

        // �θ� ������Ʈ�� ������ ����
        if (charactersParent == null)
        {
            GameObject charactersContainer = new GameObject("Characters");
            charactersParent = charactersContainer.transform;
        }

        if (goalsParent == null)
        {
            GameObject goalsContainer = new GameObject("Goals");
            goalsParent = goalsContainer.transform;
        }
    }

    void Start()
    {
        if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
    }

    /// <summary>
    /// ���� �����͸� �ε��Ͽ� Scene�� ��ġ
    /// </summary>
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null!");
            return;
        }

        // ���� ������Ʈ�� ����
        ClearCurrentLevel();

        currentLevelData = levelData;

        // ���ο� �ý��۵鿡 ���� ������ ����
        NotifySystemsOfLevelData();

        // GridVisualizer ���� ������Ʈ
        UpdateGridVisualizerSettings();

        // ĳ���͵� ����
        SpawnCharacters();

        // �������� ����
        SpawnGoals();

        // ĳ����-������ �Ҵ� ����
        SetupCharacterGoalAssignments();

        Debug.Log($"Level {levelData.levelNumber} loaded successfully!");
        LogLevelStatus();
    }

    /// <summary>
    /// ���ο� �ý��۵鿡 ���� ������ ����
    /// </summary>
    private void NotifySystemsOfLevelData()
    {
        if (pathValidator != null)
        {
            pathValidator.SetLevelData(currentLevelData);
        }

        if (movementSimulator != null)
        {
            movementSimulator.SetLevelData(currentLevelData);
        }

        // CollisionDetector�� �ٸ� �ý��۵��� ���� ���������� �����͸� ����
    }

    /// <summary>
    /// ���� ���� ������ ��ȯ (�ٸ� �ý��ۿ��� ���)
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    /// <summary>
    /// ���� ������ ��� ������Ʈ ����
    /// </summary>
    private void ClearCurrentLevel()
    {
        // ���� ĳ���͵� ����
        foreach (var character in spawnedCharacters)
        {
            if (character != null)
                DestroyImmediate(character.gameObject);
        }
        spawnedCharacters.Clear();

        // ���� �������� ����
        foreach (var goal in spawnedGoals)
        {
            if (goal != null)
                DestroyImmediate(goal.gameObject);
        }
        spawnedGoals.Clear();
    }

    /// <summary>
    /// GridVisualizer ������ LevelData�� ���� ������Ʈ
    /// </summary>
    private void UpdateGridVisualizerSettings()
    {
        if (gridVisualizer == null) return;

        // �������� �����ϵ��� �ȳ� (GridVisualizer�� public ���� �޼��尡 �ִٸ� ���⼭ ȣ��)
        Debug.Log($"Please update GridVisualizer settings:");
        Debug.Log($"Grid Width: {currentLevelData.gridWidth}");
        Debug.Log($"Grid Height: {currentLevelData.gridHeight}");
        Debug.Log($"Cell Size: {currentLevelData.cellSize}");
        Debug.Log($"Grid Origin: {currentLevelData.gridOrigin}");

        // GridVisualizer�� ���� ������Ʈ �޼��尡 �ִٸ� �ּ� ����
        // gridVisualizer.UpdateGridSettings(
        //     currentLevelData.gridWidth, 
        //     currentLevelData.gridHeight,
        //     currentLevelData.cellSize,
        //     currentLevelData.gridOrigin
        // );
    }

    /// <summary>
    /// ĳ���͵��� Scene�� ����
    /// </summary>
    private void SpawnCharacters()
    {
        if (characterPrefab == null)
        {
            Debug.LogError("Character prefab is not assigned!");
            return;
        }

        foreach (var characterData in currentLevelData.characters)
        {
            // ĳ���� ������Ʈ ����
            Vector3 worldPosition = Vector3.zero;
            if (gridVisualizer != null)
            {
                worldPosition = gridVisualizer.GridToWorldPosition(characterData.startPosition);
            }
            else
            {
                worldPosition = new Vector3(characterData.startPosition.x, characterData.startPosition.y, 0);
            }

            GameObject characterObj = Instantiate(characterPrefab, worldPosition, Quaternion.identity, charactersParent);

            // GamePiece ������Ʈ ����
            GamePiece GamePiece = characterObj.GetComponent<GamePiece>();
            if (GamePiece == null)
            {
                GamePiece = characterObj.AddComponent<GamePiece>();
            }

            GamePiece.Initialize(
                characterData.characterId,
                characterData.startPosition,
                characterData.maxSelections,
                characterData.characterColor,
                currentLevelData.moveSpeed
            );

            spawnedCharacters.Add(GamePiece);
        }

        Debug.Log($"Spawned {spawnedCharacters.Count} characters");
    }

    /// <summary>
    /// ���������� Scene�� ����
    /// </summary>
    private void SpawnGoals()
    {
        if (goalPrefab == null)
        {
            Debug.LogError("Goal prefab is not assigned!");
            return;
        }

        for (int i = 0; i < currentLevelData.goals.Count; i++)
        {
            var goalData = currentLevelData.goals[i];

            // ������ ������Ʈ ����
            Vector3 worldPosition = Vector3.zero;
            if (gridVisualizer != null)
            {
                worldPosition = gridVisualizer.GridToWorldPosition(goalData.position);
            }
            else
            {
                worldPosition = new Vector3(goalData.position.x, goalData.position.y, 0);
            }

            GameObject goalObj = Instantiate(goalPrefab, worldPosition, Quaternion.identity, goalsParent);

            // GoalController ������Ʈ ����
            GoalController goalController = goalObj.GetComponent<GoalController>();
            if (goalController == null)
            {
                goalController = goalObj.AddComponent<GoalController>();
            }

            goalController.Initialize(
                i,
                goalData.position,
                goalData.goalType,
                goalData.goalColor,
                goalData.assignedCharacters
            );

            spawnedGoals.Add(goalController);
        }

        Debug.Log($"Spawned {spawnedGoals.Count} goals");
    }

    /// <summary>
    /// ĳ���Ϳ� ������ ���� �Ҵ� ���� ����
    /// </summary>
    private void SetupCharacterGoalAssignments()
    {
        foreach (var character in spawnedCharacters)
        {
            string characterId = character.GetCharacterId();

            // LevelData���� �ش� ĳ������ �Ҵ� ���� ã��
            var characterData = currentLevelData.characters.Find(c => c.characterId == characterId);

            if (characterData.assignedGoalIndex >= 0 && characterData.assignedGoalIndex < spawnedGoals.Count)
            {
                // ���� ������ �Ҵ�
                spawnedGoals[characterData.assignedGoalIndex].AssignCharacter(characterId);
            }
            else
            {
                // ���� �������� ���� �������� ��� ��� �ش� �������� �Ҵ�
                foreach (var goal in spawnedGoals)
                {
                    if (goal.GetGoalType() == GoalType.Shared || goal.GetGoalType() == GoalType.Single)
                    {
                        goal.AssignCharacter(characterId);
                    }
                }
            }
        }

        Debug.Log("Character-Goal assignments completed");
    }

    /// <summary>
    /// ���� ���� ���� �α� ���
    /// </summary>
    private void LogLevelStatus()
    {
        Debug.Log($"=== Level Status ===");
        Debug.Log($"Characters spawned: {spawnedCharacters.Count}");
        Debug.Log($"Goals spawned: {spawnedGoals.Count}");
        Debug.Log($"Systems connected: PathValidator={pathValidator != null}, CollisionDetector={collisionDetector != null}, MovementSimulator={movementSimulator != null}");

        foreach (var character in spawnedCharacters)
        {
            character.LogCharacterInfo();
        }

        foreach (var goal in spawnedGoals)
        {
            goal.LogGoalInfo();
        }
    }

    /// <summary>
    /// ������ ĳ���� ��� ��ȯ
    /// </summary>
    public List<GamePiece> GetSpawnedCharacters()
    {
        return new List<GamePiece>(spawnedCharacters);
    }

    /// <summary>
    /// ������ ������ ��� ��ȯ
    /// </summary>
    public List<GoalController> GetSpawnedGoals()
    {
        return new List<GoalController>(spawnedGoals);
    }

    /// <summary>
    /// Ư�� ��ġ�� �ִ� ĳ���� ã��
    /// </summary>
    public GamePiece GetCharacterAt(Vector3Int gridPosition)
    {
        return spawnedCharacters.Find(c => c.GetCurrentGridPosition() == gridPosition);
    }

    /// <summary>
    /// Ư�� ��ġ�� �ִ� ������ ã��
    /// </summary>
    public GoalController GetGoalAt(Vector3Int gridPosition)
    {
        return spawnedGoals.Find(g => g.GetGridPosition() == gridPosition);
    }

    /// <summary>
    /// ĳ���� ID�� ĳ���� ã��
    /// </summary>
    public GamePiece GetCharacterById(string characterId)
    {
        return spawnedCharacters.Find(c => c.GetCharacterId() == characterId);
    }

    /// <summary>
    /// ��� ĳ������ ���� ���� Ȯ�� (������)
    /// </summary>
    public void LogAllCharacterStates()
    {
        Debug.Log("=== All Character States ===");
        foreach (var character in spawnedCharacters)
        {
            Vector3Int pos = character.GetCurrentGridPosition();
            Debug.Log($"{character.GetCharacterId()}: Position({pos}), Remaining({character.GetRemainingSelections()}), Moving({character.IsMoving()})");
        }
    }

    /// <summary>
    /// ���� �Ϸ� ���� �˻� (���߿� ����)
    /// </summary>
    public bool CheckLevelComplete()
    {
        // TODO: ��� ĳ���Ͱ� �������� �����ߴ��� Ȯ���ϴ� ����
        return false;
    }

    /// <summary>
    /// ���� ���� (�׽�Ʈ��)
    /// </summary>
    public void ResetLevel()
    {
        if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Reload Current Level")]
    public void ReloadCurrentLevel()
    {
        if (currentLevelData != null)
        {
            LoadLevel(currentLevelData);
        }
        else
        {
            Debug.LogWarning("No current level data to reload!");
        }
    }

    [ContextMenu("Log All Character States")]
    public void EditorLogAllCharacterStates()
    {
        LogAllCharacterStates();
    }

    [ContextMenu("Test System Connections")]
    public void TestSystemConnections()
    {
        Debug.Log("=== System Connection Test ===");
        Debug.Log($"GridVisualizer: {gridVisualizer != null}");
        Debug.Log($"PathValidator: {pathValidator != null}");
        Debug.Log($"CollisionDetector: {collisionDetector != null}");
        Debug.Log($"MovementSimulator: {movementSimulator != null}");
        Debug.Log($"Current Level Data: {currentLevelData != null}");
    }
#endif
}