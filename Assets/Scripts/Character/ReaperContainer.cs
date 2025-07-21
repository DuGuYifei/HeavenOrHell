using System;
using System.Numerics;
using AntMill.Liu.Scripts.networks;
using DefaultNamespace.UI;
using Player;
using Unity.VisualScripting;
using UnityEngine;

public class ReaperContainer : CharacterContainer
{
    [SerializeField] private CircleCollider2D collider2d;
    [SerializeField] private CollisionChecker checker;
    [SerializeField] private float attackTimeLength = 1f;
    [SerializeField] private float debuffLength = 5.0f;
    [SerializeField] private float speedDebuff = 0.5f;
    [SerializeField] private float regularSpeedMultiplier = 1.05f;
    // public GameObject Pointer;

    private bool _attackPerformed;
    private float _initialSpeedMultiplier;
    private float _debuffTimer = 0f;
    private float attackDistance = 0.65f;
    private float collisionRadius = 0.25f;
    private PlayerControlManager _playerControlManager;


    public override void OnInit()
    {
        base.OnInit();
        if (isPlayer)
        {
            InGameUIManager.Instance?.InitializeUI(false);
            _playerControlManager = GetComponentInChildren<PlayerControlManager>();
            // _initialSpeedMultiplier = _playerControlManager.speedMultiplier;
            _initialSpeedMultiplier = regularSpeedMultiplier;
            //Change the color of the gates
            for (var i = 0; i < GameManager.Instance.mapInfoContainer.gatePositions.Count; i++)
            {
                GameManager.Instance.SetGateColor(i);
            }
        }
    }

    public override void SkillPerformed()
    {
        // No skills
    }

    public override void DashPerformed()
    {
        // No Dash
    }

    protected override void Start()
    {
        base.Start();
    }

    private void Update()
    {
        if (_attackPerformed)
        {
            _debuffTimer += Time.deltaTime;
            InGameUIManager.Instance?.UpdateReaperAttackCooldown(_debuffTimer / debuffLength);
            if (_debuffTimer >= debuffLength)
            {
                _playerControlManager.speedMultiplier = _initialSpeedMultiplier;
                _attackPerformed = false;
                _debuffTimer = 0f;
            }
        }
        if (isPlayer)
        {
            // Pointer.SetActive(true);
            // UnityEngine.Vector2 mousePos = Input.mousePosition;
            // mousePos = Camera.main.ScreenToWorldPoint(mousePos);

            // UnityEngine.Vector2 hitDir = new UnityEngine.Vector2(
            //     mousePos.x - transform.position.x,
            //     mousePos.y - transform.position.y
            // );
            // hitDir.Normalize();

            // hitDir = hitDir * attackDistance;

            // UnityEngine.Vector3 pointerPosition = new UnityEngine.Vector3(
            //     transform.position.x + hitDir.x,
            //     transform.position.y + hitDir.y,
            //     transform.position.z
            // );
            // Pointer.transform.position = pointerPosition;

        }
    }

    public override void AttackPerformed()
    {
        if (_attackPerformed) return;
        checker.StartCheck();
        // get the position of the mouse:
        // UnityEngine.Vector2 mousePos = Input.mousePosition;
        // mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        // UnityEngine.Vector2 hitDir = new UnityEngine.Vector2(
        //     mousePos.x - transform.position.x,
        //     mousePos.y - transform.position.y
        // );
        // hitDir.Normalize();

        // hitDir = hitDir * attackDistance;
        
        // // generate the position of the collision shape to place
        // UnityEngine.Vector3 collisionSpawn = new UnityEngine.Vector3(
        //     transform.position.x + hitDir.x,
        //     transform.position.y + hitDir.y,
        //     transform.position.z
        // );
        
        // float angle = Mathf.Atan2(
        //     (-1) * hitDir.y, 
        //     (-1) * hitDir.x
        // ) * Mathf.Rad2Deg;
        // UnityEngine.Quaternion collisionRotation = UnityEngine.Quaternion.AngleAxis(angle, UnityEngine.Vector3.forward);


        // CircleCollider2D collider = Instantiate<CircleCollider2D>(new CircleCollider2D());
        // collider2d.transform.position = collisionSpawn;
        // collider2d.transform.parent = transform;
        // collider2d.radius = collisionRadius;
        // collider2d.transform.rotation = collisionRotation;
        // collider.AddComponent<CollisionChecker>();
        PlayAnimation(PlayerState.ATTACK);
        
        // _audioSource.Play();
        
        // mb check for collisions and if there's a poor soul, DAMAGE it
        _attackPerformed = true;
        _debuffTimer = 0f;
        _playerControlManager.speedMultiplier *= speedDebuff;
    }

    public void PlayAttackSound()
    {
        _audioSource.Play();
    }

    public override void GateActionPerformed()
    {
        // No Gate Action
    }

    public void RegisterTheAttack(int victimID)
    {
        Debug.Log($"[Client→Server] Sent ReaperAttackMessage: VictimID={victimID}");
        KcpNetwork.Instance.SendReaperAttackMessage(victimID);
    }
}