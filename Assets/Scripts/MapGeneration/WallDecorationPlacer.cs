using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapGeneration
{
    public class WallDecorationPlacer : MonoBehaviour
    {
        [SerializeField] private Tilemap decorationTilemap;
        [SerializeField] private Tilemap wallTilemap;
        [SerializeField] private RuleTile targetRuleTile;
        
        [SerializeField] private float decorationChance = 0.1f;
        
        [SerializeField] private List<Tile> decorationTiles;
        
        private readonly HashSet<Vector3Int> _decorationTilesSet = new ();
        
        public void PlaceDecorations()
        {
            if (decorationTilemap == null || wallTilemap == null) return;

            BoundsInt bounds = wallTilemap.cellBounds;

            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                for (int y = bounds.yMin; y < bounds.yMax; y++)
                {
                    Vector3Int pos = new Vector3Int(x, y, 0);
                    TileBase tile = wallTilemap.GetTile(pos);

                    if (tile == targetRuleTile)
                    {
                        // Use neighbor logic to determine the actual rule
                        if (IsLikelyVerticalWall(pos))
                        {
                            PlaceTileWithChance(new Vector3Int(x,y, 0));
                        }
                    }
                }
            }
        }
        
        private void PlaceTileWithChance(Vector3Int pos)
        {
            if (NeighborsNotPlaced(pos) && Random.value < decorationChance)
            {
                TileBase tileToPlace = decorationTiles[Random.Range(0, decorationTiles.Count)];
                decorationTilemap.SetTile(pos, tileToPlace);
                _decorationTilesSet.Add(pos);
            }
        }

        private bool NeighborsNotPlaced(Vector3Int pos)
        {
            for (int x = pos.x - 1; x <= pos.x + 1; x++)
            {
                for (int y = pos.y - 1; y <= pos.y + 1; y++)
                {
                    Vector3Int neighborPos = new Vector3Int(x, y, pos.z);
                    if (_decorationTilesSet.Contains(neighborPos))
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        
        bool IsLikelyVerticalWall(Vector3Int pos)
        {
            return
                wallTilemap.GetTile(pos + Vector3Int.down) == targetRuleTile &&
                wallTilemap.GetTile(pos + Vector3Int.left) == targetRuleTile &&
                wallTilemap.GetTile(pos + Vector3Int.right) == targetRuleTile &&
                wallTilemap.GetTile(pos + 2 * Vector3Int.down) != targetRuleTile;
        }
        
    }
}