using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MiniGames.Runner {

    public class RunnerGamePlayerController : MonoBehaviour
    {
        [SerializeField][Tooltip("Dog is 0, detective is 1, psychologist is 2")] private List<SPUM_Prefabs> soulPrefabs;
        [SerializeField] private float soulInstanceScale = 2.6f;
        public float ForwardSpeed = 7.5f;
        public float Acceleration = 200.0f;
        public float SideSpeed = 7.5f;
        public float SideAcceleration = 200.0f;
        public float JumpSpeed = 40.0f;
        public float GravityScale = 100.0f;

        public float SideBoundary = 2.0f;

        public float SlowSpeed = 3.25f;
        public float SlowDuration = 1.5f;
        float SlowTimer = 0f;

        public InputActionAsset inputAction;
        private Transform _spriteInstanceTransform;
        private Vector3 _spriteLeftScale;
        private Vector3 _spriteRightScale;
        InputActionMap minigameInputMap;

        Rigidbody rb;

        void OnDisable()
        {
            minigameInputMap.Disable();
        }

        void Start()
        {
            if (inputAction != null)
            {
                minigameInputMap = inputAction.FindActionMap("Main");
                minigameInputMap.Enable();
            }
            rb = GetComponent<Rigidbody>();
            if(!GameManager.Instance)SetSoulPrefab(0);
        }

        void Update()
        {
            if (transform.position.y <= 0f)
            {
                transform.position = new Vector3
                (
                    transform.position.x,
                    0.0f,
                    transform.position.z
                );
            }
            if (transform.position.x < (-1) * SideBoundary)
            {
                transform.position = new Vector3
                (
                    (-1) * SideBoundary,
                    transform.position.y,
                    transform.position.z
                );
            }
            else if (transform.position.x > SideBoundary)
            {
                transform.position = new Vector3
                (
                    SideBoundary,
                    transform.position.y,
                    transform.position.z
                );
            }

            if (rb == null)
            {
                return;
            }
            if (Mathf.Abs(rb.linearVelocity.z) < ForwardSpeed)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y,
                    rb.linearVelocity.z - Acceleration * Time.deltaTime
                );
            }
            else
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y,
                    SlowTimer > 0f ? SlowSpeed * (-1) : ForwardSpeed * (-1)
                );
            }

            if (Mathf.Abs(rb.linearVelocity.x) <= SideSpeed)
            {
                if (minigameInputMap.FindAction("Left").IsPressed())
                {
                    rb.linearVelocity = rb.linearVelocity + new Vector3(
                        (-1) * SideAcceleration * Time.deltaTime,
                        0f,
                        0f
                    );
                    _spriteInstanceTransform.localScale = _spriteLeftScale;
                }
                else if (minigameInputMap.FindAction("Right").IsPressed())
                {
                    rb.linearVelocity = rb.linearVelocity + new Vector3(
                        SideAcceleration * Time.deltaTime,
                        0f,
                        0f
                    );
                    _spriteInstanceTransform.localScale = _spriteRightScale;
                }
            }
            else
            {
                rb.linearVelocity = new Vector3(
                    SideSpeed * (rb.linearVelocity.x > 0 ? 1 : (-1)),
                    rb.linearVelocity.y,
                    rb.linearVelocity.z
                );
            }


            if (minigameInputMap.FindAction("Jump").triggered && transform.position.y == 0f)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    JumpSpeed,
                    rb.linearVelocity.z
                );
            }

            if (GravityScale > 0f && transform.position.y > 0f)
            {
                rb.linearVelocity += new Vector3
                (
                    0f,
                    (-1) * GravityScale * Time.deltaTime,
                    0f
                );
            }
            if (SlowTimer > 0f)
            {
                SlowTimer -= Time.deltaTime;
            }
            else
            {
                SlowTimer = 0f;
            }
        }
        
        public void SetSoulPrefab(int prefabIndex)
        {
            if (prefabIndex < 0 || prefabIndex >= soulPrefabs.Count)
            {
                Debug.LogError("Invalid prefab index: " + prefabIndex);
                return;
            }
            SPUM_Prefabs selectedPrefab = soulPrefabs[prefabIndex];
            if (!selectedPrefab) return;
            var instance = Instantiate(selectedPrefab, transform, false);
            _spriteInstanceTransform = instance.transform;
            _spriteLeftScale = new Vector3(soulInstanceScale, soulInstanceScale, soulInstanceScale);
            _spriteRightScale = new Vector3(-soulInstanceScale, soulInstanceScale, soulInstanceScale);
            _spriteInstanceTransform.localScale = _spriteRightScale;
            instance.OverrideControllerInit();
            instance.PlayAnimation(PlayerState.MOVE, 0);
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "FireWall")
            {
                Debug.Log("HANDLE DEATH");
            }
            else if (other.tag == "EscapeWall")
            {
                Debug.Log("HANDLE RETURN");
            }
            else if (other.tag == "Obstacle")
            {
                Debug.Log("Obstacle touched => slowing down");
                SlowTimer = SlowDuration;
            }
        }
    }
}