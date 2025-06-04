using Spine.Unity;
using UnityEngine;
using utils;

public class CharacterContainer : MonoBehaviour
{
    public int id;

    // private Rigidbody2D _rigidbody2D;
    private Transform _transform;
    private void Awake()
    {
        _transform = transform;
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
                    
                }
                else
                {
                    
                }
            }
            else
            {
                // Vertical movement
                if (direction.y > 0)
                {
                    
                }
                else
                {
                    
                }
            }
        }
        else
        {
        }
    }
}