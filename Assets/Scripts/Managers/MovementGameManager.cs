using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ĳ���� �̵��� �浹�� ���� �����ϴ� ���� �Ŵ���
/// Unity ���� �浹 �ý��۸� ���
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
    private Dictionary<string, CharacterCollisionDetector> collisionDetectors;

    private GridVisualizer gridVisualizer;

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
        collisionDetectors = new Dictionary<string, CharacterCollisionDetector>();
    }

    void Start()
    {
        InitializeCharacterMovers();
        SetupCollisionDetection();
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
    /// ��� ĳ���Ϳ� CharacterMover �� ���� ������Ʈ �߰�
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

            // ���� �浹�� Collider �߰�
            var collider = character.GetComponent<CircleCollider2D>();
            if (collider == null)
            {
                collider = character.gameObject.AddComponent<CircleCollider2D>();
                collider.radius = 0.35f; // Ÿ�� ũ���� 70% ����
                collider.isTrigger = true;
            }

            // Rigidbody2D �߰�
            var rigidbody = character.GetComponent<Rigidbody2D>();
            if (rigidbody == null)
            {
                rigidbody = character.gameObject.AddComponent<Rigidbody2D>();
                rigidbody.gravityScale = 0;
                rigidbody.freezeRotation = true;
            }
        }
    }

    /// <summary>
    /// ���� ��� �浹 ���� �ý��� ����
    /// </summary>
    private void SetupCollisionDetection()
    {
        var characters = levelLoader.GetSpawnedCharacters();

        foreach (var character in characters)
        {
            var detector = character.GetComponent<CharacterCollisionDetector>();
            if (detector == null)
            {
                detector = character.gameObject.AddComponent<CharacterCollisionDetector>();
            }

            detector.Initialize(this, character.GetCharacterId());
            collisionDetectors[character.GetCharacterId()] = detector;
        }
    }

    /// <summary>
    /// �浹 ���� Ȱ��ȭ/��Ȱ��ȭ
    /// </summary>
    private void SetCollisionDetectionEnabled(bool enabled)
    {
        foreach (var detector in collisionDetectors.Values)
        {
            detector.SetCollisionEnabled(enabled);
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

        // �浹 ���� Ȱ��ȭ
        SetCollisionDetectionEnabled(true);

        yield return new WaitForSeconds(1f);

        // ��� ĳ���� �̵� ����
        StartAllCharacterMovement(characterPaths);

        // ��� ĳ���� �̵� �Ϸ���� ���
        yield return StartCoroutine(WaitForAllMovementComplete());

        // ���� ���� ó��
        OnGameComplete();
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
    /// ���� �浹 �̺�Ʈ ó�� (CharacterCollisionDetector���� ȣ��)
    /// </summary>
    public void OnCharacterCollision(string characterId1, string characterId2, Vector3 collisionPoint)
    {
        if (!gameInProgress || !movementStarted)
        {
            return;
        }

        // �̵� ���� ĳ���͵鸸 �浹�� ����
        bool char1Moving = characterMovers.ContainsKey(characterId1) && characterMovers[characterId1].IsMoving();
        bool char2Moving = characterMovers.ContainsKey(characterId2) && characterMovers[characterId2].IsMoving();

        if (!char1Moving || !char2Moving)
        {
            return;
        }

        // Goal ��ġ������ �浹�� ����
        Vector3Int gridPos = gridVisualizer.WorldToGridPosition(collisionPoint);
        if (IsGoalPosition(gridPos))
        {
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

            var rb = mover.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }

        // �浹 ���� ��Ȱ��ȭ
        SetCollisionDetectionEnabled(false);

        // ���� ���� ����
        gameInProgress = false;
        movementStarted = false;
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
        if (!gameInProgress && !movementStarted)
        {
            return;
        }
        SetCollisionDetectionEnabled(false);
        gameInProgress = false;

        // LevelLoader�� ���� Ŭ���� �˸�
        var levelLoader = FindFirstObjectByType<LevelLoader>();
        if (levelLoader != null)
        {
            levelLoader.OnLevelCleared();
        }
        else
        {
            ShowGameClearUI(); // ���� �޼��� ����
        }
    }
    /// <summary>
    /// ���� ���� �� ȣ�� - ��� ���� ����
    /// </summary>
    public void OnLevelChanged()
    {
        // ���� ���� ���� ���� �ߴ�
        StopAllCoroutines();

        // ���� ���� ����
        gameInProgress = false;
        movementStarted = false;

        // CharacterMover ���� ����
        if (characterMovers != null)
        {
            characterMovers.Clear();
        }

        if (collisionDetectors != null)
        {
            collisionDetectors.Clear();
        }

        Debug.Log("MovementGameManager cleaned up for level change");
    }

    /// <summary>
    /// ĳ���� ���� �ʱ�ȭ - �� ���� ���� ���� ȣ��
    /// </summary>
    public void InitializeForNewLevel()
    {
        // ���ο� ĳ���͵鿡 ���� CharacterMover ����
        InitializeCharacterMovers();
        SetupCollisionDetection();

        Debug.Log("MovementGameManager initialized for new level");
    }
    private void ShowGameOverUI(CollisionPredictor.CollisionEvent collision)
    {
        string message = $"Characters {string.Join(", ", collision.characterIds)} collided at {collision.position}";

        // LevelLoader�� ���� ���� �˸�
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
            testUIManager.ShowLevelClearedUI(); // �޼���� ����
        }
    }

    public void RestartGame()
    {
        foreach (var mover in characterMovers.Values)
        {
            mover.StopAllCoroutines();
        }

        SetCollisionDetectionEnabled(false);
        gameInProgress = false;
        movementStarted = false;

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

/// <summary>
/// �� ĳ���Ϳ� �����Ǿ� ���� �浹�� �����ϴ� ������Ʈ
/// </summary>
public class CharacterCollisionDetector : MonoBehaviour
{
    private MovementGameManager gameManager;
    private string characterId;
    private bool collisionEnabled = false;
    private HashSet<string> collidedWith = new HashSet<string>();

    public void Initialize(MovementGameManager manager, string id)
    {
        gameManager = manager;
        characterId = id;
    }

    public void SetCollisionEnabled(bool enabled)
    {
        collisionEnabled = enabled;
        if (!enabled)
        {
            collidedWith.Clear();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!collisionEnabled) return;

        var otherCharacter = other.GetComponent<GamePiece>();
        if (otherCharacter != null && gameManager != null)
        {
            string otherCharacterId = otherCharacter.GetCharacterId();

            if (otherCharacterId == characterId) return;

            if (collidedWith.Contains(otherCharacterId)) return;
            collidedWith.Add(otherCharacterId);

            Vector3 collisionPoint = (transform.position + other.transform.position) * 0.5f;
            gameManager.OnCharacterCollision(characterId, otherCharacterId, collisionPoint);
        }
    }
}