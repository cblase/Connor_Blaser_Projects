using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Bezier_Curve : MonoBehaviour
{
    public Transform[] controlPoints;
    //public LineRenderer lineRenderer;

    private int curveCount = 0;
    //private int layerOrder = 0;
    public int SEGMENT_COUNT = 50;


    //void Start()
    //{
    //    if (!lineRenderer)
    //    {
    //        lineRenderer = GetComponent<LineRenderer>();
    //    }
    //    lineRenderer.sortingLayerID = layerOrder;
    //}

    //void Update()
    //{

    //    DrawCurve();

    //}

    void DrawCurve()
    {
        for (int j = 0; j < curveCount; j++)
        {
            for (int i = 1; i <= SEGMENT_COUNT; i++)
            {
                float t = i / (float)SEGMENT_COUNT;
                int nodeIndex = j * 3;
                Vector3 node = CalculateCubicBezierPoint(t, controlPoints[nodeIndex].position, controlPoints[nodeIndex + 1].position, controlPoints[nodeIndex + 2].position, controlPoints[nodeIndex + 3].position);
                //lineRenderer.SetVertexCount(((j * SEGMENT_COUNT) + i));
                //lineRenderer.SetPosition((j * SEGMENT_COUNT) + (i - 1), pixel);
            }

        }
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
        //for (int i = 0; i < numberOfPoints; i++)
        //{
            //t = i / (SEGMENT_COUNT - 1.0f);
            position = (1.0f - t) * (1.0f - t) * p0
            + 2.0f * (1.0f - t) * t * p1 + t * t * p2;
            //lineRenderer.SetPosition(i, position);
        //}
        return position;
    }
}
