using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float panSpeed = 0.5f;

    [Header("Edge Scrolling (ดันขอบจอตอนถือของ)")]
    [SerializeField] private bool useEdgeScrolling = true;
    [SerializeField] private float edgePanSpeed = 15f; 
    [SerializeField] private float edgeSize = 40f;    

    [Header("Camera Bounds")]
    [SerializeField] private Transform minBoundPoint;
    [SerializeField] private Transform maxBoundPoint;

    private bool isLocked = false;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (isLocked || Mouse.current == null) return;

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 dragDelta = Mouse.current.delta.ReadValue();
            Vector3 moveDelta = new Vector3(-dragDelta.x * panSpeed, -dragDelta.y * panSpeed, 0);
            ApplyCameraMovement(moveDelta * Time.deltaTime);
        }
        else if (useEdgeScrolling && Mouse.current.leftButton.isPressed)
        {
            HandleEdgeScrolling();
        }
    }

    private void HandleEdgeScrolling()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;

        if (mousePos.x <= edgeSize) moveDirection.x = -1;
        else if (mousePos.x >= Screen.width - edgeSize) moveDirection.x = 1; 

        if (mousePos.y <= edgeSize) moveDirection.y = -1; 
        else if (mousePos.y >= Screen.height - edgeSize) moveDirection.y = 1; 

        if (moveDirection != Vector3.zero)
        {
            ApplyCameraMovement(moveDirection * edgePanSpeed * Time.deltaTime);
        }
    }

    public void SetCameraLock(bool state)
    {
        isLocked = state;
    }

    private void ApplyCameraMovement(Vector3 moveAmount)
    {
        if (minBoundPoint == null || maxBoundPoint == null) return;

        Vector3 pos = transform.position;
        pos += moveAmount;

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