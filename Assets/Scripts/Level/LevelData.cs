using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// ĳ���� �����͸� �����ϴ� ����ü
/// </summary>
[System.Serializable]
public struct CharacterData
{
    [Header("Character Info")]
    public string characterId;           // ĳ���� �ĺ��� (��: "A", "B", "C", "D")
    public Vector3Int startPosition;     // ���� ��ġ (�׸��� ��ǥ)
    public int maxSelections;            // �ִ� ��� ���� Ƚ��

    [Header("Visual")]
    public Color characterColor;         // ĳ���� ����

    [Header("Goal Assignment")]
    public int assignedGoalIndex;        // �Ҵ�� ������ �ε��� (-1�̸� ���� ������)

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
/// ������ �����͸� �����ϴ� ����ü
/// </summary>
[System.Serializable]
public struct GoalData
{
    [Header("Goal Info")]
    public Vector3Int position;          // ������ ��ġ (�׸��� ��ǥ)
    public GoalType goalType;           // ������ Ÿ��
    public List<string> assignedCharacters; // �� �������� �Ҵ�� ĳ���͵�

    [Header("Visual")]
    public Color goalColor;             // ������ ����

    public GoalData(Vector3Int pos, GoalType type, Color color)
    {
        position = pos;
        goalType = type;
        goalColor = color;
        assignedCharacters = new List<string>();
    }
}

// GoalType�� ���� ���Ͽ� ���ǵǾ� �����Ƿ� ���⼭�� ����

/// <summary>
/// Ÿ�� �����͸� �����ϴ� ����ü (�ʿ� �� Ȯ�� ����)
/// </summary>
/// <summary>
/// Ÿ�� �����͸� �����ϴ� ����ü
/// </summary>
[System.Serializable]
public struct TileData
{
    public Vector3Int position;         // Ÿ�� ��ġ
    public bool isWalkable;            // �̵� ���� ����
    public string tileName;            // Ÿ�� �̸�
    public int tileTypeId;             // Ÿ�� Ÿ�� ID

    public TileData(Vector3Int pos, bool walkable = true, string name = "", int typeId = 0)
    {
        position = pos;
        isWalkable = walkable;
        tileName = name;
        tileTypeId = typeId;
    }
}

/// <summary>
/// Ÿ�� Ÿ�� ������ �����ϴ� ����ü
/// </summary>
[System.Serializable]
public struct TileTypeData
{
    public int typeId;
    public string typeName;
    public string tileAssetPath;       // Ÿ�� ���� ���
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
/// ���� �����͸� �����ϴ� ScriptableObject
/// </summary>
[CreateAssetMenu(fileName = "LevelData", menuName = "PuzzleGame/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Level Info")]
    public int levelNumber;
    public string levelName;

    [Header("Tile Mapping")]
    public List<TileTypeData> tileTypes = new List<TileTypeData>();  // ���� Ÿ�� Ÿ�Ե�

    [Header("Tiles")]
    public List<TileData> walkableTiles = new List<TileData>();     // ���� ��ġ�� Ÿ�ϵ�

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
    public float moveSpeed = 2f;        // ĳ���� �̵� �ӵ�
    public bool showGridInGame = false; // ���� �� �׸��� ǥ�� ����

    /// <summary>
    /// Ư�� ��ġ�� Ÿ���� �ִ��� Ȯ��
    /// </summary>
    public bool HasTileAt(Vector3Int position)
    {
        return walkableTiles.Exists(tile => tile.position == position && tile.isWalkable);
    }

    /// <summary>
    /// Ư�� ��ġ�� ĳ���Ͱ� �ִ��� Ȯ��
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
    /// Ư�� ��ġ�� �������� �ִ��� Ȯ��
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
    /// ĳ���� �߰�
    /// </summary>
    public void AddCharacter(string id, Vector3Int startPos, int maxSelections, Color color, int goalIndex = -1)
    {
        characters.Add(new CharacterData(id, startPos, maxSelections, color, goalIndex));
    }

