using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ���� �̵��� ���� ���¸� ���� �����ϴ� ���� �Ŵ���
/// ���� �浹 ���� �ý��� ���
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

    // ĳ���� �̵� ����
    private Dictionary<string, CharacterMover> characterMovers;
    private GridVisualizer gridVisualizer;

    // Race condition ����
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
    /// ��� ĳ���Ϳ� CharacterMover �߰� (���� ������Ʈ ����)
    /// </summary>
    private void InitializeCharacterMovers()
    {
        if (levelLoader == null) return;

        var characters = levelLoader.GetSpawnedCharacters();

        foreach (var character in characters)
        {
            // CharacterMover �߰�
            var mover = character.GetComponent<CharacterMover>();
            if (mover == null)
            {
                mover = character.gameObject.AddComponent<CharacterMover>();
            }
            characterMovers[character.GetCharacterId()] = mover;

            // ���� ������Ʈ ���� ����
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
    /// ���� ���� ������
    /// </summary>
    private IEnumerator GameSequence()
    {
        var characterPaths = CollectAllCharacterPaths();
        if (characterPaths.Count == 0)
        {
            yield break;
        }

        yield return new WaitForSeconds(1f);

        // ������ �ߴܵǾ����� üũ
        if (!gameInProgress)
        {
            yield break;
        }

        // ��� ĳ���� �̵� ����
        StartAllCharacterMovement(characterPaths);

        // ��� ĳ���� �̵� �Ϸ���� ���
        yield return StartCoroutine(WaitForAllMovementComplete());

        // ������ ������ ���� ���� ��츸 �Ϸ� ó��
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
    /// ���� �浹 �̺�Ʈ ó�� (CharacterMover���� ȣ��)
    /// Race condition ���� ���� ����
    /// </summary>
    public void OnCharacterCollision(string characterId1, string characterId2, Vector3 collisionPoint)
    {
        // �̹� �浹 ó�� ���̸� ����
        if (collisionProcessing)
        {
            return;
        }

        if (!gameInProgress || !movementStarted)
        {
            return;
        }

        // �浹 ó�� ���� �÷���
        collisionProcessing = true;

        // �̵� ���� ĳ���͵鸸 �浹�� ����
        bool char1Moving = characterMovers.ContainsKey(characterId1) && characterMovers[characterId1].IsMoving();
        bool char2Moving = characterMovers.ContainsKey(characterId2) && characterMovers[characterId2].IsMoving();

        if (!char1Moving || !char2Moving)
        {
            collisionProcessing = false;
            return;
        }

        // Goal ��ġ������ �浹�� ����
        Vector3Int gridPos = gridVisualizer.WorldToGridPosition(collisionPoint);
        if (IsGoalPosition(gridPos))
        {
            collisionProcessing = false;
            return;
        }

        // ��� ���� �ߴ�
        StopGameImmediately();

        // �浹 �̺�Ʈ ����
        var collision = new CollisionPredictor.CollisionEvent(
            Time.time,
            gridPos,
            new List<string> { characterId1, characterId2 }
        );

        ShowGameOverUI(collision);
    }

    /// <summary>
    /// ��� ���� �ߴ�
    /// </summary>
    private void StopGameImmediately()
    {
        // ��� ĳ���� �̵� �ߴ�
        foreach (var mover in characterMovers.Values)
        {
            mover.StopAllCoroutines();
        }

        // ���� ���� ���� (���� �߿�)
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

        // movementStarted ���� ����
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
    /// ���� ���� �� ȣ�� - ��� ���� ����
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
    /// ĳ���� ���� �ʱ�ȭ - �� ���� ���� ���� ȣ��
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