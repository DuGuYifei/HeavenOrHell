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
        public List<GateDirection> heavenGateDirections = new ();
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
            var minX = 1.7f;
            var maxX = 91f;
            var minY = 1.7f;
            var maxY = 91f;
            if (position.x < minX)
            {
                gateDirections.Add(GateDirection.Left);
            } else if (position.x > maxX)
            {
                gateDirections.Add(GateDirection.Right);
            } 
            else if (position.y < minY)
            {
                gateDirections.Add(GateDirection.Down);
            } 
            else if (position.y > maxY)
            {
                gateDirections.Add(GateDirection.Up);
            } 
        }
    }
}