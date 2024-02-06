using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public float CellSize { get; private set; }
    public int segmentCount = 2;
    public NavGridPathNode[,] Grid;
    [SerializeField]
    private GameObject wallCube;
    [SerializeField]
    private GameObject Player;
    private const int diagCost = 14;
    private const int straightCost = 10;
    public void GenerateNavGrid(int w, int h, float size)
    {
        if (Grid != null) DestroyAllWalls();

        Width = (w >= 2 && w <= 100) ? w : 50;
        Height = (h >= 2 && h <= 100) ? h : 50;
        CellSize = (size >= 1 && size <= 10) ? size : 1;

        gameObject.transform.localScale = new Vector3((w / 10f) * CellSize, 1, (h / 10f) * CellSize);
        gameObject.transform.localPosition = new Vector3((w / 2f) * CellSize, 0, (h / 2f) * CellSize);

        Player.transform.localPosition = new Vector3((w / 2f) * CellSize, 1, (h / 2f) * CellSize);


        Grid = new NavGridPathNode[Width, Height];

        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                Grid[x, y] = new NavGridPathNode(x, y, size);
            }
        }
    }

    //returns an array of Grid nodes to the player
    public NavGridPathNode[] GetPath(Vector3 origin, Vector3 destination)
    {
        //Debug.Log("begin the path search!");
        int startX, startY, desX, desY;
        List<NavGridPathNode> SearchList;
        List<NavGridPathNode> DoneList;

        ToGridSpace(origin, out startX, out startY);
        ToGridSpace(destination, out desX, out desY);

        //add starting node first
        SearchList = new List<NavGridPathNode> { Grid[startX, startY] };
        DoneList = new List<NavGridPathNode>();

        NavGridPathNode startNode = Grid[startX, startY];
        NavGridPathNode endNode = Grid[desX, desY];
        //Initialize Grid for creating a path
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                NavGridPathNode node = Grid[x, y];
                node.SetGCost(int.MaxValue);
                node.connection = null;
            }
        }

        //set initial GCost and HCost for the starting node
        startNode.SetGCost(0);
        startNode.SetHCost(CalcCost(startNode, endNode));

        //begin the path search
        while (SearchList.Count > 0)
        {
            NavGridPathNode current = FindLowestFCostNode(SearchList);
            if (current == endNode)//found the end point
            {
                //Debug.Log("found shortest path!");
                NavGridPathNode[] A_Path = CreateFinalPath(endNode).ToArray();
                DrawDebugLines(A_Path, Color.black);
                Vector3[] temp = CreateSmoothPath(A_Path);
                Player.GetComponent<Player>().SmoothPath = temp;
                Debug.Log("path length: " + A_Path.Length);
                return A_Path;
                //return CreateFinalPath(endNode).ToArray();
            }
            SearchList.Remove(current);
            DoneList.Add(current);

            //searchNeighbors
            foreach (NavGridPathNode node in FindNeighbors(current))
            {
                if (DoneList.Contains(node)) continue;//skip used nodes
                //remove neighbors that are walls
                if (node.Wall != null)
                {
                    DoneList.Add(node);
                    continue;
                }
                //calculate cost to neighbor
                int tempGCost = current.gCost + CalcCost(current, node);
                if (tempGCost < node.gCost)
                {
                    node.connection = current;
                    node.SetGCost(tempGCost);
                    node.SetHCost(CalcCost(node, endNode));
                    //add this node to the search list 
                    if (!SearchList.Contains(node))
                    {
                        SearchList.Add(node);
                    }
                }
            }
        }
        //no path found
        Debug.Log("no path found!");
        return null;
    }

    private List<NavGridPathNode> CreateFinalPath(NavGridPathNode end)
    {
        List<NavGridPathNode> Path = new List<NavGridPathNode>();
        Path.Add(end);
        //Debug.LogFormat("last node: {0}", end.Position);
        NavGridPathNode curr = end;
        while (curr.connection != null)
        {
            Path.Add(curr.connection);
            //Debug.LogFormat("next node: {0}", curr.connection.Position);
            curr = curr.connection;
        }
        Path.Reverse();
        return Path;
    }

    private Vector3[] CreateSmoothPath(NavGridPathNode[] A_Path)
    {
        List<Vector3> points = new List<Vector3>();
        //Handles paths too short to be smoothed
        if(A_Path.Length < 3)
        {
            foreach(NavGridPathNode node in A_Path)
            {
                points.Add(node.Position);
            }
            return points.ToArray();
        }

        Vector3 offSet = new Vector3(CellSize / 2, 0, CellSize / 2);
        
        int nodeIndex = 0; //j * 3;
        while (nodeIndex < A_Path.Length - 1)//for (int j = 0; j < curveCount; j++)
        {
            //Debug.LogFormat("nodeIndex: {0}, Path Length: {1}, Cubic Time: {2}", nodeIndex, A_Path.Length,(nodeIndex + 3 == A_Path.Length - 1));
            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 node;

                if (nodeIndex + 3 == A_Path.Length - 1)
                {
                    node = CalculateCubicBezierPoint(t, A_Path[nodeIndex].Position + offSet, A_Path[nodeIndex + 1].Position + offSet, A_Path[nodeIndex + 2].Position + offSet, A_Path[nodeIndex + 3].Position + offSet);
                    points.Add(node);
                }
                else if (checkForStraightLines(A_Path[nodeIndex].Position + offSet, A_Path[nodeIndex+1].Position + offSet, A_Path[nodeIndex + 2].Position + offSet))
                {
                    node = A_Path[nodeIndex].Position + offSet;
                    points.Add(node);
                    nodeIndex--;
                    break;
                }
                else
                {
                    node = CalculateQuadraticBezierPoint(t, A_Path[nodeIndex].Position + offSet, A_Path[nodeIndex + 1].Position + offSet, A_Path[nodeIndex + 2].Position + offSet);
                    points.Add(node);
                }
            }
            if (nodeIndex + 3 == A_Path.Length - 1) nodeIndex += 3;
            else nodeIndex += 2;
            //nodeIndex++;
        }
        return points.ToArray();
    }

    bool checkForStraightLines(Vector3 a, Vector3 b, Vector3 c)
    {
        if (((a.z - b.z) * (b.x - c.x)) == ((b.z - c.z) * (a.x - b.x))) return true;
        return false;
        ////straight line up
        //if ((a.x - b.x) == 0 && (b.x - c.x) == 0) return true;
        ////stright line horizontal
        //if ((a.z - b.z) == 0 && (b.z - c.z) == 0) return true;
        ////Diagonal straight line
        //if (a.x - b.x != 0 && a.z - b.z != 0 && b.x - c.x != 0 && b.z - c.z != 0) return true;

        //return false;
    }
    Vector3 CalculateCubicBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 position = uuu * p0;
        position += 3 * uu * t * p1;
        position += 3 * u * tt * p2;
        position += ttt * p3;

        return position;
    }

    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 position;
        position = (1.0f - t) * (1.0f - t) * p0
        + 2.0f * (1.0f - t) * t * p1 + t * t * p2;
        return position;
    }

    //Helper Functions---------------------------------------------------------

    public Vector3 ToWorldSpace(int x, int y)
    {
        return new Vector3(x, 0, y) * CellSize;
    }

    public void ToGridSpace(Vector3 vector3, out int x, out int y)
    {
        x = Mathf.FloorToInt(vector3.x / CellSize);
        y = Mathf.FloorToInt(vector3.z / CellSize);
    }

    //find all valid neighbors of a given node
    private List<NavGridPathNode> FindNeighbors(NavGridPathNode current)
    {
        List<NavGridPathNode> neighbors = new List<NavGridPathNode>();
        //Neighbor Key
        bool left = current.X - 1 >= 0;
        bool right = current.X + 1 < Grid.GetLength(0);
        bool top = current.Z + 1 < Grid.GetLength(1);
        bool bottom = current.Z - 1 >= 0;
        bool topLeft = left && top;
        bool bottomLeft = left && bottom;
        bool topRight = right && top;
        bool bottomRight = right && bottom;

        //only adds corner neighbors if top/sides/bottom are not walls
        //Left side
        if (left) neighbors.Add(Grid[current.X - 1, current.Z]);
        //Bottom Left
        if (bottomLeft && Grid[current.X - 1, current.Z].Wall == null && Grid[current.X, current.Z - 1].Wall == null)
            neighbors.Add(Grid[current.X - 1, current.Z - 1]);
        //Top Left
        if (topLeft && Grid[current.X - 1, current.Z].Wall == null && Grid[current.X, current.Z + 1].Wall == null)
            neighbors.Add(Grid[current.X - 1, current.Z + 1]);
        //Right side
        if (right) neighbors.Add(Grid[current.X + 1, current.Z]);
        //Bottom Right
        if (bottomRight && Grid[current.X + 1, current.Z].Wall == null && Grid[current.X, current.Z - 1].Wall == null)
            neighbors.Add(Grid[current.X + 1, current.Z - 1]);
        //Top Right
        if (topRight && Grid[current.X + 1, current.Z].Wall == null && Grid[current.X, current.Z + 1].Wall == null)
            neighbors.Add(Grid[current.X + 1, current.Z + 1]);
        //Top
        if (top) neighbors.Add(Grid[current.X, current.Z + 1]);
        //Bottom
        if (bottom) neighbors.Add(Grid[current.X, current.Z - 1]);

        return neighbors;
    }
    // search for lowest fCost node in the search list
    private NavGridPathNode FindLowestFCostNode(List<NavGridPathNode> list)
    {
        NavGridPathNode lowestFCostNode = list[0];
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].fCost < lowestFCostNode.fCost)
            {
                lowestFCostNode = list[i];
            }
        }
        return lowestFCostNode;
    }

    //calculate the Cost of a given node to another
    private int CalcCost(NavGridPathNode curr, NavGridPathNode end)
    {
        int distanceX = Mathf.Abs(curr.X - end.X);
        int distanceY = Mathf.Abs(curr.Z - end.Z);
        int remainDist = Mathf.Abs(distanceX - distanceY);

        return diagCost * Mathf.Min(distanceX, distanceY) + straightCost * remainDist;
    }

    private void DrawDebugLines(NavGridPathNode[] path, Color c)
    {
        Vector3 cellSizeOffset = new Vector3(CellSize / 2, 0, CellSize / 2);
        for(int i = 0; i < path.Length - 1; i++)
        {
            Debug.DrawLine(path[i].Position + cellSizeOffset, path[i + 1].Position + cellSizeOffset, c, 10f);
        }
    }

    //Wall Management functions----------------------------------------------------------------
    public void AddWall(Vector3 location)
    {
        int x, y;
        ToGridSpace(location, out x, out y);
        if (Grid[x, y].Wall == null)
        {
            Vector3 cellCoords = Grid[x, y].Position;
            cellCoords += new Vector3(CellSize / 2, 0, CellSize / 2);
            var wall = Instantiate(wallCube, cellCoords, Quaternion.identity);
            wall.transform.localScale = new Vector3(CellSize, 1, CellSize);
            Grid[x, y].Wall = wall;
        }
        else
        {
            DestroyWall(location);
        }
    }

    public void DestroyWall(Vector3 location)
    {
        int x, y;
        ToGridSpace(location, out x, out y);
        if (Grid[x, y].Wall != null)
        {
            Destroy(Grid[x, y].Wall);
            Grid[x, y].Wall = null;
        }
        else
        {
            AddWall(location);
        }
    }

    public void DestroyAllWalls()
    {
        for (int x = 0; x < Grid.GetLength(0); x++)
        {
            for (int y = 0; y < Grid.GetLength(1); y++)
            {
                if (Grid[x, y].Wall != null)
                {
                    Destroy(Grid[x, y].Wall);
                    Grid[x, y].Wall = null;
                }
            }
        }
    }
}
