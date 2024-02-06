using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    private Vector3 Offset;
    [SerializeField]
    private Transform target;
    [SerializeField]
    private float SmoothTime;
    [SerializeField]
    private float ZoomSpeed;
    private Vector3 CurrentVelocity = Vector3.zero;

    private void Awake()
    {
        Offset = transform.position - target.transform.position;
    }

    private void LateUpdate()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll!= 0f)
        {
            Offset += new Vector3(0, -scroll, scroll) * ZoomSpeed;
        }

        Vector3 targetPosition = target.position + Offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref CurrentVelocity, SmoothTime);


    }
}
