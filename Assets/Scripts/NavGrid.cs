using System.Collections.Generic;
using UnityEngine;

public class NavGrid : MonoBehaviour
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public float CellSize { get; private set; }
    public int segmentCount = 10;
    public NavGridPathNode[,] Grid;

    [SerializeField]
    private GameObject wallCube;
    [SerializeField]
    private GameObject Player;
    //A* path costs
    private const int diagCost = 14;
    private const int straightCost = 10;

    //Initialize a new grid
    public void GenerateNavGrid(int w, int h, float size)
    {
        if (Grid != null) DestroyAllWalls();

        Width = (w >= 10 && w <= 100) ? w : 50;
        Height = (h >= 10 && h <= 100) ? h : 50;
        CellSize = (size >= 1 && size <= 10) ? size : 1;

        //adjust gridplane to reflect the new grid size
        gameObject.transform.localScale = new Vector3((w / 10f) * CellSize, 1, (h / 10f) * CellSize);
        gameObject.transform.localPosition = new Vector3((w / 2f) * CellSize, 0, (h / 2f) * CellSize);
        //set player in middle of new grid
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
                NavGridPathNode[] A_Path = CreateFinalPath(endNode).ToArray();
                DrawDebugLines(A_Path, Color.black);

                //provide player with smooth path
                Player.GetComponent<Player>().SmoothPath = CreateSmoothPath(A_Path);

                return A_Path;
            }
            SearchList.Remove(current);
            DoneList.Add(current);

            //search neighbors
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
        Debug.Log("no path found!");
        return null;
    }
    //traverses through all the connections to collect the path created with A*
    private List<NavGridPathNode> CreateFinalPath(NavGridPathNode end)
    {
        List<NavGridPathNode> Path = new List<NavGridPathNode>();
        Path.Add(end);
        NavGridPathNode curr = end;
        while (curr.connection != null)
        {
            Path.Add(curr.connection);
            curr = curr.connection;
        }
        Path.Reverse();
        return Path;
    }
    //Create smooth curves along the generated A* path
    private Vector3[] CreateSmoothPath(NavGridPathNode[] A_Path)
    {
        List<Vector3> points = new List<Vector3>();
        Vector3 offSet = new Vector3(CellSize / 2, 0, CellSize / 2);
        //Handles paths too short to be smoothed
        if (A_Path.Length < 3)//3
        {
            foreach (NavGridPathNode node in A_Path)
            {
                points.Add(node.Position + offSet);
            }
            return points.ToArray();
        }

        int nodeIndex = 0;
        Vector3 A, B, C, D;

        while (nodeIndex < A_Path.Length - 1)
        {
            bool Cubic = false;
            for (int i = 1; i <= segmentCount; i++)
            {
                float t = i / (float)segmentCount;
                Vector3 node;
                //Check for enough nodes for a Cubic curve
                if (nodeIndex + 3 < A_Path.Length)
                {
                    A = A_Path[nodeIndex].Position + offSet;
                    B = A_Path[nodeIndex + 1].Position + offSet;
                    C = A_Path[nodeIndex + 2].Position + offSet;
                    D = A_Path[nodeIndex + 3].Position + offSet;

                    //check for straight lines to skip
                    if (checkForStraightLines(A, B, C) && checkForStraightLines(B, C, D))
                    {
                        node = A;
                        points.Add(node);
                        nodeIndex--;
                        break;
                    }
                    node = CalculateCubicBezierPoint(t, A, B, C, D);
                    points.Add(node);
                    Cubic = true;
                }
                //check for enough nodes for a Quadratic curve
                else if (nodeIndex + 2 == A_Path.Length - 1)
                {
                    A = A_Path[nodeIndex].Position + offSet;
                    B = A_Path[nodeIndex + 1].Position + offSet;
                    C = A_Path[nodeIndex + 2].Position + offSet;
                    node = CalculateQuadraticBezierPoint(t, A, B, C);
                    points.Add(node);
                }
                else//only two nodes left
                {
                    points.Add(A_Path[nodeIndex].Position + offSet);
                    points.Add(A_Path[nodeIndex + 1].Position + offSet);
                    nodeIndex--;
                    break;
                }
            }
            //adjust the end of this current curve
            if (Cubic) nodeIndex += 3;
            else nodeIndex += 2;
            //nodeIndex += 2;
        }
        return points.ToArray();
    }
    //Create a Cubic Curve (4 points)
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
    //Create a Quadratic Curve (3 Points)
    Vector3 CalculateQuadraticBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        Vector3 position;
        position = (1.0f - t) * (1.0f - t) * p0
        + 2.0f * (1.0f - t) * t * p1 + t * t * p2;
        return position;
    }

    //Helper Functions---------------------------------------------------------
    bool checkForStraightLines(Vector3 a, Vector3 b, Vector3 c)
    {
        if (((a.z - b.z) * (b.x - c.x)) == ((b.z - c.z) * (a.x - b.x))) return true;
        return false;
    }
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
    //display the generated path
    private void DrawDebugLines(NavGridPathNode[] path, Color c)
    {
        Vector3 cellSizeOffset = new Vector3(CellSize / 2, 0, CellSize / 2);
        for (int i = 0; i < path.Length - 1; i++)
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

    public void CreateMaze()
    {
        int nextChunkRow = 2;
        for (int i = 0; i < Grid.GetLength(0); i++)
        {
            for (int j = 0; j < Grid.GetLength(1); j++)
            {
                float chance = Random.Range(1, 11);
                //top/bottom edges
                if (i == 0 || i == Grid.GetLength(0) - 1)
                {
                    AddWall(ToWorldSpace(i, j));
                }
                //side edges
                else if (j == 0 || j == Grid.GetLength(1) - 1)
                {
                    AddWall(ToWorldSpace(i, j));
                }
                //inner cells from outer walls stay empty
                else if (i == 1 || i == Grid.GetLength(0) - 2)
                {
                    continue;
                }
                else if (j == 1 || j == Grid.GetLength(1) - 2)
                {
                    continue;
                }

                else if (i == nextChunkRow)
                {
                    if (i < Grid.GetLength(0) - 4 && j < Grid.GetLength(1) - 4)// 2< i < rowlength - 2, 2 < j < columnlength - 2
                    {
                        CreateWallChunk(i, j);
                        if (j + 3 < Grid.GetLength(1) - 2)
                        {
                             j += 2;
                        }
                        else if(i + 3 < Grid.GetLength(0) - 2)
                        {
                             nextChunkRow += 3;
                            j = Grid.GetLength(1) - 2;
                        }
                    }
                    else
                    {
                        nextChunkRow += 3;
                        j = Grid.GetLength(1) - 2;
                    }
                }
            }
        }
    }

    private void CreateWallChunk(int x, int y)
    {
        //List<NavGridPathNode> block = new List<NavGridPathNode>();
        NavGridPathNode TL = Grid[x, y + 2];
        NavGridPathNode TM = Grid[x + 1, y + 2];
        NavGridPathNode TR = Grid[x + 2, y + 2];
        NavGridPathNode L = Grid[x, y + 1];
        NavGridPathNode M = Grid[x + 1, y + 1];
        NavGridPathNode R = Grid[x+2, y + 1];
        NavGridPathNode BL = Grid[x, y];
        NavGridPathNode BM = Grid[x + 1, y];
        NavGridPathNode BR = Grid[x + 2, y];

        int randChunk = Random.Range(1, 7);
        switch (randChunk)
        {
            case 1://horizontal line in middle
                AddWall(L.Position);
                AddWall(M.Position);
                AddWall(R.Position);
                break;
            case 2: //vertical line in middle
                AddWall(TM.Position);
                AddWall(M.Position);
                AddWall(BM.Position);
                break;
            case 3://Top-Right L
                AddWall(TM.Position);
                AddWall(M.Position);
                AddWall(R.Position);
                break;
            case 4://Top-left L
                AddWall(TM.Position);
                AddWall(M.Position);
                AddWall(L.Position);
                break;
            case 5://bottom - right L
                AddWall(BM.Position);
                AddWall(M.Position);
                AddWall(R.Position);
                break;
            case 6://bottom - left L
                AddWall(BM.Position);
                AddWall(M.Position);
                AddWall(L.Position);
                break;
        }
    }
}