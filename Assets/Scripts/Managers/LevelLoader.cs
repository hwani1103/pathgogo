using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LevelData를 읽어서 Scene에 캐릭터와 목적지를 배치하는 시스템
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
    private List<CharacterController> spawnedCharacters = new List<CharacterController>();
    private List<GoalController> spawnedGoals = new List<GoalController>();

    // 참조
    private GridVisualizer gridVisualizer;

    void Awake()
    {
        gridVisualizer = FindFirstObjectByType<GridVisualizer>();

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

        // GridVisualizer의 설정을 직접 업데이트하려면 public 필드나 메서드 필요
        // 현재는 수동으로 설정하도록 안내
        Debug.Log($"Please update GridVisualizer settings:");
        Debug.Log($"Grid Width: {currentLevelData.gridWidth}");
        Debug.Log($"Grid Height: {currentLevelData.gridHeight}");
        Debug.Log($"Cell Size: {currentLevelData.cellSize}");
        Debug.Log($"Grid Origin: {currentLevelData.gridOrigin}");
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

            // CharacterController 컴포넌트 설정
            CharacterController characterController = characterObj.GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = characterObj.AddComponent<CharacterController>();
            }

            characterController.Initialize(
                characterData.characterId,
                characterData.startPosition,
                characterData.maxSelections,
                characterData.characterColor,
                currentLevelData.moveSpeed
            );

            spawnedCharacters.Add(characterController);
        }
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
    }

    /// <summary>
    /// 현재 레벨 상태 로그 출력
    /// </summary>
    private void LogLevelStatus()
    {
        Debug.Log($"=== Level Status ===");
        Debug.Log($"Characters spawned: {spawnedCharacters.Count}");
        Debug.Log($"Goals spawned: {spawnedGoals.Count}");

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
    public List<CharacterController> GetSpawnedCharacters()
    {
        return new List<CharacterController>(spawnedCharacters);
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
    public CharacterController GetCharacterAt(Vector3Int gridPosition)
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
#endif
}