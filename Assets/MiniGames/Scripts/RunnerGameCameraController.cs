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
            transform.localPosition = new Vector3(
                0.0f,
                offset.y,
                player.transform.localPosition.z + offset.z 
            );
        }
    }
}