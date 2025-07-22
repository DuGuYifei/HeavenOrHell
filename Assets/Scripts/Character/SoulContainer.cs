using System.Collections.Generic;
using AntMill.Liu.Scripts.networks;
using Assets.Scripts.game.sfx;
using DefaultNamespace.UI;
using Message;
using network;
using Player;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class SoulContainer : CharacterContainer
{
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    [SerializeField] private SoulType soulType;
    [SerializeField] private float dashCooldown = 4f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashSpeedMultiplier = 2f;
    [SerializeField] private float gateCheckRange = 1f;
    [SerializeField] [ColorUsage(true, true)] private Color weakColor;
    [SerializeField] private Vector3 farPosition = new Vector3(-100, 0, 0);
    private Material _sharedMaterial;
    private Color _defaultColor;
    private PlayerControlManager _playerControlManager;
    private bool _inDash = false;
    private float _dashTime = 0f;
    private float _timeSinceLastDash = 0f;
    private float _initialSpeedMultiplier;


    private bool _isDead = false;
    public bool isWeak = false;
    private float _hp = 100.0f;
    private float _weakTimer = 0f;
    private float _maxHp = 100.0f;
    private bool _enteredGate = false;
    private Vector3 _enteredGatePosition = Vector3.zero;
    
    private int _nearestGate = -1;
    
    protected GhostingContainer GhostingContainer;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        _playerControlManager = GetComponentInChildren<PlayerControlManager>();
        GhostingContainer = GetComponent<GhostingContainer>();
    }

    private void Update()
    {
        if (_hp <= 0)
        {
            isWeak = true;
            _hp = 0.0f;
        }
        _timeSinceLastDash += Time.deltaTime;
        InGameUIManager.Instance?.UpdateSoulDashCooldown(_timeSinceLastDash / dashCooldown);

        if (!_inDash) return;
        _dashTime += Time.deltaTime;
        if (_dashTime < dashDuration) return;
        GhostingContainer.StopEffect();
        _inDash = false;
        _dashTime = 0f;
        _playerControlManager.speedMultiplier = _initialSpeedMultiplier;
        // if (isWeak)
        // {
        //     //check if the player is the only soul left
        //     
        // }
    }
    
    public override void OnInit()
    {
        base.OnInit();
        KcpRecvMessageParser.Instance?.onReaperResultReceived.AddListener(ReaperResultReceived);
        if (isPlayer)
        {
            InGameUIManager.Instance?.InitializeUI(true);
        }
        var renderers = GetComponentsInChildren<SpriteRenderer>();
        for (var i = 0; i < renderers.Length; i++)
        {
            if (!_sharedMaterial) _sharedMaterial = renderers[i].material;
            else renderers[i].material = _sharedMaterial;
        }

        _defaultColor = _sharedMaterial.GetColor(BaseColor);
    }

    protected override void HandleBasicMessage(PlayerBasicMessage basicMessage)
    {
        if (basicMessage.PlayerId != id) return;
        base.HandleBasicMessage(basicMessage);
        if (_isDead) return;
        if (basicMessage.CharacterState == CharacterState.Weak && !isWeak)
        {
            isWeak = true;
            ChangeMaterialColor(true);
            if (isPlayer)
            {
                InGameUIManager.Instance?.TurnDeathIndicatorOn(true);
            }
        } else if (basicMessage.CharacterState == CharacterState.Normal && isWeak)
        {
            isWeak = false;
            ChangeMaterialColor(false);
            if (isPlayer)
            {
                InGameUIManager.Instance?.TurnDeathIndicatorOn(false);
            }
        } else if (basicMessage.CharacterState == CharacterState.Die && isWeak)
        {
            //Dead by weak
            _playerControlManager.enabled = false;
            _isDead = true;
            transform.position = farPosition;
            KcpNetwork.Instance.SendPlayerBasicMessage(farPosition.x, farPosition.y, GameManager.Instance.PlayerId,
                PlayerAnimationType.Die);
            GameEndUI.Instance.TurnOnSoulEndPanel(false, true);
        }

        _hp = basicMessage.Hp;
        if (isPlayer)
        {
            InGameUIManager.Instance?.SetHearts(_hp / _maxHp);
        }
        
        _maxHp = basicMessage.MaxHp;
        _weakTimer = basicMessage.WeakTimer;
        if (_weakTimer > 0f && isPlayer) InGameUIManager.Instance?.UpdateSoulWeakTimer(_weakTimer / 60f);
    }

    public void ChangeMaterialColor(bool toWeak)
    {
        if (!_sharedMaterial) return;
        _sharedMaterial.SetColor(BaseColor, toWeak ? weakColor : _defaultColor);
    }

    public override void DashPerformed()
    {
        if (_timeSinceLastDash < dashCooldown) return;
        _inDash = true;
        _dashTime = 0f;
        _timeSinceLastDash = 0f;
        _initialSpeedMultiplier = _playerControlManager.speedMultiplier;
        _playerControlManager.speedMultiplier = dashSpeedMultiplier;
        GhostingContainer.Init(5, 0.02f);

    }

    public override void AttackPerformed()
    {
        // no attack ... YET
    }
    
    private void ReaperResultReceived(int playerId)
    {
        print("received reaper result for player: " + playerId);
        // prefab.PlayAnimation(PlayerState.DAMAGED, 0);
        // if (playerId != id) return;
        if (playerId == id)
        {
            _audioSource.Play();
        }
    }

    public override void GateActionPerformed()
    {
        if (_enteredGate || isWeak) return;
        var nearestGate = -1;
        var minDistance = float.MaxValue;
        foreach (var gate in GameManager.Instance.mapInfoContainer.gatePositions)
        {
            var distance = Vector3.Distance(transform.position, gate);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestGate = GameManager.Instance.mapInfoContainer.gatePositions.IndexOf(gate);
            }
        }

        if (minDistance > gateCheckRange) return;
        _enteredGate = true;
        _playerControlManager.enabled = false;
        _enteredGatePosition = transform.position;
        KcpNetwork.Instance.SendPlayerBasicMessage(farPosition.x, farPosition.y, GameManager.Instance.PlayerId,
            PlayerAnimationType.Idle);
        transform.position = farPosition;
        _nearestGate = nearestGate;
        if (!GameManager.Instance.mapInfoContainer.heavenGateDirections.Contains(GameManager.Instance.mapInfoContainer.gateDirections[nearestGate]))
        {
            // GameEndUI.Instance.TurnOnGameEndPanel(false);
            
            GameManager.Instance.SpawnGateMinigame(soulType);
        }
        else
        {
            GameEndUI.Instance.TurnOnSoulEndPanel(true);
            KcpNetwork.Instance.SendEnterGateMessage(id, GameManager.Instance.mapInfoContainer.gateDirections[nearestGate]);
        }

    }
    
    
    public void RunnerMinigameFinished(bool isHeavenGate)
    {
        if (!isPlayer) return;
        if (isHeavenGate)
        {
            _playerControlManager.enabled = true;
            _enteredGate = false;
            transform.position = _enteredGatePosition;
        }
        else
        {
            GameEndUI.Instance?.TurnOnSoulEndPanel(false);
            KcpNetwork.Instance.SendEnterGateMessage(id, GameManager.Instance.mapInfoContainer.gateDirections[_nearestGate]);
            _isDead = true;
        }
        
    }

    #region Properties

    public SoulType SoulType => soulType;
    public bool IsWeak => isWeak;

    #endregion

}

#if UNITY_EDITOR
[CustomEditor(typeof(SoulContainer), true)]
public class SoulContainerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var soulContainer = (SoulContainer) target;
        if (GUILayout.Button("Change Material Color"))
        {
            soulContainer.ChangeMaterialColor(soulContainer.IsWeak);
        }
    }
}
#endif

public enum SoulType
{
    Dog,
    Detective,
    Psychologist,
}