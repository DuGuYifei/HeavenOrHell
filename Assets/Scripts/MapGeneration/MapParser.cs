

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DefaultNamespace;
using Message;
using network;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
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
        
        [SerializeField] private RawImage minimapRawImage;
        
        private const int Width = 31;
        private const int Height = 31;
        private Texture2D _minimapTexture;
        private readonly Color _wallColor = new Color(163 / 255f, 110 / 255f, 52 / 255f);
        private readonly Color _floorColor = new Color(232 / 255f, 202 / 255f, 153 / 255f);
        private bool _testFlag = false;

        private void Start()
        {
            _minimapTexture = new Texture2D(Width, Height);
            if (KcpRecvMessageParser.Instance) KcpRecvMessageParser.Instance.onMapReceived.AddListener(ParseMessage);
        }

        // TODO: update only for test, so delete it
        private void Update()
        {
            if (!_testFlag)
            {
                _testFlag = true;
                StreamReader sr = new StreamReader("D:\\Project\\Game\\Unity\\HeavenOrHell\\heaven-or-heal-client\\Assets\\Scripts\\sample_map.txt");
                String msg = sr.ReadToEnd();
                sr.Close();
                ParseMap(msg);
            }
        }
        
        private void ParseMessage(StringMessage msg)
        {
            ParseMap(msg.MessageContent);
        }

        public void ParseMap(string msg)
        {
            
            //open sample_map.txt
            // var lines = 
            var lines = msg;
            var decodedLines = DecompressRle(lines, Height, Width).Split('\n');
           
            //create a new tilemap with the given width and height
            wallTilemap.ClearAllTiles();
            floorTilemap.ClearAllTiles();
            //tilemap is three times the size of the map
            var map = new char[3 * Width][];
            for (var index = 0; index < 3 * Width; index++) map[index] = new char[3 * Height];
            var spawnPoints = new List<Vector2>();
            var scale = Consts.MapScale;
            for (var y = 0; y < Height; y++)
            {
                var linesArray = decodedLines[y];
                print(linesArray.Length);
                for (var x = 0; x < Width; x++)
                {
                    if (linesArray[x] == SPAWN)
                    {
                        spawnPoints.Add(new Vector2(x, y));
                    } else if (linesArray[x] == EXIT)
                    {
                        // TODO: comment for test, so uncomment it
                        // GameManager.Instance?.mapInfoContainer.AddGatePosition(new Vector2(x, y) * scale, false);
                    }

                    for (var i = scale * x ; i < scale * (x + 1); i++)
                    {
                        for (var j = scale * y; j < scale * (y + 1); j++)
                        {
                            map[i][j] = linesArray[x];
                        }
                    }
                }
            }

            for (var x = 0; x < scale * Width; x++)
            {
                for (var y = 0; y < scale * Height; y++)
                {
                    if (map[x][y] == WALL)
                    {
                        wallTilemap.SetTile(new Vector3Int(x, y, 0), tileset.wallTile);
                        if (x % 3 == 0 && y % 3 == 0)
                            _minimapTexture.SetPixel(x / 3, y / 3, _wallColor);
                    }
                    else if (map[x][y] == FLOOR)
                    {
                        floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.floor);
                        if (x % 3 == 0 && y % 3 == 0)
                            _minimapTexture.SetPixel(x / 3, y / 3, _floorColor);
                    }
                    else if (map[x][y] == CENTER)
                    {
                        floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.centerTile);
                        if (x % 3 == 0 && y % 3 == 0)
                            _minimapTexture.SetPixel(x / 3, y / 3, _floorColor);
                    }
                    else if (map[x][y] == SPAWN)
                    {
                        floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.spawnTile);
                        if (x % 3 == 0 && y % 3 == 0)
                            _minimapTexture.SetPixel(x / 3, y / 3, _floorColor);
                    }
                    else
                    {
                        floorTilemap.SetTile(new Vector3Int(x, y, 0), tileset.floor);
                        if (x % 3 == 0 && y % 3 == 0)
                            _minimapTexture.SetPixel(x / 3, y / 3, _floorColor);
                    }
                }
            }

            _minimapTexture.Apply();
            _minimapTexture.filterMode = FilterMode.Point;
            minimapRawImage.texture = _minimapTexture;
            
                // else
                //     print(map[x][y]);
            // TODO: comment for test, so uncomment it
            // GameManager.Instance?.SetSpawnPositions(spawnPoints, scale);
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

        if (GUILayout.Button("Parse Map")) mapParser.ParseMap(File.ReadAllText("Assets/Scripts/sample_map.txt"));
    }
}
#endif
}