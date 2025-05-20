using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MapParser : MonoBehaviour
{
    [SerializeField] private Tile wallTile;
    [SerializeField] private Tile floorTile;
    [SerializeField] private Tilemap wallTilemap;
    [SerializeField] private Tilemap floorTilemap;

    public void ParseMap()
    {
        //open sample_map.txt
        string[] lines = System.IO.File.ReadAllLines("Assets/Scripts/sample_map.txt");
        //first line is the width and height
        string[] dimensions = lines[0].Split(' ');
        int width = int.Parse(dimensions[0]);
        int height = int.Parse(dimensions[1]);
        //create a new tilemap with the given width and height
        wallTilemap.ClearAllTiles();
        floorTilemap.ClearAllTiles();
        print("size" + lines[1].Length);
        
        for (int y = 0; y < height; y++)
        {
            var linesArray = lines[y + 1].Split(' ');
            for (int x = 0; x < width; x++)
            {
                //parse the tile type
                print(lines[y+1][x]);
                var tileType = linesArray[x][0];
                if (tileType == '1')
                {
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                }
                else
                {
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floorTile);
                }
            }
        }
    }
}


#if UNITY_EDITOR
[CustomEditor(typeof(MapParser))]
public class MapParserEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MapParser mapParser = (MapParser)target;

        if (GUILayout.Button("Parse Map"))
        {
            mapParser.ParseMap();
        }
    }
}
#endif