using UnityEngine;
using System;

public class PlayerCamera : MonoBehaviour
{
    [SerializeField]private Transform target;
    [SerializeField]private AreaRect movableArea;
    [SerializeField]private float distance = 0;
    [SerializeField]private Vector3 offset;

    private bool isLock;

    void FixedUpdate()
    {
        if(isLock)
            return;

        Vector3 cameraPos = target.position + (-transform.forward) * distance + offset;
        cameraPos = new Vector3(
            Mathf.Clamp(cameraPos.x, movableArea.min.x, movableArea.max.x),
            Mathf.Clamp(cameraPos.y, movableArea.min.y, movableArea.max.y),
            Mathf.Clamp(cameraPos.z, movableArea.min.z, movableArea.max.z)
        );

        transform.position = cameraPos;
    }

    [Serializable]
    public class AreaRect
    {
        public Vector3 min;
        public Vector3 max;
    }
}