using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerControlManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset playerActionAsset;
        [SerializeField] private float moveSpeed;
        public float speedMultiplier = 1f;

        private InputAction _moveAction;
        private InputAction _skillAction;
        private InputAction _dashAction;
        private Rigidbody2D _rigidbody2D;
        private CharacterContainer _characterContainer;


        private void Awake()
        {
            var actionMap = playerActionAsset.FindActionMap("Player", true);
            _moveAction = actionMap.FindAction("Move", true);
            _skillAction = actionMap.FindAction("Skill", true);
            _dashAction = actionMap.FindAction("Dash", true);
            _rigidbody2D = transform.parent.GetComponent<Rigidbody2D>();
            _characterContainer = transform.parent.GetComponent<CharacterContainer>();
            transform.parent.GetComponent<Collider2D>().enabled = true;
        }

        private void OnEnable()
        {
            _skillAction.Enable();
            _skillAction.performed += OnSkillActionPerformed;
            _dashAction.Enable();
            _dashAction.performed += OnDashActionPerformed;
        }

        private void OnDisable()
        {
            _skillAction.Disable();
            _skillAction.performed -= OnSkillActionPerformed;
            _dashAction.Disable();
            _dashAction.performed -= OnDashActionPerformed;
        }

        private void OnSkillActionPerformed(InputAction.CallbackContext obj)
        {
            print("skillPerformed");
            _characterContainer.SkillPerformed();
        }
        
        private void OnDashActionPerformed(InputAction.CallbackContext obj)
        {
            print("dashPerformed");
            _characterContainer.DashPerformed();
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            moveInput *= moveSpeed * speedMultiplier;
            _rigidbody2D.linearVelocity = moveInput;
            if (moveInput.x != 0 || moveInput.y != 0)
            {
                _characterContainer.prefab.PlayAnimation(PlayerState.MOVE, 0);
                _characterContainer.SetCharacterSide(moveInput.x > 0);
            }
            else
            {
                _characterContainer.prefab.PlayAnimation(PlayerState.IDLE, 0);
            }
        }
    }
}