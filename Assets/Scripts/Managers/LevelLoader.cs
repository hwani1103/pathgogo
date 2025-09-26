using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class LevelLoader : MonoBehaviour
{
    [Header("Level Data")]
    [SerializeField] private LevelData[] levelDataArray;
    [SerializeField] private int currentLevelIndex = 0;

    [Header("Prefabs")]
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject goalPrefab;

    [Header("Runtime Objects")]
    [SerializeField] private Transform charactersParent;
    [SerializeField] private Transform goalsParent;

    [Header("Tilemap References")]
    [SerializeField] private Tilemap targetTilemap;
    [SerializeField] private Grid targetGrid;

    private List<GamePiece> spawnedCharacters = new List<GamePiece>();
    private List<GoalController> spawnedGoals = new List<GoalController>();
    private LevelData currentLevelData;

    private GridVisualizer gridVisualizer;
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;
    private TestUIManager testUIManager;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        pathValidator = FindFirstObjectByType<PathValidator>();
        collisionDetector = FindFirstObjectByType<CollisionDetector>();
        movementSimulator = FindFirstObjectByType<MovementSimulator>();
        testUIManager = FindFirstObjectByType<TestUIManager>();

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

        if (targetTilemap == null)
            targetTilemap = FindFirstObjectByType<Tilemap>();
        if (targetGrid == null)
            targetGrid = FindFirstObjectByType<Grid>();
    }

    void Start()
    {
        LoadCurrentLevel();
    }

    private void LoadCurrentLevel()
    {
        if (levelDataArray == null || levelDataArray.Length == 0)
        {
            Debug.LogError("No level data assigned!");
            return;
        }

        if (currentLevelIndex >= levelDataArray.Length)
        {
            Debug.LogError($"Level index {currentLevelIndex} out of range!");
            return;
        }

        LoadLevel(levelDataArray[currentLevelIndex]);
    }

    public void LoadNextLevel()
    {
        if (levelDataArray == null || levelDataArray.Length == 0) return;

        currentLevelIndex++;

        if (currentLevelIndex >= levelDataArray.Length)
        {
            currentLevelIndex = levelDataArray.Length - 1;

            if (testUIManager != null)
            {
                testUIManager.ShowAllLevelsCompleted();
            }
            return;
        }

        LoadCurrentLevel();
    }

    public void RestartCurrentLevel()
    {
        LoadCurrentLevel();
    }

    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null!");
            return;
        }

        var gameManager = FindFirstObjectByType<MovementGameManager>();
        if (gameManager != null)
        {
            gameManager.OnLevelChanged();
        }

        ClearCurrentLevel();
        currentLevelData = levelData;
        NotifySystemsOfLevelData();
        RestoreTilemap();
        SpawnCharacters();
        SpawnGoals();
        SetupCharacterGoalAssignments();

        if (gameManager != null)
        {
            gameManager.InitializeForNewLevel();
        }
    }

    public void OnLevelCleared()
    {
        if (testUIManager != null)
        {
            testUIManager.ShowLevelClearedUI();
        }
    }

    public void OnGameOver(string message)
    {
        if (testUIManager != null)
        {
            testUIManager.ShowGameOverUI(message);
        }
    }

    private void RestoreTilemap()
    {
        if (currentLevelData == null || targetTilemap == null) return;

        ClearExistingTilemap();

        foreach (var tileData in currentLevelData.walkableTiles)
        {
            TileBase tileAsset = currentLevelData.GetTileAsset(tileData.tileTypeId);

            if (tileAsset != null)
            {
                targetTilemap.SetTile(tileData.position, tileAsset);
            }
        }

        if (targetGrid != null)
        {
            targetGrid.cellSize = new Vector3(currentLevelData.cellSize, currentLevelData.cellSize, 1f);
            targetGrid.transform.position = currentLevelData.gridOrigin;
        }

        targetTilemap.CompressBounds();
    }

    private void ClearExistingTilemap()
    {
        if (targetTilemap == null) return;

        BoundsInt bounds = targetTilemap.cellBounds;

        if (bounds.size.x > 0 && bounds.size.y > 0)
        {
            TileBase[] emptyTiles = new TileBase[bounds.size.x * bounds.size.y * bounds.size.z];
            targetTilemap.SetTilesBlock(bounds, emptyTiles);
        }

        targetTilemap.CompressBounds();
    }

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
    }

    private void ClearCurrentLevel()
    {
        foreach (var character in spawnedCharacters)
        {
            if (character != null)
                DestroyImmediate(character.gameObject);
        }
        spawnedCharacters.Clear();

        foreach (var goal in spawnedGoals)
        {
            if (goal != null)
                DestroyImmediate(goal.gameObject);
        }
        spawnedGoals.Clear();
    }

    private void SpawnCharacters()
    {
        if (characterPrefab == null) return;

        foreach (var characterData in currentLevelData.characters)
        {
            Vector3 worldPosition = gridVisualizer != null
                ? gridVisualizer.GridToWorldPosition(characterData.startPosition)
                : new Vector3(characterData.startPosition.x, characterData.startPosition.y, 0);

            GameObject characterObj = Instantiate(characterPrefab, worldPosition, Quaternion.identity, charactersParent);

            GamePiece gamePiece = characterObj.GetComponent<GamePiece>();
            if (gamePiece == null)
            {
                gamePiece = characterObj.AddComponent<GamePiece>();
            }

            Color characterColor = characterData.characterColor;
            if (characterColor.a == 0)
            {
                characterColor = GetDefaultCharacterColor(characterData.characterId);
            }

            gamePiece.Initialize(
                characterData.characterId,
                characterData.startPosition,
                characterData.maxSelections,
                characterColor,
                currentLevelData.moveSpeed
            );

            spawnedCharacters.Add(gamePiece);
        }
    }

    private void SpawnGoals()
    {
        if (goalPrefab == null) return;

        for (int i = 0; i < currentLevelData.goals.Count; i++)
        {
            var goalData = currentLevelData.goals[i];

            Vector3 worldPosition = gridVisualizer != null
                ? gridVisualizer.GridToWorldPosition(goalData.position)
                : new Vector3(goalData.position.x, goalData.position.y, 0);

            GameObject goalObj = Instantiate(goalPrefab, worldPosition, Quaternion.identity, goalsParent);

            GoalController goalController = goalObj.GetComponent<GoalController>();
            if (goalController == null)
            {
                goalController = goalObj.AddComponent<GoalController>();
            }

            Color goalColor = goalData.goalColor;
            if (goalColor.a == 0)
            {
                goalColor = Color.white;
            }

            goalController.Initialize(
                i,
                goalData.position,
                goalData.goalType,
                goalColor,
                goalData.assignedCharacters
            );

            spawnedGoals.Add(goalController);
        }
    }

    private Color GetDefaultCharacterColor(string characterId)
    {
        switch (characterId)
        {
            case "A": return Color.red;
            case "B": return Color.blue;
            case "C": return Color.green;
            case "D": return Color.yellow;
            default: return Color.white;
        }
    }

    private void SetupCharacterGoalAssignments()
    {
        foreach (var character in spawnedCharacters)
        {
            string characterId = character.GetCharacterId();
            var characterData = currentLevelData.characters.Find(c => c.characterId == characterId);

            if (characterData.assignedGoalIndex >= 0 && characterData.assignedGoalIndex < spawnedGoals.Count)
            {
                spawnedGoals[characterData.assignedGoalIndex].AssignCharacter(characterId);
            }
            else
            {
                foreach (var goal in spawnedGoals)
                {
                    if (goal.GetGoalType() == GoalType.Shared || goal.GetGoalType() == GoalType.Single)
                    {
                        goal.AssignCharacter(characterId);
                    }
                }
            }
        }
    }

    public LevelData GetCurrentLevelData() => currentLevelData;
    public List<GamePiece> GetSpawnedCharacters() => new List<GamePiece>(spawnedCharacters);
    public List<GoalController> GetSpawnedGoals() => new List<GoalController>(spawnedGoals);
    public GamePiece GetCharacterAt(Vector3Int gridPosition) => spawnedCharacters.Find(c => c.GetCurrentGridPosition() == gridPosition);
    public GoalController GetGoalAt(Vector3Int gridPosition) => spawnedGoals.Find(g => g.GetGridPosition() == gridPosition);
    public GamePiece GetCharacterById(string characterId) => spawnedCharacters.Find(c => c.GetCharacterId() == characterId);
}