using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LevelData를 읽어서 Scene에 캐릭터와 목적지를 배치하는 시스템
/// 새로운 아키텍처와의 연동을 위해 수정됨
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

    // 런타임에 생성된 오브젝트들
    private List<GamePiece> spawnedCharacters = new List<GamePiece>();
    private List<GoalController> spawnedGoals = new List<GoalController>();

    // 참조
    private GridVisualizer gridVisualizer;

    // 새로운 시스템들에 대한 참조
    private PathValidator pathValidator;
    private CollisionDetector collisionDetector;
    private MovementSimulator movementSimulator;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

        // 새로운 시스템들 찾기
        pathValidator = FindFirstObjectByType<PathValidator>();
        collisionDetector = FindFirstObjectByType<CollisionDetector>();
        movementSimulator = FindFirstObjectByType<MovementSimulator>();

        // 부모 오브젝트가 없으면 생성
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
    /// 레벨 데이터를 로드하여 Scene에 배치
    /// </summary>
    public void LoadLevel(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData is null!");
            return;
        }

        // 기존 오브젝트들 정리
        ClearCurrentLevel();

        currentLevelData = levelData;

        // 새로운 시스템들에 레벨 데이터 전달
        NotifySystemsOfLevelData();

        // GridVisualizer 설정 업데이트
        UpdateGridVisualizerSettings();

        // 캐릭터들 생성
        SpawnCharacters();

        // 목적지들 생성
        SpawnGoals();

        // 캐릭터-목적지 할당 설정
        SetupCharacterGoalAssignments();

        Debug.Log($"Level {levelData.levelNumber} loaded successfully!");
        LogLevelStatus();
    }

    /// <summary>
    /// 새로운 시스템들에 레벨 데이터 전달
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

        // CollisionDetector는 다른 시스템들을 통해 간접적으로 데이터를 받음
    }

    /// <summary>
    /// 현재 레벨 데이터 반환 (다른 시스템에서 사용)
    /// </summary>
    public LevelData GetCurrentLevelData()
    {
        return currentLevelData;
    }

    /// <summary>
    /// 현재 레벨의 모든 오브젝트 정리
    /// </summary>
    private void ClearCurrentLevel()
    {
        // 기존 캐릭터들 삭제
        foreach (var character in spawnedCharacters)
        {
            if (character != null)
                DestroyImmediate(character.gameObject);
        }
        spawnedCharacters.Clear();

        // 기존 목적지들 삭제
        foreach (var goal in spawnedGoals)
        {
            if (goal != null)
                DestroyImmediate(goal.gameObject);
        }
        spawnedGoals.Clear();
    }

    /// <summary>
    /// GridVisualizer 설정을 LevelData에 맞춰 업데이트
    /// </summary>
    private void UpdateGridVisualizerSettings()
    {
        if (gridVisualizer == null) return;

        // 수동으로 설정하도록 안내 (GridVisualizer에 public 설정 메서드가 있다면 여기서 호출)
        Debug.Log($"Please update GridVisualizer settings:");
        Debug.Log($"Grid Width: {currentLevelData.gridWidth}");
        Debug.Log($"Grid Height: {currentLevelData.gridHeight}");
        Debug.Log($"Cell Size: {currentLevelData.cellSize}");
        Debug.Log($"Grid Origin: {currentLevelData.gridOrigin}");

        // GridVisualizer에 설정 업데이트 메서드가 있다면 주석 해제
        // gridVisualizer.UpdateGridSettings(
        //     currentLevelData.gridWidth, 
        //     currentLevelData.gridHeight,
        //     currentLevelData.cellSize,
        //     currentLevelData.gridOrigin
        // );
    }

    /// <summary>
    /// 캐릭터들을 Scene에 생성
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
            // 캐릭터 오브젝트 생성
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

            // GamePiece 컴포넌트 설정
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
    /// 목적지들을 Scene에 생성
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

            // 목적지 오브젝트 생성
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

            // GoalController 컴포넌트 설정
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
    /// 캐릭터와 목적지 간의 할당 관계 설정
    /// </summary>
    private void SetupCharacterGoalAssignments()
    {
        foreach (var character in spawnedCharacters)
        {
            string characterId = character.GetCharacterId();

            // LevelData에서 해당 캐릭터의 할당 정보 찾기
            var characterData = currentLevelData.characters.Find(c => c.characterId == characterId);

            if (characterData.assignedGoalIndex >= 0 && characterData.assignedGoalIndex < spawnedGoals.Count)
            {
                // 개별 목적지 할당
                spawnedGoals[characterData.assignedGoalIndex].AssignCharacter(characterId);
            }
            else
            {
                // 공유 목적지나 단일 목적지의 경우 모든 해당 목적지에 할당
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
    /// 현재 레벨 상태 로그 출력
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
    /// 생성된 캐릭터 목록 반환
    /// </summary>
    public List<GamePiece> GetSpawnedCharacters()
    {
        return new List<GamePiece>(spawnedCharacters);
    }

    /// <summary>
    /// 생성된 목적지 목록 반환
    /// </summary>
    public List<GoalController> GetSpawnedGoals()
    {
        return new List<GoalController>(spawnedGoals);
    }

    /// <summary>
    /// 특정 위치에 있는 캐릭터 찾기
    /// </summary>
    public GamePiece GetCharacterAt(Vector3Int gridPosition)
    {
        return spawnedCharacters.Find(c => c.GetCurrentGridPosition() == gridPosition);
    }

    /// <summary>
    /// 특정 위치에 있는 목적지 찾기
    /// </summary>
    public GoalController GetGoalAt(Vector3Int gridPosition)
    {
        return spawnedGoals.Find(g => g.GetGridPosition() == gridPosition);
    }

    /// <summary>
    /// 캐릭터 ID로 캐릭터 찾기
    /// </summary>
    public GamePiece GetCharacterById(string characterId)
    {
        return spawnedCharacters.Find(c => c.GetCharacterId() == characterId);
    }

    /// <summary>
    /// 모든 캐릭터의 현재 상태 확인 (디버깅용)
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
    /// 레벨 완료 조건 검사 (나중에 구현)
    /// </summary>
    public bool CheckLevelComplete()
    {
        // TODO: 모든 캐릭터가 목적지에 도달했는지 확인하는 로직
        return false;
    }

    /// <summary>
    /// 레벨 리셋 (테스트용)
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