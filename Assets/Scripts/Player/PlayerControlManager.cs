using System;
using AntMill.Liu.Scripts.networks;
using Message;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerControlManager : MonoBehaviour
    {
        [SerializeField] private InputActionAsset playerActionAsset;
        [SerializeField] private float moveSpeed;
        public float speedMultiplier = 1f;

        public float speedDebuff = 0.5f;
        public float debuffLength = 5.0f;
        public float debuffTimer = 0.0f;

        private InputAction _moveAction;
        private InputAction _skillAction;
        private InputAction _dashAction;
        private InputAction _mapAction;
        private InputAction _attackAction;
        private Rigidbody2D _rigidbody2D;
        private CharacterContainer _characterContainer;
        private bool _isSoul = false;
        
        private void Awake()
        {
            var actionMap = playerActionAsset.FindActionMap("Player", true);
            _moveAction = actionMap.FindAction("Move", true);
            _skillAction = actionMap.FindAction("Skill", true);
            _dashAction = actionMap.FindAction("Dash", true);
            _mapAction = actionMap.FindAction("Map", true);
            _attackAction = actionMap.FindAction("Attack", true);
            _rigidbody2D = transform.parent.GetComponent<Rigidbody2D>();
            _characterContainer = transform.parent.GetComponent<CharacterContainer>();
            // check if the character is a soul
            _isSoul = _characterContainer is SoulContainer;
            transform.parent.GetComponent<Collider2D>().enabled = true;
        }

        private void OnEnable()
        {
            _skillAction.Enable();
            _skillAction.performed += OnSkillActionPerformed;
            _dashAction.Enable();
            _dashAction.performed += OnDashActionPerformed;
            _mapAction.Enable();
            _mapAction.started += OnMapStarted;
            _mapAction.canceled += OnMapEnded;
            _attackAction.Enable();
            _attackAction.performed += OnAttackPerformed;
        }

        private void OnDisable()
        {
            _skillAction.Disable();
            _skillAction.performed -= OnSkillActionPerformed;
            _dashAction.Disable();
            _dashAction.performed -= OnDashActionPerformed;
            _mapAction.Disable();
            _mapAction.performed -= OnMapStarted;
            _mapAction.canceled -= OnMapEnded;
        }

        private void OnMapStarted(InputAction.CallbackContext obj)
        {
            GameManager.Instance?.MiniMapController?.SetVisibility(true);
        }
        
        private void OnMapEnded(InputAction.CallbackContext obj)
        {
            GameManager.Instance?.MiniMapController?.SetVisibility(false);
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

        private void OnAttackPerformed(InputAction.CallbackContext obj)
        {
            print("attackPerformed");
            if (_characterContainer is ReaperContainer && debuffTimer <= 0.0f)
            {
                _characterContainer.AttackPerformed();
                debuffTimer = debuffLength;
            }
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            moveInput *= moveSpeed * speedMultiplier;
            if (debuffTimer > 0.0f)
            {
                moveInput *= speedDebuff;
                debuffTimer -= Time.deltaTime;
            }
            _rigidbody2D.linearVelocity = moveInput;
            var position = transform.position;
            if (moveInput.x != 0 || moveInput.y != 0)
            {
                _characterContainer.prefab.PlayAnimation(PlayerState.MOVE, 0);
                _characterContainer.SetCharacterSide(moveInput.x > 0);
                KcpNetwork.Instance.SendPlayerBasicMessage(position.x, position.y, 
                    _isSoul? ((SoulContainer) _characterContainer)._hp : 100.0f, 
                    _isSoul? ((SoulContainer) _characterContainer)._maxHp : 100.0f
                    , moveInput.x > 0 ? PlayerAnimationType.WalkRight: PlayerAnimationType.WalkLeft);
            }
            else
            {
                _characterContainer.prefab.PlayAnimation(PlayerState.IDLE, 0);
                KcpNetwork.Instance.SendPlayerBasicMessage(position.x, position.y, 
                    _isSoul? ((SoulContainer) _characterContainer)._hp : 100.0f, 
                    _isSoul? ((SoulContainer) _characterContainer)._maxHp : 100.0f
                    , PlayerAnimationType.Idle);
            }
        }
    }
}