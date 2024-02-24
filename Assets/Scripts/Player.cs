using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Player : MonoBehaviour
{
    private NavGridPathNode[] CurrentPath = Array.Empty<NavGridPathNode>();
    private int CurrentPathIndex = 1;
    [SerializeField]
    public NavGrid Grid;
    public Vector3[] SmoothPath, aPath;
    [SerializeField]
    private float Speed = 10.0f;
    [SerializeField]
    private float rotationSpeed = 0.75f;

    //apply offset to path for player to follow
    void extractPath(NavGridPathNode[] path)
    {
        Vector3 offSet = new Vector3(Grid.CellSize / 2, 0, Grid.CellSize / 2);
        List<Vector3> newPath = new List<Vector3>();
        foreach(NavGridPathNode node in path)
        {
            newPath.Add(node.Position + offSet);
        }
        aPath = newPath.ToArray();
    }

    void Update()
    {
        // Check Input
        if (Input.GetMouseButtonUp(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
            {
                SmoothPath = Array.Empty<Vector3>();
                CurrentPath = Grid.GetPath(transform.position, hitInfo.point);
                if (CurrentPath != null)
                {
                    extractPath(CurrentPath);
                }
                CurrentPathIndex = 0;
            }
        }
        //Spawn/despawn wall blocks
        if (Input.GetMouseButtonUp(1))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hitInfo))
                Grid.AddWall(hitInfo.point);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Grid.CreateMaze();
        }

        // Traverse
        if(CurrentPathIndex < SmoothPath.Length)
        {
            //show smoothed path
            for (int i = 0; i < SmoothPath.Length - 1; i++)
            {
                Debug.DrawLine(SmoothPath[i], SmoothPath[i + 1], Color.blue);
            }

            Vector3 currentNode = SmoothPath[CurrentPathIndex];
            var vectorToDestination = currentNode - transform.position;
            vectorToDestination.y = 0f;
            var maxDistance = Speed * Time.deltaTime;
            var moveDistance = Mathf.Min(vectorToDestination.magnitude, maxDistance);
            var moveVector = vectorToDestination.normalized * moveDistance;
            moveVector.y = 0f; // Ignore Y
            Quaternion rotgoal = Quaternion.LookRotation(vectorToDestination.normalized);
            transform.position += moveVector;
            transform.rotation = Quaternion.Slerp(transform.rotation, rotgoal, rotationSpeed);
            
            if (vectorToDestination.magnitude < 0.6f)
            {
                CurrentPathIndex++;
            }
        }
    }
}
