using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGeneration
{
    [CreateAssetMenu(fileName = "Tileset", menuName = "TileGeneration/Tileset", order = 0)]
    public class Tileset : ScriptableObject
    {
        public RuleTile wallTile;
        public Tile floor;
        public Tile centerTile;
        public Tile spawnTile;
        
    }
}