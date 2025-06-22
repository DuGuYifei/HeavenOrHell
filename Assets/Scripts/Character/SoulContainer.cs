using Player;
using UnityEngine;

public abstract class SoulContainer : CharacterContainer
{
    [SerializeField] private SoulType soulType;
    [SerializeField] private float dashCooldown = 4f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private float dashSpeedMultiplier = 2f;

    private PlayerControlManager _playerControlManager;
    private bool _inDash = false;
    private float _dashTime = 0f;
    private float _timeSinceLastDash = 0f;
    private float _initialSpeedMultiplier;

    public bool _isWeak = false;
    public float _hp = 100.0f;
    public float _weakHP = 120.0f;
    public float _maxHp = 100.0f;

    protected override void Awake()
    {
        base.Awake();
        _playerControlManager = GetComponentInChildren<PlayerControlManager>();
    }

    private void Update()
    {
        _timeSinceLastDash += Time.deltaTime;
        if (!_inDash) return;
        _dashTime += Time.deltaTime;
        if (!(_dashTime >= dashDuration)) return;
        _inDash = false;
        _dashTime = 0f;
        _playerControlManager.speedMultiplier = _initialSpeedMultiplier;
        if (_hp <= 0)
        {
            _isWeak = true;
            _hp = 0.0f;
        }
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

    #region Properties

    public SoulType SoulType => soulType;
    public bool IsWeak => _isWeak;

    #endregion
}

public enum SoulType
{
    Dog,
    Psychologist,
    Detective
}