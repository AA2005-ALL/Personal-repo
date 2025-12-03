using UnityEngine;

public class cohesionBehaviour : SteeringBehaviour
{
    public int currentNode;
    private int allyCount;
    private Vector3 averageAllyPos = Vector3.zero;
    private AllyAgent[] rifleAgents;
    public float weight = 2f;
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        //Ensures only rifle wielding agents populate this array before looping
        rifleAgents = FindObjectsByType<AllyAgent>(FindObjectsSortMode.None);

        allyCount = 0;

        averageAllyPos = Vector3.zero;

        //Loops through rifle wielding agents only so that RPG and melee agents can carry out their own behaviour seperate to the flock 
        for (int i = 0; i < rifleAgents.Length; i++)
        { 
            float distanceToAlly = Vector3.Distance(transform.position, rifleAgents[i].transform.position);

            if(distanceToAlly < 1)
            {
                averageAllyPos += rifleAgents[i].transform.position;

                allyCount++;
            }
        }

        //Calculates average with all allies close enough to be included in calculations 
        if (allyCount > 0)
        {
            averageAllyPos /= allyCount;

            desiredVelocity = Vector3.Normalize(averageAllyPos - steeringAgent.transform.position) * SteeringAgent.MaxCurrentSpeed;
            steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;
        }

        return steeringVelocity * weight;
    }
}


