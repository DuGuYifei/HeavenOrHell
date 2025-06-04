using UnityEngine;


namespace MiniGames.Runner
{
    public class RunnerWallController : MonoBehaviour
    {

        public float WallSpeed = 7.5f;
        Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            rb.linearVelocity = new Vector3
            (
                0f,
                0f,
                WallSpeed * (-1)
            );
        }
    }
}