    /// <summary>
    /// ������ �߰�
    /// </summary>
    public void AddGoal(Vector3Int position, GoalType goalType, Color color)
    {
        goals.Add(new GoalData(position, goalType, color));
    }

    /// <summary>
    /// Ÿ�� �߰�
    /// </summary>
    public void AddTile(Vector3Int position, bool isWalkable = true)
    {
        // �ߺ� üũ
        if (!walkableTiles.Exists(tile => tile.position == position))
        {
            walkableTiles.Add(new TileData(position, isWalkable));
        }
    }

    /// <summary>
    /// ���� ������ ��ȿ�� �˻�
    /// </summary>
    public bool ValidateLevelData(out string errorMessage)
    {
        errorMessage = "";

        // ĳ���� �� üũ
        if (characters.Count < 2 || characters.Count > 4)
        {
            errorMessage = "ĳ���ʹ� 2-4�� ���̿��� �մϴ�.";
            return false;
        }

        // ������ �� üũ
        if (goals.Count == 0)
        {
            errorMessage = "�ּ� 1���� �������� �ʿ��մϴ�.";
            return false;
        }

        // ĳ���� ���� ��ġ�� Ÿ���� �ִ��� Ȯ��
        foreach (var character in characters)
        {
            if (!HasTileAt(character.startPosition))
            {
                errorMessage = $"ĳ���� {character.characterId}�� ���� ��ġ({character.startPosition})�� Ÿ���� �����ϴ�.";
                return false;
            }
        }

        // ������ ��ġ�� Ÿ���� �ִ��� Ȯ��
        foreach (var goal in goals)
        {
            if (!HasTileAt(goal.position))
            {
                errorMessage = $"������ ��ġ({goal.position})�� Ÿ���� �����ϴ�.";
                return false;
            }
        }

        // ĳ���Ϳ� ������ ��ġ �ߺ� üũ
        HashSet<Vector3Int> usedPositions = new HashSet<Vector3Int>();

        foreach (var character in characters)
        {
            if (usedPositions.Contains(character.startPosition))
            {
                errorMessage = $"ĳ���� ���� ��ġ�� �ߺ��˴ϴ�: {character.startPosition}";
                return false;
            }
            usedPositions.Add(character.startPosition);
        }

        return true;
    }

    /// <summary>
    /// ���� ������ Ŭ����
    /// </summary>
    public void ClearLevelData()
    {
        characters.Clear();
        goals.Clear();
        walkableTiles.Clear();
    }

    /// <summary>
    /// ����� ���� ���
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
     /// Ÿ�� Ÿ�� ID�� ���� TileBase ���� �ε�
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
    /// Ÿ�� Ÿ�� �߰�
    /// </summary>
    public void AddTileType(int typeId, string typeName, string assetPath, bool isWalkable = true)
    {
        // �ߺ� üũ
        if (!tileTypes.Exists(t => t.typeId == typeId))
        {
            tileTypes.Add(new TileTypeData(typeId, typeName, assetPath, isWalkable));
        }
    }

    /// <summary>
    /// Ÿ�� �߰� (�����ε�)
    /// </summary>
    public void AddTile(Vector3Int position, bool isWalkable, string tileName, int tileTypeId)
    {
        // �ߺ� üũ
        if (!walkableTiles.Exists(tile => tile.position == position))
        {
            walkableTiles.Add(new TileData(position, isWalkable, tileName, tileTypeId));
        }
    }

    /// <summary>
    /// Ư�� Ÿ��ID�� Ÿ�� Ÿ�� ���� ��ȯ
    /// </summary>
    public TileTypeData? GetTileType(int typeId)
    {
        var tileType = tileTypes.Find(t => t.typeId == typeId);
        return tileType.typeId == typeId ? tileType : (TileTypeData?)null;
    }

}