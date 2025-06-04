using UnityEngine;


namespace MiniGames.Runner
{
    public class RunnerSetup : MonoBehaviour
    {
        public int ObstaclesAmount = 5;
        GameObject[] CreatedObstacles = new GameObject[5];

        public GameObject[] ObstacleBlocks;
        public GameObject StartBlock;
        public GameObject EscapeBlock;

        void Start()
        {
            StartBlock.transform.position = Vector3.zero;
            if (ObstacleBlocks.Length > 0)
            {
                for (int i = 1; i <= ObstaclesAmount; i++)
                {
                    CreatedObstacles[i - 1] = Instantiate<GameObject>(ObstacleBlocks[Random.Range(0, ObstacleBlocks.Length)]);
                    CreatedObstacles[i - 1].transform.position = new Vector3
                    (
                        0f,
                        0f,
                        (-10) * i
                    );
                }
                EscapeBlock.transform.position = new Vector3
                (
                    0f,
                    0f,
                    (-10) * (ObstaclesAmount + 1)
                );
            }
            else
            {
                EscapeBlock.transform.position = new Vector3
                (
                    0f,
                    0f,
                    (-10)
                );
            }

        }

        void Update()
        {

        }
    }
}