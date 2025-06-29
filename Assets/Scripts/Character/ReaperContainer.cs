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

    private bool _attackPerformed;
    private float _initialSpeedMultiplier;
    private float _debuffTimer = 0f;
    private float attackDistance = 0.5f;
    private float collisionRadius = 0.25f;
    private PlayerControlManager _playerControlManager;
    
    
    public override void OnInit()
    {
        base.OnInit();
        if (isPlayer) InGameUIManager.Instance?.InitializeUI(false);

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
        _playerControlManager = GetComponentInChildren<PlayerControlManager>();
        _initialSpeedMultiplier = _playerControlManager.speedMultiplier;
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
    }

    public override void AttackPerformed()
    {
        if (_attackPerformed) return;
        checker.StartCheck();
        // get the position of the mouse:
        UnityEngine.Vector2 mousePos = Input.mousePosition;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        UnityEngine.Vector2 hitDir = new UnityEngine.Vector2(
            mousePos.x - transform.position.x,
            mousePos.y - transform.position.y
        );
        hitDir.Normalize();

        hitDir = hitDir * attackDistance;

        // generate the position of the collision shape to place
        UnityEngine.Vector3 collisionSpawn = new UnityEngine.Vector3(
            transform.position.x + hitDir.x,
            transform.position.y + hitDir.y,
            transform.position.z
        );
        // CircleCollider2D collider = Instantiate<CircleCollider2D>(new CircleCollider2D());
        collider2d.transform.position = collisionSpawn;
        collider2d.transform.parent = transform;
        collider2d.radius = collisionRadius;
        // collider.AddComponent<CollisionChecker>();
        PlayAnimation(PlayerState.ATTACK);
        // mb check for collisions and if there's a poor soul, DAMAGE it
        _attackPerformed = true;
        _debuffTimer = 0f;
        _playerControlManager.speedMultiplier *= speedDebuff;
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