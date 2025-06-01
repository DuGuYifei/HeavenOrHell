using Spine.Unity;
using UnityEngine;
using utils;

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
        var lastPos = _transform.position;
        _transform.position = pos;
        if ((lastPos.XY() - pos).sqrMagnitude > 0.01f)
        {
            // Check direction and set animation accordingly
            Vector2 direction = pos - lastPos.XY();
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                // Horizontal movement
                if (direction.x > 0)
                {
                    skeletonAnimation.initialSkinName = "Side";
                    skeletonAnimation.AnimationState.SetAnimation(0, "Side_Walk", true);
                }
                else
                {
                    skeletonAnimation.initialSkinName = "Side";
                    skeletonAnimation.AnimationState.SetAnimation(0, "Side_Walk", true);
                }
            }
            else
            {
                // Vertical movement
                if (direction.y > 0)
                {
                    skeletonAnimation.initialSkinName = "Back";
                    skeletonAnimation.AnimationState.SetAnimation(0, "Back_Walk", true);
                }
                else
                {
                    skeletonAnimation.initialSkinName = "Front";
                    skeletonAnimation.AnimationState.SetAnimation(0, "Front_Walk", true);
                }
            }
        }
        else
        {
            skeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
        }
    }
}