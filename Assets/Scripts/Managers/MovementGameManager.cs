using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 이동과 게임 상태를 통합 관리하는 게임 매니저
/// 수동 충돌 감지 시스템 사용
/// </summary>
public class MovementGameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PathSelectionManager pathSelectionManager;
    [SerializeField] private LevelLoader levelLoader;
    [SerializeField] private TestUIManager testUIManager;

    [Header("Game State")]
    [SerializeField] private bool gameInProgress = false;
    [SerializeField] private bool movementStarted = false;

    // 캐릭터 이동 관리
    private Dictionary<string, CharacterMover> characterMovers;
    private GridVisualizer gridVisualizer;

    // Race condition 방지
    private bool collisionProcessing = false;

    void Awake()
    {
        if (pathSelectionManager == null)
            pathSelectionManager = FindFirstObjectByType<PathSelectionManager>();
        if (levelLoader == null)
            levelLoader = FindFirstObjectByType<LevelLoader>();
        if (testUIManager == null)
            testUIManager = FindFirstObjectByType<TestUIManager>();

        gridVisualizer = FindFirstObjectByType<GridVisualizer>();
        characterMovers = new Dictionary<string, CharacterMover>();
    }

    void Start()
    {
        InitializeCharacterMovers();
    }

    public bool IsGameInProgress()
    {
        return gameInProgress;
    }

    public void OnPathCompleted()
    {
        bool allComplete = AreAllPathsComplete();

        if (allComplete && !gameInProgress)
        {
            StartGame();
        }
    }

    /// <summary>
    /// 모든 캐릭터에 CharacterMover 추가 (물리 컴포넌트 제거)
    /// </summary>
    private void InitializeCharacterMovers()
    {
        if (levelLoader == null) return;

        var characters = levelLoader.GetSpawnedCharacters();

        foreach (var character in characters)
        {
            // CharacterMover 추가
            var mover = character.GetComponent<CharacterMover>();
            if (mover == null)
            {
                mover = character.gameObject.AddComponent<CharacterMover>();
            }
            characterMovers[character.GetCharacterId()] = mover;

            // 물리 컴포넌트 완전 제거
            var collider = character.GetComponent<CircleCollider2D>();
            if (collider != null)
                DestroyImmediate(collider);

            var rigidbody = character.GetComponent<Rigidbody2D>();
            if (rigidbody != null)
                DestroyImmediate(rigidbody);
        }
    }

    public bool AreAllPathsComplete()
    {
        if (levelLoader == null) return false;

        var characters = levelLoader.GetSpawnedCharacters();
        foreach (var character in characters)
        {
            if (!character.IsCompleted())
            {
                return false;
            }
        }

        return characters.Count > 0;
    }

    public void StartGame()
    {
        if (gameInProgress)
        {
            return;
        }

        if (!AreAllPathsComplete())
        {
            return;
        }

        gameInProgress = true;
        StartCoroutine(GameSequence());
    }

    /// <summary>
    /// 게임 진행 시퀀스
    /// </summary>
    private IEnumerator GameSequence()
    {
        var characterPaths = CollectAllCharacterPaths();
        if (characterPaths.Count == 0)
        {
            yield break;
        }

        yield return new WaitForSeconds(1f);

        // 게임이 중단되었는지 체크
        if (!gameInProgress)
        {
            yield break;
        }

        // 모든 캐릭터 이동 시작
        StartAllCharacterMovement(characterPaths);

        // 모든 캐릭터 이동 완료까지 대기
        yield return StartCoroutine(WaitForAllMovementComplete());

        // 게임이 여전히 진행 중인 경우만 완료 처리
        if (gameInProgress && !movementStarted)
        {
            OnGameComplete();
        }
    }

    private Dictionary<string, List<Vector3Int>> CollectAllCharacterPaths()
    {
        if (pathSelectionManager == null)
        {
            return new Dictionary<string, List<Vector3Int>>();
        }

        return pathSelectionManager.GetCompletedPaths();
    }

    private void StartAllCharacterMovement(Dictionary<string, List<Vector3Int>> characterPaths)
    {
        movementStarted = true;

        foreach (var kvp in characterPaths)
        {
            string characterId = kvp.Key;
            List<Vector3Int> path = kvp.Value;

            if (characterMovers.ContainsKey(characterId))
            {
                characterMovers[characterId].StartMovement(path);
            }
            else
            {
                var characters = levelLoader.GetSpawnedCharacters();
                var character = characters.Find(c => c.GetCharacterId() == characterId);
                if (character != null)
                {
                    var mover = character.gameObject.AddComponent<CharacterMover>();
                    characterMovers[characterId] = mover;
                    mover.StartMovement(path);
                }
            }
        }
    }

    /// <summary>
    /// 수동 충돌 이벤트 처리 (CharacterMover에서 호출)
    /// Race condition 방지 로직 포함
    /// </summary>
    public void OnCharacterCollision(string characterId1, string characterId2, Vector3 collisionPoint)
    {
        // 이미 충돌 처리 중이면 무시
        if (collisionProcessing)
        {
            return;
        }

        if (!gameInProgress || !movementStarted)
        {
            return;
        }

        // 충돌 처리 시작 플래그
        collisionProcessing = true;

        // 이동 중인 캐릭터들만 충돌로 간주
        bool char1Moving = characterMovers.ContainsKey(characterId1) && characterMovers[characterId1].IsMoving();
        bool char2Moving = characterMovers.ContainsKey(characterId2) && characterMovers[characterId2].IsMoving();

        if (!char1Moving || !char2Moving)
        {
            collisionProcessing = false;
            return;
        }

        // Goal 위치에서의 충돌은 무시
        Vector3Int gridPos = gridVisualizer.WorldToGridPosition(collisionPoint);
        if (IsGoalPosition(gridPos))
        {
            collisionProcessing = false;
            return;
        }

        // 즉시 게임 중단
        StopGameImmediately();

        // 충돌 이벤트 생성
        var collision = new CollisionPredictor.CollisionEvent(
            Time.time,
            gridPos,
            new List<string> { characterId1, characterId2 }
        );

        ShowGameOverUI(collision);
    }

    /// <summary>
    /// 즉시 게임 중단
    /// </summary>
    private void StopGameImmediately()
    {
        // 모든 캐릭터 이동 중단
        foreach (var mover in characterMovers.Values)
        {
            mover.StopAllCoroutines();
        }

        // 게임 상태 변경 (순서 중요)
        movementStarted = false;
        gameInProgress = false;
    }

    private bool IsGoalPosition(Vector3Int position)
    {
        if (levelLoader == null) return false;

        var goals = levelLoader.GetSpawnedGoals();
        foreach (var goal in goals)
        {
            if (goal.GetGridPosition() == position)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerator WaitForAllMovementComplete()
    {
        while (movementStarted)
        {
            bool allComplete = true;

            foreach (var mover in characterMovers.Values)
            {
                if (mover.IsMoving())
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete)
            {
                movementStarted = false;
                break;
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private void OnGameComplete()
    {
        gameInProgress = false;

        // movementStarted 조건 제거
        var levelLoader = FindFirstObjectByType<LevelLoader>();
        if (levelLoader != null)
        {
            levelLoader.OnLevelCleared();
        }
        else
        {
            ShowGameClearUI();
        }
    }

    /// <summary>
    /// 레벨 변경 시 호출 - 모든 참조 정리
    /// </summary>
    public void OnLevelChanged()
    {
        StopAllCoroutines();

        gameInProgress = false;
        movementStarted = false;
        collisionProcessing = false;

        if (characterMovers != null)
        {
            characterMovers.Clear();
        }
    }

    /// <summary>
    /// 캐릭터 무버 초기화 - 새 레벨 시작 전에 호출
    /// </summary>
    public void InitializeForNewLevel()
    {
        InitializeCharacterMovers();
    }

    private void ShowGameOverUI(CollisionPredictor.CollisionEvent collision)
    {
        string message = $"Characters {string.Join(", ", collision.characterIds)} collided at {collision.position}";

        var levelLoader = FindFirstObjectByType<LevelLoader>();
        if (levelLoader != null)
        {
            levelLoader.OnGameOver(message);
        }
        else if (testUIManager != null)
        {
            testUIManager.ShowGameOverUI(message);
        }
    }

    private void ShowGameClearUI()
    {
        if (testUIManager != null)
        {
            testUIManager.ShowLevelClearedUI();
        }
    }

    public void RestartGame()
    {
        foreach (var mover in characterMovers.Values)
        {
            mover.StopAllCoroutines();
        }

        gameInProgress = false;
        movementStarted = false;
        collisionProcessing = false;

        ResetCharacterPositions();
    }

    private void ResetCharacterPositions()
    {
        if (levelLoader == null) return;

        var characters = levelLoader.GetSpawnedCharacters();
        foreach (var character in characters)
        {
            string characterId = character.GetCharacterId();
            Vector3Int startPos = GetCharacterStartPosition(character);

            if (characterMovers.ContainsKey(characterId))
            {
                characterMovers[characterId].TeleportToPosition(startPos);
            }

            character.SetCompleted(false);
        }

        if (pathSelectionManager != null)
        {
            pathSelectionManager.ClearAllCompletedPaths();
        }
    }

    private Vector3Int GetCharacterStartPosition(GamePiece character)
    {
        if (levelLoader != null)
        {
            var levelData = levelLoader.GetCurrentLevelData();
            if (levelData != null)
            {
                string characterId = character.GetCharacterId();
                foreach (var charData in levelData.characters)
                {
                    if (charData.characterId == characterId)
                    {
                        return charData.startPosition;
                    }
                }
            }
        }

        return character.GetCurrentGridPosition();
    }
}