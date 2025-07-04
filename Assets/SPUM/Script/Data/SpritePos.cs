using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpritePos : MonoBehaviour
{
    #if UNITY_EDITOR
    // Update is called once per frame
    void Update()
    {
        float tX = transform.localPosition.x;
        float tY = transform.localPosition.y;
        float tZ = transform.localPosition.z;
        tX = tX- (tX % 0.015625f);
        tY = tY- (tY % 0.015625f);
        transform.localPosition = new Vector3(tX,tY, tZ);
    }
    #endif
}