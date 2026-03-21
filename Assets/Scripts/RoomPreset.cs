using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Dungeon/Room Preset")]
public class RoomPreset : ScriptableObject
{
    public int width;
    public int height;

    public bool[] flatGrid; // 1D array for Unity serialization

    public void Save(bool[,] grid)
    {
        width = grid.GetLength(0);
        height = grid.GetLength(1);

        flatGrid = new bool[width * height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            flatGrid[y * width + x] = grid[x, y];
        }

        Debug.Log($"Saved preset: {name} | Size: {width}x{height}");

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
#endif
    }

    public bool[,] Load()
    {
        if (flatGrid == null || flatGrid.Length == 0)
        {
            Debug.LogError($"Preset {name} is empty!");
            return new bool[width, height];
        }

        bool[,] grid = new bool[width, height];

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            grid[x, y] = flatGrid[y * width + x];
        }

        return grid;
    }
}