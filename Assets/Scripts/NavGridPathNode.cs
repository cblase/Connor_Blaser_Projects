using UnityEngine;

public class NavGridPathNode
{
    public Vector3 Position;
    public int X, Z;
    public float CellSize;
    public NavGridPathNode connection;
    public GameObject Wall;
    public int gCost { get; private set; }
    public int hCost { get; private set; }

    public int fCost => gCost + hCost;
    
    public NavGridPathNode(int x, int y, float size)
    {
        X = x;
        Z = y;
        CellSize = size;
        Position = new Vector3(X*CellSize, 0.5f, Z*CellSize);
        gCost = int.MaxValue;
    }
    public void SetGCost(int cost) => gCost = cost;

    public int SetHCost(int cost) => hCost = cost;

    public void SetConnection(NavGridPathNode node) => connection = node;
}