using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGeneration
{

    public class MapParser : MonoBehaviour
    {
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private RuleTile wallTile;
        [SerializeField] private Tile floor;

        public void ParseMap()
        {
            //open sample_map.txt
            var lines = File.ReadAllLines("Assets/Scripts/sample_map.txt");
            //first line is the width and height
            var dimensions = lines[0].Split(' ');
            var width = int.Parse(dimensions[0]);
            var height = int.Parse(dimensions[1]);
            //create a new tilemap with the given width and height
            wallTilemap.ClearAllTiles();
            floorTilemap.ClearAllTiles();
            //tilemap is three times the size of the map
            var map = new int[3 * width][];
            for (var index = 0; index < 3 * width; index++) map[index] = new int[3 * height];

            for (var y = 0; y < height; y++)
            {
                var linesArray = lines[y + 1].Split(' ');
                for (var x = 0; x < width; x++)
                {
                    // if (x == 0 || y == 0 || x == width - 1 || y == height - 1)
                    // {
                    //     map[3 *x][3 * y] = 1;
                    //     map[3 *x][3 * y + 1] = 1;
                    //     map[3 *x][3 * y + 2] = 1;
                    //     map[3 *x + 1][3 * y] = 1;
                    //     map[3 *x + 1][3 * y + 1] = 1;
                    //     map[3 *x + 1][3 * y + 2] = 1;
                    //     map[3 *x + 2][3 * y] = 1;
                    //     map[3 *x + 2][3 * y + 1] = 1;
                    //     map[3 *x + 2][3 * y + 2] = 1;
                    // }
                    // else
                    // {
                    map[3 * x][3 * y] = int.Parse(linesArray[x]);
                    map[3 * x][3 * y + 1] = int.Parse(linesArray[x]);
                    map[3 * x][3 * y + 2] = int.Parse(linesArray[x]);
                    map[3 * x + 1][3 * y] = int.Parse(linesArray[x]);
                    map[3 * x + 1][3 * y + 1] = int.Parse(linesArray[x]);
                    map[3 * x + 1][3 * y + 2] = int.Parse(linesArray[x]);
                    map[3 * x + 2][3 * y] = int.Parse(linesArray[x]);
                    map[3 * x + 2][3 * y + 1] = int.Parse(linesArray[x]);
                    map[3 * x + 2][3 * y + 2] = int.Parse(linesArray[x]);
                    // }
                }
            }

            for (var x = 0; x < 3 * width; x++)
            for (var y = 0; y < 3 * height; y++)
                if (map[x][y] == 0)
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), floor);
                else if (map[x][y] == 1)
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), wallTile);
                else
                    print(map[x][y]);
        }
    }


#if UNITY_EDITOR
    [CustomEditor(typeof(MapParser))]
    public class MapParserEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mapParser = (MapParser)target;

            if (GUILayout.Button("Parse Map")) mapParser.ParseMap();
        }
    }
#endif
}