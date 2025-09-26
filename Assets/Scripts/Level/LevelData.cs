using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 캐릭터 데이터를 저장하는 구조체
/// </summary>
[System.Serializable]
public struct CharacterData
{
    [Header("Character Info")]
    public string characterId;           // 캐릭터 식별자 (예: "A", "B", "C", "D")
    public Vector3Int startPosition;     // 시작 위치 (그리드 좌표)
    public int maxSelections;            // 최대 경로 선택 횟수

    [Header("Visual")]
    public Color characterColor;         // 캐릭터 색상

    [Header("Goal Assignment")]
    public int assignedGoalIndex;        // 할당된 목적지 인덱스 (-1이면 공유 목적지)

    public CharacterData(string id, Vector3Int startPos, int maxSel, Color color, int goalIndex = -1)
    {
        characterId = id;
        startPosition = startPos;
        maxSelections = maxSel;
        characterColor = color;
        assignedGoalIndex = goalIndex;
    }
}

/// <summary>
/// 목적지 데이터를 저장하는 구조체
/// </summary>
[System.Serializable]
public struct GoalData
{
    [Header("Goal Info")]
    public Vector3Int position;          // 목적지 위치 (그리드 좌표)
    public GoalType goalType;           // 목적지 타입
    public List<string> assignedCharacters; // 이 목적지에 할당된 캐릭터들

    [Header("Visual")]
    public Color goalColor;             // 목적지 색상

    public GoalData(Vector3Int pos, GoalType type, Color color)
    {
        position = pos;
        goalType = type;
        goalColor = color;
        assignedCharacters = new List<string>();
    }
}

// GoalType은 별도 파일에 정의되어 있으므로 여기서는 제거

/// <summary>
/// 타일 데이터를 저장하는 구조체 (필요 시 확장 가능)
/// </summary>
/// <summary>
/// 타일 데이터를 저장하는 구조체
/// </summary>
[System.Serializable]
public struct TileData
{
    public Vector3Int position;         // 타일 위치
    public bool isWalkable;            // 이동 가능 여부
    public string tileName;            // 타일 이름
    public int tileTypeId;             // 타일 타입 ID

    public TileData(Vector3Int pos, bool walkable = true, string name = "", int typeId = 0)
    {
        position = pos;
        isWalkable = walkable;
        tileName = name;
        tileTypeId = typeId;
    }
}

/// <summary>
/// 타일 타입 정보를 저장하는 구조체
/// </summary>
[System.Serializable]
public struct TileTypeData
{
    public int typeId;
    public string typeName;
    public string tileAssetPath;       // 타일 에셋 경로
    public bool isWalkable;

    public TileTypeData(int id, string name, string assetPath, bool walkable = true)
    {
        typeId = id;
        typeName = name;
        tileAssetPath = assetPath;
        isWalkable = walkable;
    }
}

