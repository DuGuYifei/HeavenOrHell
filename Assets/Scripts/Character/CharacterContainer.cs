using System;
using Message;
using network;
using UnityEngine;
using utils;

public abstract class CharacterContainer : MonoBehaviour
{
    public int id;
    public SPUM_Prefabs prefab;
    public bool hasSkills = false;
    public bool isPlayer = false;

    // private Rigidbody2D _rigidbody2D;
    protected Transform ContainerTransform;
    private Transform _charTransform;
    private Vector3 _initialCharScale;
    private bool _wasLastMoveRight = true;
    
    protected AudioSource _audioSource;
    
    protected virtual void Awake()
    {
        ContainerTransform = transform;
        _charTransform = prefab.transform;
        _initialCharScale = _charTransform.localScale;
        // _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    protected virtual void Start()
    {
        prefab.OverrideControllerInit();
        OnInit();
    }

    public virtual void OnInit()
    {
        KcpRecvMessageParser.Instance?.onSoulBasicReceived.AddListener(HandleBasicMessage);

        try
        {
            _audioSource = GetComponent<AudioSource>();
        }
        catch (Exception e)
        {
            Debug.LogError("AudioSource not found on CharacterContainer: " + e.Message);
        }
    }

    protected virtual void HandleBasicMessage(PlayerBasicMessage basicMessage)
    {
        if (basicMessage.PlayerId != id || isPlayer) return;
        ContainerTransform.position = new Vector3(basicMessage.PositionX, basicMessage.PositionY, 0);
        switch (basicMessage.AnimationType)
        {
            case PlayerAnimationType.Idle:
                SetCharacterSide(_wasLastMoveRight);
                PlayAnimation(PlayerState.IDLE);
                break;
            case PlayerAnimationType.WalkLeft:
                SetCharacterSide(false);
                _wasLastMoveRight = false;
                PlayAnimation(PlayerState.MOVE);
                break;
            case PlayerAnimationType.WalkRight:
                _wasLastMoveRight = true;
                SetCharacterSide(true);
                PlayAnimation(PlayerState.MOVE);
                break;
            case PlayerAnimationType.Attack:
                PlayAnimation(PlayerState.ATTACK);
                break;
            case PlayerAnimationType.Die:
                PlayAnimation(PlayerState.DEATH);
                break;
            case PlayerAnimationType.Hit:
                PlayAnimation(PlayerState.DAMAGED);
                break;
            case PlayerAnimationType.DashLeft:
            case PlayerAnimationType.DashRight:
            case PlayerAnimationType.Weak:
            default:
                PlayAnimation(PlayerState.IDLE);
                break;
        }
        // if (_lastPosition != ContainerTransform.position)
        // {
        //     SetCharacterSide(ContainerTransform.position.x - _lastPosition.x > 0);
        //     PlayAnimation(PlayerState.MOVE);
        // }
        // else
        // {
        //     PlayAnimation(PlayerState.IDLE);
        // }
    }
    
    public void SetCharacterSide(bool isRight)
    {
        var scale = _initialCharScale;
        if (isRight)
        {
            scale.x = -Mathf.Abs(scale.x);
        }
        else
        {
            scale.x = Mathf.Abs(scale.x);
        }
        _charTransform.localScale = scale;
    }

    public void PlayAnimation(PlayerState state)
    {
        prefab.PlayAnimation(state, 0);
    }

    public abstract void SkillPerformed();

    public abstract void DashPerformed();

    public abstract void AttackPerformed();

    public abstract void GateActionPerformed();

}