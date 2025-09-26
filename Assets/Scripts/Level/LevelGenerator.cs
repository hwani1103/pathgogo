using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unity Editor���� ������ �����ϰ� �����ϴ� �ý���
/// Tilemap�� �о LevelData ScriptableObject�� ��ȯ�մϴ�
/// </summary>
public class LevelGenerator : MonoBehaviour
{
    [Header("Tilemap References")]
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private Grid grid;

    [Header("Generation Settings")]
    [SerializeField] private int levelNumber = 1;
    [SerializeField] private string levelName = "Level 1";
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private bool showGridInGame = false;

    [Header("Character Setup")]
    [SerializeField] private List<CharacterSetup> characterSetups = new List<CharacterSetup>();

    [Header("Goal Setup")]
    [SerializeField] private List<GoalSetup> goalSetups = new List<GoalSetup>();

    [System.Serializable]
    public struct CharacterSetup
    {
        public string characterId;
        public Vector3Int startPosition;
        public int maxSelections;
        public Color characterColor;
        public int assignedGoalIndex; // -1 for shared goals

        public CharacterSetup(string id, Vector3Int startPos, int maxSel, Color color, int goalIndex = -1)
        {
            characterId = id;
            startPosition = startPos;
            maxSelections = maxSel;
            characterColor = color;
            assignedGoalIndex = goalIndex;
        }
    }

    [System.Serializable]
    public struct GoalSetup
    {
        public Vector3Int position;
        public GoalType goalType;
        public Color goalColor;

        public GoalSetup(Vector3Int pos, GoalType type, Color color)
        {
            position = pos;
            goalType = type;
            goalColor = color;
        }
    }

    void Start()
    {
        // �ڵ����� Grid�� Tilemap ã��
        if (grid == null)
            grid = FindFirstObjectByType<Grid>();

        if (tilemap == null)
        {
            var tilemaps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
            if (tilemaps.Length > 0)
                tilemap = tilemaps[0];
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Level Data")]
    public void GenerateLevelData()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap�� �������� �ʾҽ��ϴ�!");
            return;
        }

        if (grid == null)
        {
            Debug.LogError("Grid�� �������� �ʾҽ��ϴ�!");
            return;
        }

        // LevelData ����
        LevelData levelData = CreateLevelDataFromScene();

        // ScriptableObject�� ����
        SaveLevelData(levelData);

        Debug.Log($"���� ������ ���� �Ϸ�: {levelData.name}");
        levelData.LogLevelInfo();
    }

    private LevelData CreateLevelDataFromScene()
    {
        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();

        // �⺻ ���� ����
        levelData.levelNumber = levelNumber;
        levelData.levelName = levelName;
        levelData.moveSpeed = moveSpeed;
        levelData.showGridInGame = showGridInGame;

        // �׸��� ���� - Grid ������Ʈ���� ���� ��������
        levelData.cellSize = grid != null ? grid.cellSize.x : 1f;
        levelData.gridOrigin = transform.position;

        // Tilemap���� Ÿ�� ���� �о����
        ReadTilemapData(levelData);

        // ĳ���� ���� �߰�
        foreach (var setup in characterSetups)
        {
            levelData.AddCharacter(
                setup.characterId,
                setup.startPosition,
                setup.maxSelections,
                setup.characterColor,
                setup.assignedGoalIndex
            );
        }

        // ������ ���� �߰�
        foreach (var setup in goalSetups)
        {
            levelData.AddGoal(setup.position, setup.goalType, setup.goalColor);
        }

        // �׸��� ũ�� ���
        CalculateGridBounds(levelData);

        return levelData;
    }

    private void ReadTilemapData(LevelData levelData)
    {
        Debug.Log("=== Reading Tilemap Data ===");

        // ���� Ÿ�� ������ Ŭ����
        levelData.walkableTiles.Clear();
        levelData.tileTypes.Clear();

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] tiles = tilemap.GetTilesBlock(bounds);

        // Ÿ�� Ÿ�� ���� ����
        Dictionary<TileBase, int> tileToTypeId = new Dictionary<TileBase, int>();
        int nextTypeId = 0;

        Debug.Log($"Scanning tilemap bounds: {bounds}");
        Debug.Log($"Total tile slots: {tiles.Length}");

        int processedTiles = 0;