/// <summary>
/// 레벨 데이터를 저장하는 ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "PuzzleGame/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;
    public string levelName;

    [Header("Tile Mapping")]
    public List<TileTypeData> tileTypes = new List<TileTypeData>();  // 사용된 타일 타입들

    [Header("Tiles")]
    public List<TileData> walkableTiles = new List<TileData>();     // 실제 배치된 타일들

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridHeight = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;

    [Header("Characters")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Header("Goals")]
    public List<GoalData> goals = new List<GoalData>();
    
    [Header("Level Settings")]
    public float moveSpeed = 2f;        // 캐릭터 이동 속도
    public bool showGridInGame = false; // 게임 중 그리드 표시 여부

    /// <summary>
    /// 특정 위치에 타일이 있는지 확인
    /// </summary>
    public bool HasTileAt(Vector3Int position)
    {
        return walkableTiles.Exists(tile => tile.position == position && tile.isWalkable);
    }

    /// <summary>
    /// 특정 위치에 캐릭터가 있는지 확인
    /// </summary>
    /// 



    public CharacterData? GetCharacterAt(Vector3Int position)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i].startPosition == position)
                return characters[i];
        }
        return null;
    }

    /// <summary>
    /// 특정 위치에 목적지가 있는지 확인
    /// </summary>
    public GoalData? GetGoalAt(Vector3Int position)
    {
        for (int i = 0; i < goals.Count; i++)
        {
            if (goals[i].position == position)
                return goals[i];
        }
        return null;
    }

    /// <summary>
    /// 캐릭터 추가
    /// </summary>
    public void AddCharacter(string id, Vector3Int startPos, int maxSelections, Color color, int goalIndex = -1)
    {
        characters.Add(new CharacterData(id, startPos, maxSelections, color, goalIndex));
    }

    /// <summary>
    /// 목적지 추가
    /// </summary>
    public void AddGoal(Vector3Int position, GoalType goalType, Color color)
    {
        goals.Add(new GoalData(position, goalType, color));
    }

    /// <summary>
    /// 타일 추가
    /// </summary>
    public void AddTile(Vector3Int position, bool isWalkable = true)
    {
        // 중복 체크
        if (!walkableTiles.Exists(tile => tile.position == position))
        {
            walkableTiles.Add(new TileData(position, isWalkable));
        }
    }

    /// <summary>
    /// 레벨 데이터 유효성 검사
    /// </summary>
    public bool ValidateLevelData(out string errorMessage)
    {
        errorMessage = "";

        // 캐릭터 수 체크
        if (characters.Count < 2 || characters.Count > 4)
        {
            errorMessage = "캐릭터는 2-4개 사이여야 합니다.";
            return false;
        }

        // 목적지 수 체크
        if (goals.Count == 0)
        {
            errorMessage = "최소 1개의 목적지가 필요합니다.";
            return false;
        }

        // 캐릭터 시작 위치에 타일이 있는지 확인
        foreach (var character in characters)
        {
            if (!HasTileAt(character.startPosition))
            {
                errorMessage = $"캐릭터 {character.characterId}의 시작 위치({character.startPosition})에 타일이 없습니다.";
                return false;
            }
        }

        // 목적지 위치에 타일이 있는지 확인
        foreach (var goal in goals)
        {
            if (!HasTileAt(goal.position))
            {
                errorMessage = $"목적지 위치({goal.position})에 타일이 없습니다.";
                return false;
            }
        }

        // 캐릭터와 목적지 위치 중복 체크
        HashSet<Vector3Int> usedPositions = new HashSet<Vector3Int>();

        foreach (var character in characters)
        {
            if (usedPositions.Contains(character.startPosition))
            {
                errorMessage = $"캐릭터 시작 위치가 중복됩니다: {character.startPosition}";
                return false;
            }
            usedPositions.Add(character.startPosition);
        }

        return true;
    }

    /// <summary>
    /// 레벨 데이터 클리어
    /// </summary>
    public void ClearLevelData()
    {
        characters.Clear();
        goals.Clear();
        walkableTiles.Clear();
    }

    /// <summary>
    /// 디버그 정보 출력
    /// </summary>
    public void LogLevelInfo()
    {
        Debug.Log($"=== Level {levelNumber}: {levelName} ===");
        Debug.Log($"Grid: {gridWidth}x{gridHeight}, Cell Size: {cellSize}");
        Debug.Log($"Characters: {characters.Count}, Goals: {goals.Count}, Tiles: {walkableTiles.Count}");

        foreach (var character in characters)
        {
            Debug.Log($"Character {character.characterId}: Start({character.startPosition}), MaxSel({character.maxSelections}), Goal({character.assignedGoalIndex})");
        }

        for (int i = 0; i < goals.Count; i++)
        {
            Debug.Log($"Goal {i}: Pos({goals[i].position}), Type({goals[i].goalType})");
        }
    }
    
    /// <summary>
     /// 타일 타입 ID로 실제 TileBase 에셋 로드
     /// </summary>
    public TileBase GetTileAsset(int tileTypeId)
    {
#if UNITY_EDITOR
        var tileType = tileTypes.Find(t => t.typeId == tileTypeId);
        if (!string.IsNullOrEmpty(tileType.tileAssetPath))
        {
            return UnityEditor.AssetDatabase.LoadAssetAtPath<TileBase>(tileType.tileAssetPath);
        }
#endif
        return null;
    }

    /// <summary>
    /// 타일 타입 추가
    /// </summary>
    public void AddTileType(int typeId, string typeName, string assetPath, bool isWalkable = true)
    {
        // 중복 체크
        if (!tileTypes.Exists(t => t.typeId == typeId))
        {
            tileTypes.Add(new TileTypeData(typeId, typeName, assetPath, isWalkable));
        }
    }

    /// <summary>
    /// 타일 추가 (오버로드)
    /// </summary>
    public void AddTile(Vector3Int position, bool isWalkable, string tileName, int tileTypeId)
    {
        // 중복 체크
        if (!walkableTiles.Exists(tile => tile.position == position))
        {
            walkableTiles.Add(new TileData(position, isWalkable, tileName, tileTypeId));
        }
    }

    /// <summary>
    /// 특정 타입ID의 타일 타입 정보 반환
    /// </summary>
    public TileTypeData? GetTileType(int typeId)
    {
        var tileType = tileTypes.Find(t => t.typeId == typeId);
        return tileType.typeId == typeId ? tileType : (TileTypeData?)null;
    }

}