using UnityEngine;
using System.Collections.Generic;

public class CollisionChecker : MonoBehaviour
{
    public float activationTime = 0.25f;
    public float lifeLength = 0.5f;
    private float lifeTime = 0f;
    private List<GameObject> currentCollisions = new List<GameObject>();
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        lifeTime += Time.deltaTime;

        if (lifeTime >= activationTime)
        {
            foreach (GameObject gObject in currentCollisions)
            {
                SoulContainer soulCon = gObject.GetComponent<SoulContainer>();
                if (soulCon != null && !soulCon._isWeak)
                {
                    transform.parent.GetComponent<ReaperContainer>().RegisterTheAttack(soulCon.id);
                }
            }
        }
        else if (lifeTime >= lifeLength)
        {
            Destroy(this);
        }

    }
    


    public void OnTriggerEnter2D(Collider2D collision)
    {
        currentCollisions.Add (collision.gameObject);
        
        foreach (GameObject gObject in currentCollisions)
        {
            print(gObject.name);
        }

    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        currentCollisions.Remove (collision.gameObject);

        foreach (GameObject gObject in currentCollisions)
        {
            print (gObject.name);
        }
    }
}
