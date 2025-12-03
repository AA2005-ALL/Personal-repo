using UnityEngine;
using System.Collections.Generic;
public class PathfindingController : PathfinderBase
{
    public int StartNodeX;
    public int StartNodeY;
    public int EndNodeX;
    public int EndNodeY;
    public EnemyAgent currentEnemyTarget;
    private float currentFurthestEnemy; 

    public List<Node> ExecutePathfind()
    {
        movementDiagonalMinusXY = movementDiagonalCost - (movementXCost + movementYCost);

        //Simple check to ensure no null .neighbours exist 
        foreach(var node in nodes)
        {
            if(GameData.Instance.Map.GetTerrainAt(node.x, node.y) == Map.Terrain.Tree)
            {
                continue;
            }
            
            bool allNeigboursNull = true;

            foreach(var neighbour in node.neighbours)
            {
                if(neighbour != null)
                {
                    allNeigboursNull = false;
                    break;
                }
            }

            if(allNeigboursNull)
            {
                Debug.Log("WTF");
            }
        }

        //Clears any temporary data the nodes have from any previous execution of the algorithm
        foreach (var node in nodes)
        {
            //Calls the reset function within node 
            node.Reset();
        }

        //Initial enemy target
        currentEnemyTarget = FindAnyObjectByType<EnemyAgent>();
        

      

        //Because the nodes array stores 0-9999 to represent 10000 NodesWidth and NodesHeight has to be subtracted by 1 to ensure the array is never out of range  
        StartNodeX = Mathf.Clamp(Mathf.FloorToInt(transform.position.x), 0, NodesWidth - 1);
        StartNodeY = Mathf.Clamp(Mathf.FloorToInt(transform.position.y), 0, NodesHeight - 1);


        
        if(currentEnemyTarget.Health < 0 && transform.GetComponent<MeleeAgent>() == null)
        {
           currentEnemyTarget = (EnemyAgent)SteeringAgent.GetNearestAgent(transform.position, GameData.Instance.enemies); 
        }

        if (currentEnemyTarget.Health < 0 && transform.parent.GetComponent<MeleeAgent>() != null)
        {
            for (int i = 0; i < GameData.Instance.enemies.Count; i++)
            {
                if (Vector3.Distance(transform.position, GameData.Instance.enemies[i].transform.position) > Vector3.Distance(transform.position, EnemyAgent.GetNearestAgent(transform.position, GameData.Instance.enemies).transform.position))
                {
                    currentFurthestEnemy = Vector3.Distance(transform.position, GameData.Instance.enemies[i].transform.position);
                }

                if (Vector3.Distance(transform.position, GameData.Instance.enemies[i].transform.position) == currentFurthestEnemy)
                {
                    currentEnemyTarget = (EnemyAgent)GameData.Instance.enemies[i];
                }
            }
        }

        EndNodeX = Mathf.Clamp(Mathf.FloorToInt(currentEnemyTarget.transform.position.x), 0, NodesWidth - 1);
        EndNodeY = Mathf.Clamp(Mathf.FloorToInt(currentEnemyTarget.transform.position.y), 0, NodesHeight - 1);
 

        //Sets start and end nodes
        Node startNode = nodes[StartNodeX + (NodesWidth * StartNodeY)];
        Node endNode = nodes[EndNodeX + (NodesWidth * EndNodeY)];

        return AStar(startNode, endNode);
    }


    public List<Node> AStar(Node startNode, Node endNode)
    {
        startNode.onOpenList = true;

        List<Node> openList = new List<Node>();
        List<Node> closedList = new List<Node>();

        openList.Add(startNode);

        while(openList.Count > 0)
        {
            openList.Sort((a, b) => a.f.CompareTo(b.f));

            Node currentNode = openList[0];
            openList.RemoveAt(0);

            currentNode.onClosedList = true;
            closedList.Add(currentNode);

            if (currentNode == endNode)
            { 
                return GetFoundPath(endNode);
            }

            if (currentNode.neighbours != null)
            {
                Node[] neighbours = currentNode.neighbours;
                
                int neighboursCount = neighbours.Length;
                
                for (int neighbourIndex = 0; neighbourIndex < neighboursCount; neighbourIndex++)
                {
                    if(neighbours.Length <= 0)
                    {
                        Debug.LogError("neighbours is 0");
                    }
                    
                    Node currentNeighbour = neighbours[neighbourIndex];

                    //Ignores all neighbours on the closed list 
                    if (currentNeighbour.onClosedList)
                    {
                        continue;
                    }

                    //Calculates the cost to each neighbouring
                    float g = currentNode.g + currentNode.neighbourCosts[neighbourIndex];

                    float h = EuclideanDistanceHeuristic(endNode.x, endNode.y, currentNeighbour.x, currentNeighbour.y);

                    float f = g + h;

                    
                    if (f <= currentNeighbour.f || !currentNeighbour.onOpenList)
                    {
                        currentNeighbour.g = g;

                        currentNeighbour.h = h;

                        currentNeighbour.f = g + h;

                        currentNeighbour.parent = currentNode;
                    }

                    if (!currentNeighbour.onOpenList)
                    {
                        currentNeighbour.onOpenList = true;


                        openList.Add(currentNeighbour);
                    }
                }
            }

            else
            {
                Debug.LogError("Current node has no neighbours");
            }
        }

        // No path has been found
        return GetFoundPath(null);
    }

    //Called each time A* algorithm is used to save writing the full equation everytime 
    private float EuclideanDistanceHeuristic(float currentX, float currentY, float targetX, float targetY)
    {
        float dx = targetX - currentX;
        float dy = targetY - currentY;

        return Mathf.Sqrt(dx * dx + dy * dy) * movementXCost;
    }
}
