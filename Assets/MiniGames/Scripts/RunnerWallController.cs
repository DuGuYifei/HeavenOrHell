using UnityEngine;


namespace MiniGames.Runner
{
    public class RunnerWallController : MonoBehaviour
    {

        public float WallSpeed = 7.5f;
        public bool IsMoving = false;
        Rigidbody rb;

        void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        void Update()
        {
            if (IsMoving)
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
}