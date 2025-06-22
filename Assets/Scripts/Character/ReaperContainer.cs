using System.Numerics;
using AntMill.Liu.Scripts.networks;
using Unity.VisualScripting;
using UnityEngine;

public class ReaperContainer : CharacterContainer
{

    private float attackDistance = 0.5f;
    private float collisionRadius = 0.25f;
    public override void OnInit()
    {
        base.OnInit();
    }

    public override void SkillPerformed()
    {
        // No skills
    }

    public override void DashPerformed()
    {
        // No Dash
    }

    public override void AttackPerformed()
    {
        // get the position of the mouse:
        UnityEngine.Vector2 mousePos = Input.mousePosition;
        mousePos = Camera.main.ScreenToWorldPoint(mousePos);

        UnityEngine.Vector2 hitDir = new UnityEngine.Vector2(
            mousePos.x - transform.position.x,
            mousePos.y - transform.position.y
        );
        hitDir.Normalize();

        hitDir = hitDir * attackDistance;

        // generate the position of the collision shape to place
        UnityEngine.Vector3 collisionSpawn = new UnityEngine.Vector3(
            transform.position.x + hitDir.x,
            transform.position.y + hitDir.y,
            transform.position.z
        );
        CircleCollider2D collider = Instantiate<CircleCollider2D>(new CircleCollider2D());
        collider.transform.position = collisionSpawn;
        collider.transform.parent = transform;
        collider.radius = collisionRadius;
        collider.AddComponent<CollisionChecker>();

        // mb check for collisions and if there's a poor soul, DAMAGE it

    }

    public void RegisterTheAttack(int victimID)
    {
        Debug.Log($"[Client→Server] Sent ReaperAttackMessage: VictimID={victimID}");
        KcpNetwork.Instance.SendReaperAttackMessage(victimID, 0);
    }
}