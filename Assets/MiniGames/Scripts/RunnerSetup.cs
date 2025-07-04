using UnityEngine;


namespace MiniGames.Runner
{
    public class RunnerSetup : MonoBehaviour
    {
        public Camera runnerCamera;
        public int ObstaclesAmount = 5;
        GameObject[] CreatedObstacles = new GameObject[5];

        public float StartTimer = 2.0f;
        public float StartTimerLength = 2.0f;
        public GameObject StartObject;

        public GameObject[] ObstacleBlocks;
        public GameObject StartBlock;
        public GameObject EscapeBlock;

        public GameObject FireWall;
        public GameObject Player;
        [SerializeField] private RunnerGamePlayerController playerController;
        [SerializeField] private Transform obstacleParent;

        void Start()
        {
            StartBlock.transform.localPosition = Vector3.zero;
            if (ObstacleBlocks.Length > 0)
            {
                for (int i = 1; i <= ObstaclesAmount; i++)
                {
                    CreatedObstacles[i - 1] = Instantiate<GameObject>(ObstacleBlocks[Random.Range(0, ObstacleBlocks.Length)], obstacleParent);
                    CreatedObstacles[i - 1].transform.localPosition = new Vector3
                    (
                        0f,
                        0f,
                        (-10) * i
                    );
                    CreatedObstacles[i - 1].transform.RotateAround(CreatedObstacles[i - 1].transform.position, Vector3.up, 180f);
                }
                EscapeBlock.transform.localPosition = new Vector3
                (
                    0f,
                    0f,
                    (-10) * (ObstaclesAmount + 1)
                );
            }
            else
            {
                EscapeBlock.transform.localPosition = new Vector3
                (
                    0f,
                    0f,
                    (-10)
                );
            }
        }

        public void Update()
        {
            if (StartTimer < 0f)
            {
                FireWall.GetComponent<RunnerWallController>().IsMoving = true;
                Player.GetComponent<RunnerGamePlayerController>().IsMoving = true;
                StartObject.SetActive(false);
                StartTimer = StartTimerLength * 2;
            }
            else if (StartTimer <= StartTimerLength)
            {
                StartTimer -= Time.deltaTime;
                StartObject.transform.localScale = new Vector3(
                    StartTimer / StartTimerLength,
                    StartTimer / StartTimerLength,
                    StartTimer / StartTimerLength
                );
            }
        }

        public void InitializeSoul(int soulIndex)
        {
            playerController.SetSoulPrefab(soulIndex);
        }
    }
}