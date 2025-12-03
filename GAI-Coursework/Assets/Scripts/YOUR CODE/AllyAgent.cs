using System.Collections.Generic;
using UnityEngine;

public class AllyAgent : SteeringAgent
{
	private Attack.AttackType attackType = Attack.AttackType.AllyGun;
	private PathfindingController pathfinder;
	private seperationBehaviour seperationScript;
	private cohesionBehaviour cohesionScript;
	private alignmentBehaviour alignmentScript;
	private seekBehaviour seekScript; 
	public List<Node> currentPath; 
	public float withinEnemyReach;
	private float baseSeperationWeight = 4f; 
	private float baseCohesionWeight = 1f;
	private float baseAlignmentWeight = 1f;
	private float baseSeekWeight = 2f;
	private float maxDistance = 10f;
    protected override void InitialiseFromAwake()
	{
		withinEnemyReach = 10f;
		pathfinder = gameObject.AddComponent<PathfindingController>();
        pathfinder.CreateNodes();
        pathfinder.CreateNodeConnections();
        seperationScript = gameObject.AddComponent<seperationBehaviour>();
		cohesionScript = gameObject.AddComponent<cohesionBehaviour>();
		alignmentScript = gameObject.AddComponent<alignmentBehaviour>();
		seekScript = gameObject.AddComponent<seekBehaviour>();
        currentPath = pathfinder.ExecutePathfind();
    }

	

    protected override void CooperativeArbitration()
	{
		base.CooperativeArbitration();

		EnemyAgent closestEnemy = (EnemyAgent)SteeringAgent.GetNearestAgent(transform.position, GameData.Instance.enemies);

        if (pathfinder.currentEnemyTarget.Health < 0)
        {
            seekScript.currentNode = 0;
            currentPath = pathfinder.ExecutePathfind();
        }

		if (GameData.Instance.Map.GetTerrainAt(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)) == Map.Terrain.Tree)
		{
            //Seek weight is given a high value and all other weights are set to 0 to allow agents to manouver around trees  
			seekScript.weight = 6f;
			seekScript.currentNode = 0;
			currentPath = pathfinder.ExecutePathfind();
			seperationScript.weight = 0;
			alignmentScript.weight = 0;
			cohesionScript.weight = 0;
        }

        else
		{ 
            //All weights are set to their original value to carry out standard flocking 
            seperationScript.weight = baseSeperationWeight;
            alignmentScript.weight = baseAlignmentWeight;
            cohesionScript.weight = baseCohesionWeight;
            seekScript.weight = baseSeekWeight;
        }

        //Checks if there is an enemy nearby
        if (Vector3.Distance(transform.position, closestEnemy.transform.position) <= withinEnemyReach && closestEnemy.Health > 0)
		{
			//All weights are 0 to prioritse attacking over movement 
			seperationScript.weight = 0;
			alignmentScript.weight = 0;
			cohesionScript.weight = 0;
			seekScript.weight = 0;

			//Transform up is set to the opposite of the enemies so that agents turn and face the enemy when attacking 
			transform.up = -closestEnemy.transform.up;
			AttackWith(attackType);
		}

		else if (closestEnemy.Health <= 0)
		{
            seperationScript.weight = baseSeperationWeight;
            alignmentScript.weight = baseAlignmentWeight;
            cohesionScript.weight = baseCohesionWeight;
            seekScript.weight = baseSeekWeight;
        }

		if (seperationScript.weight > 0 && alignmentScript.weight > 0 && cohesionScript.weight > 0 && seekScript.weight > 0)
		{
			//Ensures NaN error never occurs by making sure velocity can only be altered if at least one value is greater than 0
			float sumWeight;

			sumWeight = seperationScript.weight + alignmentScript.weight + cohesionScript.weight + seekScript.weight;

			SteeringVelocity = SteeringVelocity / sumWeight;
		}
    }




	protected override void UpdateDirection()
	{
		//base.UpdateDirection();
	}
}
