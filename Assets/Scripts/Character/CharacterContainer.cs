using System;
using Spine.Unity;
using UnityEngine;
using utils;

public abstract class CharacterContainer : MonoBehaviour
{
    public int id;
    public SPUM_Prefabs prefab;

    // private Rigidbody2D _rigidbody2D;
    private Transform _transform;
    private Transform _charTransform;
    private Vector3 _initialCharScale;
    private void Awake()
    {
        _transform = transform;
        _charTransform = prefab.transform;
        _initialCharScale = _charTransform.localScale;
        // _rigidbody2D = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        prefab.OverrideControllerInit();
    }
        
    public void SetPose(Vector2 pos)
    {
        var lastPos = _transform.position;
        _transform.position = pos;
        var diffX = lastPos.x - pos.x;
        if (Mathf.Abs(diffX) > 0)
        {
            SetCharacterSide(diffX > 0);
            prefab.PlayAnimation(PlayerState.MOVE, 0);
        }
        else
        {
            prefab.PlayAnimation(PlayerState.IDLE, 0);
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
    
}