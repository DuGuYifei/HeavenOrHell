using UnityEngine;

namespace MiniGames.Runner
{
    public class RunnerGameCameraController : MonoBehaviour
    {

        public GameObject player;
        public Vector3 offset = new Vector3(0f, 0f, 0f);

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            transform.position = new Vector3(
                0.0f,
                offset.y,
                player.transform.position.z + offset.z 
            );
        }
    }
}