using System;
public class Node : IComparable<Node>
{
    public int x;
    
    public int y;

    public Node parent;

    public Node[] neighbours;

    public int[] neighbourCosts;

    public float f;

    public float g;

  
    public float h;

    public bool onOpenList = false;

    public bool onClosedList = false;

    public int CompareTo(Node otherNode)
    {
        if (f < otherNode.f)
        {
            return -1;
        }
        else if (f > otherNode.f)
        {
            return 1;
        }

        return 0;
    }

    public void Reset()
    {
        parent = null;
        f = 0;
        g = 0;
        h = 0;
        onOpenList = false;
        onClosedList = false;
    }
}
