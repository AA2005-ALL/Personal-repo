using System.Collections.Generic;
using UnityEngine;

public class MeleeAgent : SteeringAgent
{
    private Attack.AttackType attackType = Attack.AttackType.Melee;
    private PathfindingController pathfinder;
    private seekBehaviour seekScript;
    public float withinEnemyReach;
    public List<Node> currentPath;

    protected override void InitialiseFromAwake()
    {
        withinEnemyReach = 1f;
        pathfinder = gameObject.AddComponent<PathfindingController>();
        pathfinder.CreateNodes();
        pathfinder.CreateNodeConnections();
        seekScript = gameObject.AddComponent<seekBehaviour>();
        currentPath = pathfinder.ExecutePathfind();   
    }

   
   

    protected override void CooperativeArbitration()
    {
        base.CooperativeArbitration();

        EnemyAgent closestEnemy = (EnemyAgent)SteeringAgent.GetNearestAgent(transform.position, GameData.Instance.enemies);

        if (pathfinder.currentEnemyTarget.Health > 0)
        {
            //Checks if there is an enemy nearby
            if (Vector3.Distance(transform.position, closestEnemy.transform.position) <= withinEnemyReach)
            {
              seekScript.weight = 0;
              AttackWith(attackType);
            }

            else
            {
                seekScript.weight = 6f;
                seekScript.enabled = true;
            }
        }

        else
        {
            seekScript.currentNode = 0;
            currentPath = pathfinder.ExecutePathfind();
        }
    }

    protected override void UpdateDirection()
    {
        base.UpdateDirection();
    }
}
