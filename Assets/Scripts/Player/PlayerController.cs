using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionAsset playerActionAsset;
        [SerializeField] private float moveSpeed;

        private InputAction _moveAction;
        private Rigidbody2D _rigidbody2D;


        private void Awake()
        {
            var actionMap = playerActionAsset.FindActionMap("Player", true);
            _moveAction = actionMap.FindAction("Move", true);
            _rigidbody2D = transform.parent.GetComponent<Rigidbody2D>();
            transform.parent.GetComponent<Collider2D>().enabled = true;
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Move()
        {
            Vector2 moveInput = _moveAction.ReadValue<Vector2>();
            moveInput *= moveSpeed;
            _rigidbody2D.linearVelocity = moveInput;
        }
    }
}