using UnityEngine;
public class seperationBehaviour : SteeringBehaviour
{
    private AllyAgent[] rifleAgents;
    public float weight = 4f;
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var allyAgent = steeringAgent as AllyAgent;

        steeringVelocity = Vector3.zero;

        //Ensures only rifle wielding agents populate this array before looping
        rifleAgents = FindObjectsByType<AllyAgent>(FindObjectsSortMode.None);

        //Loops through rifle wielding agents only so that RPG and melee agents can carry out their own behaviour seperate to the flock
        for (int i =0; i < rifleAgents.Length; i++)
        {
            float distanceToAlly = Vector3.Distance(transform.position, rifleAgents[i].transform.position); 

            if (distanceToAlly > 0 && distanceToAlly < SteeringAgent.CollisionRadius)
            {
                desiredVelocity = Vector3.Normalize(allyAgent.transform.position - rifleAgents[i].transform.position) / distanceToAlly;
                steeringVelocity = desiredVelocity;
            }
        }

        return steeringVelocity * weight;
    } 
}
