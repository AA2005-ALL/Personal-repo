using System.Collections.Generic;
using UnityEngine;

public class RPGAgent : SteeringAgent
{
    private Attack.AttackType attackType = Attack.AttackType.Rocket;
    private PathfindingController pathfinder;
    private seekBehaviour seekScript;
    public List<Node> currentPath;
    private float rpgCooldown = 2;
    [SerializeField] private float currentRPGCooldown;
    protected override void InitialiseFromAwake()
    {
        pathfinder = gameObject.AddComponent<PathfindingController>();
        pathfinder.CreateNodes();
        pathfinder.CreateNodeConnections();
        seekScript = gameObject.AddComponent<seekBehaviour>();
        currentPath = pathfinder.ExecutePathfind();
        currentRPGCooldown = rpgCooldown;
    }

    protected override void CooperativeArbitration()
    {
        base.CooperativeArbitration();

        currentRPGCooldown -= Time.deltaTime;
       
        if(Vector3.Distance(transform.position, pathfinder.currentEnemyTarget.transform.position) <= 10)
        {
            if(currentRPGCooldown <= 0 && pathfinder.currentEnemyTarget.Health > 0)
            {
                AttackWith(attackType);
                currentRPGCooldown = rpgCooldown;
            }

            else if (pathfinder.currentEnemyTarget.Health <= 0)
            {
                seekScript.currentNode = 0;
                currentPath = pathfinder.ExecutePathfind();
            }
        }
    }

    protected override void UpdateDirection()
    {
        base.UpdateDirection();
    }

}
