using System.Collections.Generic;
using UnityEngine;
public class PathfinderBase : MonoBehaviour 
{
    protected int NodesWidth = 100;
    protected int NodesHeight = 100;

    [SerializeField]
    protected int movementXCost = 10;

    [SerializeField]
    protected int movementYCost = 10;

    [SerializeField]
    protected int movementDiagonalCost = 14;

    protected int movementDiagonalMinusXY;

    protected Node[] nodes;



    private void Start()
    {
      GridRenderer gridRenderer = gameObject.AddComponent<GridRenderer>();
    }
    public void CreateNodes()
    { 
        nodes = new Node[NodesWidth * NodesHeight];

        //Done to make all nodes empty at the beginning 
        for (int y = 0; y < NodesHeight; ++y)
        {
            for (int x = 0; x < NodesWidth; ++x)
            {
                Node node = new Node();
                node.x = x;
                node.y = y;
                nodes[x + (NodesWidth * y)] = node;
            }
        }
    }

    public void CreateNodeConnections()
    {

        for (int nodeY = 0; nodeY < NodesHeight; ++nodeY)
        {
            for (int nodeX = 0; nodeX < NodesWidth; ++nodeX)
            {
                int nodeIndex = nodeX + (NodesWidth * nodeY);
                Node node = nodes[nodeIndex];

                //Simple check to tell my algorithm to ignore trees
                if (GameData.Instance.Map.IsNavigatable(nodeX, nodeY) == false)
                {
                    node.neighbours = new Node[0];
                    node.neighbourCosts = new int[0];

                    //Skipping to ensure trees aren't counted as neighbours 
                    continue;
                }

                int connectedNodesCount = 0;
                for (int neighbourY = nodeY - 1; neighbourY <= nodeY + 1; neighbourY++)
                {

                    if (neighbourY < 0 || neighbourY >= NodesHeight)
                    {
                        continue;
                    }

                    for (int neighbourX = nodeX - 1; neighbourX <= nodeX + 1; neighbourX++)
                    {
                        node.g = 15;
                        
                        // Simple check to tell my algorithm to ignore trees
                        if (neighbourX < 0 || neighbourX >= NodesWidth || (neighbourX == nodeX && neighbourY == nodeY) || !GameData.Instance.Map.IsNavigatable(neighbourX, neighbourY))
                        {
                            //Skipping to ensure trees aren't counted as neighbours  
                            continue;
                        }

                        ++connectedNodesCount;
                    }
                }

                node.neighbours = new Node[connectedNodesCount];
             
                node.neighbourCosts = new int[connectedNodesCount];
     
                int connectedNodesIndex = 0;

                for (int neighbourY = nodeY - 1; neighbourY <= nodeY + 1; ++neighbourY)
                {
                    if (neighbourY < 0 || neighbourY >= NodesHeight)
                    {
                        continue;
                    }

                    for (int neighbourX = nodeX - 1; neighbourX <= nodeX + 1; ++neighbourX)
                    {
                        node.g = 15;

                        if (neighbourX < 0 || neighbourX >= NodesWidth || (neighbourX == nodeX && neighbourY == nodeY) || !GameData.Instance.Map.IsNavigatable(neighbourX, neighbourY))
                        {
                            continue;
                        }

                        node.neighbours[connectedNodesIndex] = nodes[neighbourX + (neighbourY * NodesWidth)];
                    
                        node.neighbourCosts[connectedNodesIndex] = CalculateInitialCost(nodeX, nodeY, neighbourX, neighbourY);
    
                        ++connectedNodesIndex;
                    }
                }

                //Safety check for null neighbours
                foreach (var neighbour in node.neighbours)
                {
                    if(neighbour == null)
                    {
                        Debug.Log("This neighbour is null: " + neighbour.x + " " + neighbour.y);
                    }
                }

            }
        }
    }

    protected int CalculateInitialCost(int firstNodeX, int firstNodeY, int secondNodeX, int secondNodeY)
    {
        int xCost = Mathf.Abs(secondNodeX - firstNodeX);
        int yCost = Mathf.Abs(secondNodeY - firstNodeY);
        if ((xCost + yCost) < 2)
        {
            if (xCost > 0)
            {
                return movementXCost;
            }
            return movementYCost;
        }
        return movementDiagonalCost;
    }

    protected List<Node> GetFoundPath(Node endNode)
    {
        List<Node> foundPath = new List<Node>();
        if (endNode != null)
        {
            foundPath.Add(endNode);

            while (endNode.parent != null)
            {
                foundPath.Add(endNode.parent);
                endNode = endNode.parent;
            }

            // Reverse the path so the start node is at index 0
            foundPath.Reverse();
        }

        if(foundPath.Count == 0)
        {
            Debug.LogWarning("path is 0 length");
        }

        return foundPath;
    }
}

