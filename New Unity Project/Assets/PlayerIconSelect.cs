using UnityEngine;

public class PlayerIconSelect : MonoBehaviour
{
    public int playerIconNo; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playerIconOne()
    {
        playerIconNo = 0;
    }

    public void playerIconTwo()
    {
        playerIconNo = 1;
    }

    public void playerIconThree()
    {
        playerIconNo = 2;
    }
}
