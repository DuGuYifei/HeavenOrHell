using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;
using UnityEngine.Serialization;
using utils;

namespace Character
{
    public class DogPathManager : MonoBehaviour
    {
        [SerializeField] private int spawnDistance = 2;
        [FormerlySerializedAs("dogPoolPath")] [FormerlySerializedAs("objectPooler")] [SerializeField] private DogPathPool dogPathPool;

        private readonly Dictionary<int, HashSet<Vector2Int>> _characterPaths = new ();

        private bool _checkPaths = true;
        private Transform _playerTransform;
        
        private readonly Dictionary<int, HashSet<Vector2Int>> _currentPath = new();
        private readonly Dictionary<int, List<DogPathContainer>> _activePaths = new();

        private Camera _mainCamera;
        private Vector3 _gridSize;

        public void TurnOnPathChecking(Transform playerTransform)
        {
            _mainCamera = GameManager.Instance.mainCamera;
            _playerTransform = playerTransform;
            _checkPaths = true;
            _gridSize = GameManager.Instance.GridSize;
        }

        private void Update()
        {
            if (!_checkPaths) return;
            // Add character paths
            var characters = GameManager.Instance.Characters;
            foreach (var character in characters)
            {
                var transformPosition = character.transform.position;
                if (!_characterPaths.ContainsKey(character.id))
                {
                    _characterPaths[character.id] = new HashSet<Vector2Int>();
                }
                var chunkCoord = new Vector2Int(Mathf.FloorToInt(transformPosition.x / _gridSize.x), Mathf.FloorToInt(transformPosition.y / _gridSize.y));
                if (!_characterPaths[character.id].Contains(chunkCoord))
                {
                    _characterPaths[character.id].Add(chunkCoord);
                }
            }
            
            // update visible paths for the player
            if (!_playerTransform) return;
            Vector3 camPos = _mainCamera.transform.position;
            float halfHeight = _mainCamera.orthographicSize;
            float halfWidth = halfHeight * _mainCamera.aspect;

            Vector2 min = new Vector2(camPos.x - halfWidth - spawnDistance, camPos.y - halfHeight - spawnDistance);
            Vector2 max = new Vector2(camPos.x + halfWidth + spawnDistance, camPos.y + halfHeight + spawnDistance);

            Vector2Int minChunk = new Vector2Int(Mathf.FloorToInt(min.x / _gridSize.x), Mathf.FloorToInt(min.y / _gridSize.y));
            Vector2Int maxChunk = new Vector2Int(Mathf.FloorToInt(max.x / _gridSize.x), Mathf.FloorToInt(max.y / _gridSize.y));

            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (var i = 0; i < Consts.PlayerCount; i++)
                    {
                        if (!_currentPath.ContainsKey(i))
                        {
                            _currentPath[i] = new HashSet<Vector2Int>();
                        }
                        Vector2Int chunkCoord = new Vector2Int(x, y);
                        if (_characterPaths[i].Contains(chunkCoord) && !_currentPath[i].Contains(chunkCoord))
                        {
                            SpawnObjectAtChunk(chunkCoord, i);
                            _currentPath[i].Add(chunkCoord);
                        }
                    }
                }
            }
            // Check new visible positions for paths
            CullFarObjects(camPos, halfWidth + spawnDistance, halfHeight + spawnDistance);
        }
        
        void SpawnObjectAtChunk(Vector2Int chunkCoord, int id)
        {
            Vector3 worldPos = new Vector3(chunkCoord.x * _gridSize.x, chunkCoord.y * _gridSize.y, 0);
            var obj = dogPathPool.GetFromPool(id);
            obj.transform.position = worldPos;
            if (!_activePaths.ContainsKey(id))
            {
                _activePaths[id] = new List<DogPathContainer>();
            }
            _activePaths[id].Add(obj);
        }

        void CullFarObjects(Vector3 center, float maxWidth, float maxHeight)
        {
            for(var i = 0; i < Consts.PlayerCount; i++)
            {
                for (int j = _activePaths.Count - 1; j >= 0; j--)
                {
                    var obj = _activePaths[i][j];
                    Vector3 pos = obj.transform.position;

                    if (Mathf.Abs(pos.x - center.x) > maxWidth || Mathf.Abs(pos.y - center.y) > maxHeight)
                    {
                        dogPathPool.ReturnToPool(obj);
                        _activePaths[i].RemoveAt(j);
                        Vector2Int chunk = new Vector2Int(Mathf.FloorToInt(pos.x / _gridSize.x), Mathf.FloorToInt(pos.y / _gridSize.y));
                        _currentPath[i].Remove(chunk);
                    }
                }
            }
            
        }
    }
}