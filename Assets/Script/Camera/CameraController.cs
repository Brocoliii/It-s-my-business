using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float investigationZoomSize = 3.5f;
    [SerializeField] private float investigationTransitionSpeed = 10f;

    [Header("Edge Scrolling (ดันขอบจอตอนถือของ)")]
    [SerializeField] private bool useEdgeScrolling = true;
    [SerializeField] private float edgePanSpeed = 15f;
    [SerializeField] private float edgeSize = 40f;

    [Header("Camera Bounds")]
    [SerializeField] private Transform minBoundPoint;
    [SerializeField] private Transform maxBoundPoint;

    private bool isLocked = false;
    private bool isInvestigationBlocked = false;
    private bool isInvestigationFocused = false;
    private bool isReturningFromInvestigation = false;
    private Transform investigationTarget;
    private Vector3 restorePosition;
    private float restoreZoom;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        restorePosition = transform.position;
        restoreZoom = cam.orthographicSize;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        if (isInvestigationFocused || isReturningFromInvestigation)
        {
            UpdateInvestigationFocus();
            return;
        }

        if (isLocked || isInvestigationBlocked || Mouse.current == null) return;

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

    public void SetInvestigationInputBlocked(bool state)
    {
        isInvestigationBlocked = state;
    }

    public void BeginInvestigationFocus(Transform target)
    {
        if (target == null || cam == null) return;

        if (!isInvestigationFocused && !isReturningFromInvestigation)
        {
            restorePosition = transform.position;
            restoreZoom = cam.orthographicSize;
        }

        investigationTarget = target;
        isInvestigationFocused = true;
        isReturningFromInvestigation = false;
    }

    public void EndInvestigationFocus()
    {
        if (cam == null) return;

        if (!isInvestigationFocused && !isReturningFromInvestigation) return;

        isInvestigationFocused = false;
        isReturningFromInvestigation = true;
        investigationTarget = null;
    }

    private void UpdateInvestigationFocus()
    {
        if (cam == null) return;

        if (isInvestigationFocused && investigationTarget == null)
        {
            EndInvestigationFocus();
            return;
        }

        Vector3 targetPosition = isInvestigationFocused
            ? new Vector3(investigationTarget.position.x, investigationTarget.position.y, transform.position.z)
            : restorePosition;

        float targetZoom = isInvestigationFocused ? investigationZoomSize : restoreZoom;

        transform.position = Vector3.Lerp(transform.position, targetPosition, investigationTransitionSpeed * Time.deltaTime);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, investigationTransitionSpeed * Time.deltaTime);

        bool positionReached = Vector3.Distance(transform.position, targetPosition) < 0.01f;
        bool zoomReached = Mathf.Abs(cam.orthographicSize - targetZoom) < 0.01f;

        if (!isInvestigationFocused && positionReached && zoomReached)
        {
            transform.position = restorePosition;
            cam.orthographicSize = restoreZoom;
            isReturningFromInvestigation = false;
        }
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