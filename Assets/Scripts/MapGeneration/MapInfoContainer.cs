using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    public class MapInfoContainer : MonoBehaviour
    {
        public List<Vector3> spawnPositions;
        public List<Vector3> gatePositions;
        public int heavenGateIndex = 1;
        
        // TODO: add function calls to GameManager
        public void AddSpawnPosition(Vector3 position)
        {
            spawnPositions.Add(position);
        }

        //TODO: add function calls to GameManager
        public void AddGatePosition(Vector3 position, bool isHeaven)
        {
            gatePositions.Add(position);
            if (isHeaven) 
            {
                heavenGateIndex = gatePositions.Count - 1;
            }
        }

    }
}