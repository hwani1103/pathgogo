using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Unity Editor에서 레벨을 생성하고 관리하는 시스템
/// Tilemap을 읽어서 LevelData ScriptableObject로 변환합니다
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
        // 자동으로 Grid와 Tilemap 찾기
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
            Debug.LogError("Tilemap이 설정되지 않았습니다!");
            return;
        }

        if (grid == null)
        {
            Debug.LogError("Grid가 설정되지 않았습니다!");
            return;
        }

        // LevelData 생성
        LevelData levelData = CreateLevelDataFromScene();

        // ScriptableObject로 저장
        SaveLevelData(levelData);

        Debug.Log($"레벨 데이터 생성 완료: {levelData.name}");
        levelData.LogLevelInfo();
    }

    private LevelData CreateLevelDataFromScene()
    {
        LevelData levelData = ScriptableObject.CreateInstance<LevelData>();

        // 기본 정보 설정
        levelData.levelNumber = levelNumber;
        levelData.levelName = levelName;
        levelData.moveSpeed = moveSpeed;
        levelData.showGridInGame = showGridInGame;

        // 그리드 설정 - Grid 컴포넌트에서 직접 가져오기
        levelData.cellSize = grid != null ? grid.cellSize.x : 1f;
        levelData.gridOrigin = transform.position;

        // Tilemap에서 타일 정보 읽어오기
        ReadTilemapData(levelData);

        // 캐릭터 설정 추가
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

        // 목적지 설정 추가
        foreach (var setup in goalSetups)
        {
            levelData.AddGoal(setup.position, setup.goalType, setup.goalColor);
        }

        // 그리드 크기 계산
        CalculateGridBounds(levelData);

        return levelData;
    }

    private void ReadTilemapData(LevelData levelData)
    {
        Debug.Log("=== Reading Tilemap Data ===");

        // 기존 타일 데이터 클리어
        levelData.walkableTiles.Clear();
        levelData.tileTypes.Clear();

        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] tiles = tilemap.GetTilesBlock(bounds);

        // 타일 타입 매핑 생성
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

                    // 새로운 타일 타입이면 등록
                    if (!tileToTypeId.ContainsKey(tile))
                    {
                        tileToTypeId[tile] = nextTypeId;

                        // 타일 에셋 경로 가져오기
                        string assetPath = "";
#if UNITY_EDITOR
                        assetPath = UnityEditor.AssetDatabase.GetAssetPath(tile);
#endif

                        levelData.AddTileType(
                            nextTypeId,
                            tile.name,
                            assetPath,
                            true  // 기본적으로 모든 타일은 이동 가능
                        );

                        Debug.Log($"New tile type registered: ID={nextTypeId}, Name={tile.name}, Path={assetPath}");
                        nextTypeId++;
                    }

                    // 타일 데이터 저장
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

        // 타일 타입 정보 출력
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

        // 폴더가 없으면 생성
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // ScriptableObject 저장
        AssetDatabase.CreateAsset(levelData, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Inspector에서 선택
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
            Debug.LogWarning("캐릭터가 2개 미만입니다.");
        }

        if (goalSetups.Count == 0)
        {
            Debug.LogWarning("목적지가 설정되지 않았습니다.");
        }

        // 위치 중복 체크
        HashSet<Vector3Int> positions = new HashSet<Vector3Int>();

        foreach (var character in characterSetups)
        {
            if (positions.Contains(character.startPosition))
            {
                Debug.LogWarning($"캐릭터 위치 중복: {character.startPosition}");
            }
            positions.Add(character.startPosition);
        }

        foreach (var goal in goalSetups)
        {
            if (positions.Contains(goal.position))
            {
                Debug.LogWarning($"목적지가 캐릭터와 같은 위치에 있습니다: {goal.position}");
            }
        }

        Debug.Log("설정 검증 완료!");
    }
#endif

    /// <summary>
    /// 런타임에서 LevelData를 로드하여 레벨을 설정
    /// </summary>
    public void LoadLevelData(LevelData levelData)
    {
        if (levelData == null)
        {
            Debug.LogError("LevelData가 null입니다!");
            return;
        }

        // Grid 설정 적용
        if (grid != null)
        {
            grid.cellSize = new Vector3(levelData.cellSize, levelData.cellSize, 1f);
            transform.position = levelData.gridOrigin;
        }

        Debug.Log($"Level {levelData.levelNumber} 로드 완료: {levelData.levelName}");
    }
}