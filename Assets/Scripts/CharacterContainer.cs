using Spine.Unity;
using UnityEngine;

public class CharacterContainer : MonoBehaviour
{
    public int id;
    public SkeletonAnimation skeletonAnimation;

    // private Rigidbody2D _rigidbody2D;
    private Transform _transform;
    private void Awake()
    {
        _transform = transform;
        skeletonAnimation = GetComponentInChildren<SkeletonAnimation>();
        // _rigidbody2D = GetComponent<Rigidbody2D>();
    }
        
    public void SetPose(Vector2 pos)
    {
        _transform.position = pos;
    }
}