        for (int x = 0; x < bounds.size.x; x++)
        {
            for (int y = 0; y < bounds.size.y; y++)
            {
                TileBase tile = tiles[x + y * bounds.size.x];
                if (tile != null)
                {
                    Vector3Int position = new Vector3Int(
                        bounds.x + x,
                        bounds.y + y,
                        0
                    );

                    // ���ο� Ÿ�� Ÿ���̸� ���
                    if (!tileToTypeId.ContainsKey(tile))
                    {
                        tileToTypeId[tile] = nextTypeId;

                        // Ÿ�� ���� ��� ��������
                        string assetPath = "";
#if UNITY_EDITOR
                        assetPath = UnityEditor.AssetDatabase.GetAssetPath(tile);
#endif

                        levelData.AddTileType(
                            nextTypeId,
                            tile.name,
                            assetPath,
                            true  // �⺻������ ��� Ÿ���� �̵� ����
                        );

                        Debug.Log($"New tile type registered: ID={nextTypeId}, Name={tile.name}, Path={assetPath}");
                        nextTypeId++;
                    }

                    // Ÿ�� ������ ����
                    levelData.AddTile(
                        position,
                        true,
                        tile.name,
                        tileToTypeId[tile]
                    );

                    processedTiles++;
                }
            }
        }

        Debug.Log($"=== Tilemap Reading Complete ===");
        Debug.Log($"Processed tiles: {processedTiles}");
        Debug.Log($"Unique tile types: {levelData.tileTypes.Count}");
        Debug.Log($"Total saved tiles: {levelData.walkableTiles.Count}");

        // Ÿ�� Ÿ�� ���� ���
        foreach (var tileType in levelData.tileTypes)
        {
            int count = levelData.walkableTiles.FindAll(t => t.tileTypeId == tileType.typeId).Count;
            Debug.Log($"  - {tileType.typeName} (ID: {tileType.typeId}): {count} tiles");
        }
    }

    private void CalculateGridBounds(LevelData levelData)
    {
        if (levelData.walkableTiles.Count == 0)
        {
            levelData.gridWidth = 10;
            levelData.gridHeight = 10;
            return;
        }

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var tile in levelData.walkableTiles)
        {
            if (tile.position.x < minX) minX = tile.position.x;
            if (tile.position.x > maxX) maxX = tile.position.x;
            if (tile.position.y < minY) minY = tile.position.y;
            if (tile.position.y > maxY) maxY = tile.position.y;
        }

        levelData.gridWidth = maxX - minX + 1;
        levelData.gridHeight = maxY - minY + 1;
        levelData.gridOrigin = new Vector3(minX, minY, 0);
    }

    private void SaveLevelData(LevelData levelData)
    {
        string path = $"Assets/ScriptableObjects/LevelData/Level_{levelNumber:D2}.asset";

        // ������ ������ ����
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // ScriptableObject ����
        AssetDatabase.CreateAsset(levelData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Inspector���� ����
        Selection.activeObject = levelData;

        Debug.Log($"LevelData saved to: {path}");
    }

    [ContextMenu("Add Sample Character")]
    public void AddSampleCharacter()
    {
        Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow };
        string[] ids = { "A", "B", "C", "D" };

        int index = characterSetups.Count;
        if (index < 4)
        {
            characterSetups.Add(new CharacterSetup(
                ids[index],
                new Vector3Int(index, 0, 0),
                3,
                colors[index],
                -1
            ));
        }
    }

    [ContextMenu("Add Sample Goal")]
    public void AddSampleGoal()
    {
        goalSetups.Add(new GoalSetup(
            new Vector3Int(5, 5, 0),
            GoalType.Individual,
            Color.white
        ));
    }

    [ContextMenu("Clear Setup Data")]
    public void ClearSetupData()
    {
        characterSetups.Clear();
        goalSetups.Clear();
    }

    [ContextMenu("Validate Current Setup")]
    public void ValidateSetup()
    {
        if (characterSetups.Count < 2)
        {
            Debug.LogWarning("ĳ���Ͱ� 2�� �̸��Դϴ�.");
        }

        if (goalSetups.Count == 0)
        {
            Debug.LogWarning("�������� �������� �ʾҽ��ϴ�.");
        }

        // ��ġ �ߺ� üũ
        HashSet<Vector3Int> positions = new HashSet<Vector3Int>();

        foreach (var character in characterSetups)
        {
            if (positions.Contains(character.startPosition))
            {
                Debug.LogWarning($"ĳ���� ��ġ �ߺ�: {character.startPosition}");
            }
            positions.Add(character.startPosition);
        }

        foreach (var goal in goalSetups)
        {
            if (positions.Contains(goal.position))
            {
                Debug.LogWarning($"�������� ĳ���Ϳ� ���� ��ġ�� �ֽ��ϴ�: {goal.position}");
            }
        }

        Debug.Log("���� ���� �Ϸ�!");
    }
#endif

    /// <summary>
    /// ��Ÿ�ӿ��� LevelData�� �ε��Ͽ� ������ ����
    /// </summary>
    public void LoadLevelData(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData�� null�Դϴ�!");
            return;
        }

        // Grid ���� ����
        if (grid != null)
        {
            grid.cellSize = new Vector3(levelData.cellSize, levelData.cellSize, 1f);
            transform.position = levelData.gridOrigin;
        }

        Debug.Log($"Level {levelData.levelNumber} �ε� �Ϸ�: {levelData.levelName}");
    }
}