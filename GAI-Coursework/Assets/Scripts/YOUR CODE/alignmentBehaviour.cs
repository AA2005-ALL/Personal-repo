using UnityEngine;

public class alignmentBehaviour : SteeringBehaviour 
{
    private int allyCount;
    private Vector3 allyPos = Vector3.zero;
    private AllyAgent[] rifleAgents;
    public float weight = 2f; 
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var allyAgent = steeringAgent as AllyAgent;

        allyCount = 0;

        allyPos = Vector3.zero;

        //Ensures only rifle wielding agents populate this array before looping
        rifleAgents = FindObjectsByType<AllyAgent>(FindObjectsSortMode.None);

        //Intentionally discluding the z axis of steering velocity as that was previously causing visual bugs by rotating agents off screen
        transform.up = new Vector3(steeringVelocity.x, steeringVelocity.y, 0);

        //Loops through rifle wielding agents only so that RPG and melee agents can carry out their own behaviour seperate to the flock 
        for (int i = 0; i < rifleAgents.Length; i++)
        {
            float distanceToAlly = Vector3.Distance(transform.position, rifleAgents[i].transform.position);

            if (distanceToAlly < 1)
            {
                //Using transform up instead of forward as when using forward allies would clip out of vision 
                allyPos += rifleAgents[i].transform.up;

                //Adding each allies velocity to math speeds as well as direction
                allyPos += rifleAgents[i].CurrentVelocity;

                allyCount++;
            }
        }

        if (allyCount > 0)
        {
            allyPos /= allyCount;
            desiredVelocity = allyPos; 
            steeringVelocity = Vector3.Normalize(desiredVelocity - allyAgent.transform.up);
        }

        return steeringVelocity * weight;
    }
}
