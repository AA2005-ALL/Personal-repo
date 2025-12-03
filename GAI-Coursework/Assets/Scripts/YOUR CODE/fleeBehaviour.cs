using UnityEngine;

public class fleeBehaviour : SteeringBehaviour
{
    private int currentNode;
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var meleeAgent = steeringAgent as MeleeAgent;

        Vector3 targetPos = new Vector3(meleeAgent.currentPath[currentNode].x, meleeAgent.currentPath[currentNode].y, 0);

        if (Vector3.Distance(transform.position, targetPos) <= 0.5f)
        {
            //Ensures logic is never applied if we're outside the bounds of current path
            if (currentNode < meleeAgent.currentPath.Count - 1)
            {
                currentNode++;

                targetPos = new Vector3(meleeAgent.currentPath[currentNode].x, meleeAgent.currentPath[currentNode].y, 0);
            }

            else
            {
                //Debug.Log("Reached the end of the current path");
                return Vector3.zero;
            }

        }

        //Debug.Log(targetPos + " outside of loop");



        desiredVelocity = Vector3.Normalize(targetPos - steeringAgent.transform.position) * SteeringAgent.MaxCurrentSpeed;
        steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;


        return -steeringVelocity;
    }
}
