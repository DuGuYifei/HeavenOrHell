using UnityEngine;

namespace MapGeneration
{
    [CreateAssetMenu(fileName = "PropSet", menuName = "TileGeneration/PropSet", order = 0)]
    public class PropSet : ScriptableObject
    {
        public GameObject altarPrefab;
        public GameObject gateLeftPrefab;
        public GameObject gateRightPrefab;
        public GameObject gateUpPrefab;
        public GameObject gateDownPrefab;
    }
}