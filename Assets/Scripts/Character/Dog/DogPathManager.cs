using System.Collections.Generic;
using Character.Detective;
using Character.Psychologist;
using MapGeneration;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Character
{
    public class DogPathManager : MonoBehaviour
    {
        [SerializeField] private int spawnDistance = 2;
        [SerializeField] private Tilemap dogPathTilemap;
        [SerializeField] private Tileset tileset;
        private readonly Dictionary<int, HashSet<Vector3Int>> _characterPaths = new ();

        private bool _checkPaths = true;
        
        // private readonly Dictionary<int, HashSet<Vector2Int>> _currentPath = new();
        // private readonly Dictionary<int, List<DogPathContainer>> _activePaths = new();

        private Camera _mainCamera;
        private Vector3 _gridSize;
        private List<Tile> _dogTiles = new(); 

        public void TurnOnPathChecking()
        {
            _mainCamera = GameManager.Instance.mainCamera;
            _checkPaths = true;
            _gridSize = GameManager.Instance.GridSize;
            GameManager.Instance.OnGameInitializeFinished.AddListener(Initialize);
        }

        private void Initialize()
        {
            foreach (var character in GameManager.Instance.Characters)
            {
                switch (character)
                {
                    case DogContainer dog:
                        _dogTiles.Add(tileset.dogTrail);
                        break;
                    case PsychologistContainer psy:
                        _dogTiles.Add(tileset.psychologistTrail);
                        break;
                    case DetectiveContainer detective:
                        _dogTiles.Add(tileset.detectiveTrail);
                        break;
                    case ReaperContainer reaper:
                        _dogTiles.Add(tileset.reaperTrail);
                        break;
                }
            }
        }

        private void Update()
        {
            if (!_checkPaths) return;
            // Add character paths
            var characters = GameManager.Instance.Characters;
            var i = 0;
            foreach (var character in characters)
            {
                var transformPosition = character.transform.position;
                if (!_characterPaths.ContainsKey(character.id))
                {
                    _characterPaths[character.id] = new HashSet<Vector3Int>();
                }
                var chunkCoord = new Vector3Int(Mathf.FloorToInt(transformPosition.x / _gridSize.x), Mathf.FloorToInt(transformPosition.y / _gridSize.y), 0);
                if (!_characterPaths[character.id].Contains(chunkCoord))
                {
                    _characterPaths[character.id].Add(chunkCoord);
                    dogPathTilemap.SetTile(chunkCoord, _dogTiles[i % _dogTiles.Count]);
                }
                i++;
            }
            
            // update visible paths for the player


            // Check new visible positions for paths
            // CullFarObjects(camPos, halfWidth + spawnDistance, halfHeight + spawnDistance);
        }
        
        // void SpawnObjectAtChunk(Vector2Int chunkCoord, int id)
        // {
        //     Vector3 worldPos = new Vector3(chunkCoord.x * _gridSize.x, chunkCoord.y * _gridSize.y, 0);
        //     var obj = dogPathPool.GetFromPool(id);
        //     obj.transform.position = worldPos;
        //     if (!_activePaths.ContainsKey(id))
        //     {
        //         _activePaths[id] = new List<DogPathContainer>();
        //     }
        //     _activePaths[id].Add(obj);
        // }
        
    }
}