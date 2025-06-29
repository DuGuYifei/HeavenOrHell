using System.Collections.Generic;
using AntMill.Liu.Scripts.networks;
using DefaultNamespace.UI;
using Message;
using network;
using Player;
using UnityEngine;
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

    private Material _sharedMaterial;
    private Color _defaultColor;
    private PlayerControlManager _playerControlManager;
    private bool _inDash = false;
    private float _dashTime = 0f;
    private float _timeSinceLastDash = 0f;
    private float _initialSpeedMultiplier;
    

    public bool _isWeak = false;
    public float _hp = 100.0f;
    public float _weakHP = 120.0f;
    public float _maxHp = 100.0f;
    private bool _enteredGate = false;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Start()
    {
        base.Start();
        _playerControlManager = GetComponentInChildren<PlayerControlManager>();
    }

    private void Update()
    {
        if (_hp <= 0)
        {
            _isWeak = true;
            _hp = 0.0f;
        }
        _timeSinceLastDash += Time.deltaTime;
        if (!_inDash) return;
        _dashTime += Time.deltaTime;
        if (_dashTime < dashDuration) return;
        _inDash = false;
        _dashTime = 0f;
        _playerControlManager.speedMultiplier = _initialSpeedMultiplier;
    }
    
    public override void OnInit()
    {
        base.OnInit();
        if (isPlayer) KcpRecvMessageParser.Instance?.onReaperResultReceived.AddListener(ReaperResultReceived);
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
        if (basicMessage.CharacterState == CharacterState.Weak && !_isWeak)
        {
            _isWeak = true;
            ChangeMaterialColor(true);
        } else if (basicMessage.CharacterState == CharacterState.Normal && _isWeak)
        {
            _isWeak = false;
            ChangeMaterialColor(false);
        }
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
    }

    public override void AttackPerformed()
    {
        // no attack ... YET
    }
    
    private void ReaperResultReceived(int playerId)
    {
        if (playerId != id) return;
        _hp = 0.0f;
        _isWeak = true;
    }

    public override void GateActionPerformed()
    {
        if (_enteredGate) return;
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
        if (nearestGate != GameManager.Instance.mapInfoContainer.heavenGateIndex)
        {
            GameEndUI.Instance.TurnOnGameEndPanel(false);
        }
        else
        {
            GameEndUI.Instance.TurnOnGameEndPanel(true);
        }
        KcpNetwork.Instance.SendEnterGateMessage(id, GameManager.Instance.mapInfoContainer.gateDirections[nearestGate]);
    }

    #region Properties

    public SoulType SoulType => soulType;
    public bool IsWeak => _isWeak;

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
    Psychologist,
    Detective
}