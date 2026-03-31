using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float panSpeed = 0.5f; 

    [Header("Camera Bounds")]
    [SerializeField] private Transform minBoundPoint;
    [SerializeField] private Transform maxBoundPoint;

    private bool isLocked = false;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    public void SetCameraLock(bool state)
    {
        isLocked = state;
    }

    public void PanCamera(Vector2 dragDelta)
    {
        if (isLocked || minBoundPoint == null || maxBoundPoint == null) return;

        Vector3 pos = transform.position;

        pos.x -= dragDelta.x * panSpeed * Time.deltaTime;
        pos.y -= dragDelta.y * panSpeed * Time.deltaTime;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = cam.aspect * camHalfHeight;

        float clampMinX = minBoundPoint.position.x + camHalfWidth;
        float clampMaxX = maxBoundPoint.position.x - camHalfWidth;
        float clampMinY = minBoundPoint.position.y + camHalfHeight;
        float clampMaxY = maxBoundPoint.position.y - camHalfHeight;

        if (clampMinX > clampMaxX) clampMinX = clampMaxX = (minBoundPoint.position.x + maxBoundPoint.position.x) / 2f;
        if (clampMinY > clampMaxY) clampMinY = clampMaxY = (minBoundPoint.position.y + maxBoundPoint.position.y) / 2f;

        pos.x = Mathf.Clamp(pos.x, clampMinX, clampMaxX);
        pos.y = Mathf.Clamp(pos.y, clampMinY, clampMaxY);

        transform.position = pos;
    }

    private void OnDrawGizmos()
    {
        if (minBoundPoint != null && maxBoundPoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 minPos = minBoundPoint.position;
            Vector3 maxPos = maxBoundPoint.position;

            Gizmos.DrawLine(new Vector3(minPos.x, minPos.y, 0), new Vector3(maxPos.x, minPos.y, 0));
            Gizmos.DrawLine(new Vector3(maxPos.x, minPos.y, 0), new Vector3(maxPos.x, maxPos.y, 0));
            Gizmos.DrawLine(new Vector3(maxPos.x, maxPos.y, 0), new Vector3(minPos.x, maxPos.y, 0));
            Gizmos.DrawLine(new Vector3(minPos.x, maxPos.y, 0), new Vector3(minPos.x, minPos.y, 0));
        }
    }
}