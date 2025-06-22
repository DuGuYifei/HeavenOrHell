using System;
using System.Collections.Generic;
using Message;
using UnityEngine;
using UnityEngine.Serialization;

namespace MapGeneration
{
    [Serializable]
    public class MapInfoContainer
    {
        [FormerlySerializedAs("spawnPositions")] public List<Vector3> soulSpawnPositions;
        public Vector3 reaperPosition;
        public List<Vector3> gatePositions;
        public List<GateDirection> gateDirections;
        public Vector3 altarPosition;
        public int heavenGateIndex = 1;
        public string mapString;
        
        // TODO: add function calls to GameManager
        public void AddSpawnPosition(Vector3 position)
        {
            soulSpawnPositions.Add(position);
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