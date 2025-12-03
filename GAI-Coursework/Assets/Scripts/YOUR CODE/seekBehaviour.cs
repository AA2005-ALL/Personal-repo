using UnityEngine;
using System.Collections.Generic;

public class seekBehaviour : SteeringBehaviour
{
    public int currentNode;
    private List<Node> currentPath;
    public float weight = 6f; 
   
    public override Vector3 UpdateBehaviour(SteeringAgent steeringAgent)
    {
        var meleeAgent = steeringAgent as MeleeAgent;

        var rpgAgent = steeringAgent as RPGAgent;

        var allyAgent = steeringAgent as AllyAgent; 

        if(steeringAgent == meleeAgent)
        {
            currentPath = meleeAgent.currentPath;
        }

        else if (steeringAgent == allyAgent)
        {
            currentPath = allyAgent.currentPath;
        }

        else if(steeringAgent == rpgAgent)
        {
            currentPath = rpgAgent.currentPath;
        }

        if(currentNode >= currentPath.Count)
        {
            currentNode = 0;
            Debug.LogError("Current path not valid");
        }

        Vector3 targetPos = new Vector3(currentPath[currentNode].x, currentPath[currentNode].y, 0);

        if (Vector3.Distance(transform.position, targetPos) <= 0.25f)
        {
            //Ensures logic is never applied if we're outside the bounds of current path
            if (currentNode < currentPath.Count - 1)
            {
                currentNode++;

                targetPos = new Vector3(currentPath[currentNode].x, currentPath[currentNode].y, 0);
            }

            else
            {
                return Vector3.zero;
            }

        }


        desiredVelocity = Vector3.Normalize(targetPos - steeringAgent.transform.position) * SteeringAgent.MaxCurrentSpeed;
        steeringVelocity = desiredVelocity - steeringAgent.CurrentVelocity;

       
        return steeringVelocity * weight;
    }
}
