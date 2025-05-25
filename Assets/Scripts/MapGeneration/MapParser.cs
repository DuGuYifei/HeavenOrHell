

using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MapGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MapGeneration
{
    
    public class MapParser : MonoBehaviour
    {
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private Tilemap floorTilemap;
        [SerializeField] private Tileset tileset;
        public static char WALL = '#';
        public static char FLOOR = '.';
        public static char EXIT = 'E';
        public static char CENTER = 'C';
        public static char SPAWN = 'S';
        public static char TREASURE = '$';
        public static char REAPER = 'R';

        public void ParseMap()
        {
            //open sample_map.txt
            var lines = File.ReadAllText("Assets/Scripts/sample_map.txt");
            var width = 31;
            var height = 31;
            var decodedLines = DecompressRle(lines, height, width).Split('\n');
            print(decodedLines);
           
            //create a new tilemap with the given width and height
            wallTilemap.ClearAllTiles();
            floorTilemap.ClearAllTiles();
            //tilemap is three times the size of the map
            var map = new char[3 * width][];
            for (var index = 0; index < 3 * width; index++) map[index] = new char[3 * height];

            for (var y = 0; y < height; y++)
            {
                var linesArray = decodedLines[y];
                print(linesArray.Length);
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
                    map[3 * x][3 * y] = linesArray[x];
                    map[3 * x][3 * y + 1] = linesArray[x];
                    map[3 * x][3 * y + 2] = linesArray[x];
                    map[3 * x + 1][3 * y] = linesArray[x];
                    map[3 * x + 1][3 * y + 1] = linesArray[x];
                    map[3 * x + 1][3 * y + 2] = linesArray[x];
                    map[3 * x + 2][3 * y] = linesArray[x];
                    map[3 * x + 2][3 * y + 1] = linesArray[x];
                    map[3 * x + 2][3 * y + 2] = linesArray[x];
                    // }
                }
            }
            
            for (var x = 0; x < 3 * width; x++)
            for (var y = 0; y < 3 * height; y++)
                if (map[x][y] == WALL)
                    wallTilemap.SetTile(new Vector3Int(x, y, 0), tileset.wallTile);
                else if (map[x][y] == FLOOR)
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.floor);
                else if (map[x][y] == CENTER)
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.centerTile);
                else if (map[x][y] == SPAWN)
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.spawnTile);
                else 
                    floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.floor);
            
                // else
                //     print(map[x][y]);
        }


        private static string DecompressRle(string input, int height = 31, int width = 31)
        {
            var output = new StringBuilder();
            var pattern = @"(\d+)(\#|\.|E|C|S|\$|R)|(\n)|(\#|\.|E|C|S|\$|R)(\n)";
            

            foreach (Match match in Regex.Matches(input, pattern))
                if (match.Groups[1].Success && match.Groups[2].Success)
                {
                    // Repeated character
                    var count = int.Parse(match.Groups[1].Value);
                    var ch = match.Groups[2].Value[0];
                    output.Append(new string(ch, count));
                }
                else if (match.Groups[3].Success)
                {
                    // Newline
                    output.Append('\n');
                } else if (match.Groups[4].Success && match.Groups[5].Success)
                {
                    // Single character followed by newline
                    output.Append(match.Groups[4].Value[0]);
                    output.Append('\n');
                }

            return output.ToString();
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