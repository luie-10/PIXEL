using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 5.0f;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("Map Boundary Settings (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    private void LateUpdate()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                target = playerObj.transform;
            }
            return;
        }

        Vector3 desiredPosition = target.position + offset;

        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        if (useBounds)
        {
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minBounds.x, maxBounds.x);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothedPosition;
    }
}