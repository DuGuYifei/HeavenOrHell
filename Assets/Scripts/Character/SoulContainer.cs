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

    #region Properties

    public SoulType SoulType => soulType;

    #endregion
}

public enum SoulType
{
    Dog,
    Psychologist,
    Detective
}