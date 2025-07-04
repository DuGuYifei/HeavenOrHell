using UnityEngine;


namespace MiniGames.Runner
{
    public class RunnerSetup : MonoBehaviour
    {
        public Camera runnerCamera;
        public int ObstaclesAmount = 5;
        GameObject[] CreatedObstacles = new GameObject[5];

        public GameObject[] ObstacleBlocks;
        public GameObject StartBlock;
        public GameObject EscapeBlock;
        [SerializeField] private RunnerGamePlayerController playerController;
        [SerializeField] private Transform obstacleParent;

        void Start()
        {
            StartBlock.transform.position = Vector3.zero;
            if (ObstacleBlocks.Length > 0)
            {
                for (int i = 1; i <= ObstaclesAmount; i++)
                {
                    CreatedObstacles[i - 1] = Instantiate<GameObject>(ObstacleBlocks[Random.Range(0, ObstacleBlocks.Length)], obstacleParent);
                    CreatedObstacles[i - 1].transform.position = new Vector3
                    (
                        0f,
                        0f,
                        (-10) * i
                    );
                    CreatedObstacles[i - 1].transform.RotateAround(CreatedObstacles[i - 1].transform.position, Vector3.up, 180f);
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

        public void InitializeSoul(int soulIndex)
        {
            playerController.SetSoulPrefab(soulIndex);
        }
    }
}