using System;
using Message;
using network;
using Spine.Unity;
using UnityEngine;
using utils;

public abstract class CharacterContainer : MonoBehaviour
{
    public int id;
    public SPUM_Prefabs prefab;
    public bool hasSkills = false;
    public bool isPlayer;

    // private Rigidbody2D _rigidbody2D;
    protected Transform ContainerTransform;
    private Transform _charTransform;
    private Vector3 _initialCharScale;
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
        if (!isPlayer)
        {
            KcpRecvMessageParser.Instance?.onSoulBasicReceived.AddListener(HandleBasicMessage);
        }
    }

    protected virtual void HandleBasicMessage(PlayerBasicMessage basicMessage)
    {
        ContainerTransform.position = new Vector3(basicMessage.PositionX, basicMessage.PositionY, 0);
        
    }

    public void SetPose(Vector2 pos)
    {
        var lastPos = ContainerTransform.position;
        ContainerTransform.position = pos;
        var diffX = lastPos.x - pos.x;
        if (Mathf.Abs(diffX) > 0)
        {
            SetCharacterSide(diffX > 0);
            PlayAnimation(PlayerState.MOVE);
        }
        else
        {
            PlayAnimation(PlayerState.IDLE);
        }
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

}