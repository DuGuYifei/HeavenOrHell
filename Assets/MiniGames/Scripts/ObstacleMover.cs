using UnityEngine;

public class ObstacleMover : MonoBehaviour
{

    public float MovementSpeed = 0.5f;
    public float LeftLimit = -2.5f;
    public float RightLimit = 2.5f;

    public float InitialSideMovementSpeedSign = 1.0f;

    private Vector3 velocity;

    void Start()
    {
        velocity = new Vector3(InitialSideMovementSpeedSign * MovementSpeed, 0f, 0f);

    }

    // Update is called once per frame
    void Update()
    {
        if (transform.position.x < LeftLimit || transform.position.x > RightLimit)
        {
            velocity.x = -velocity.x;
        }
        transform.Translate(velocity * Time.deltaTime);
    }
}
