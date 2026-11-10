using UnityEngine;

public class CatTreat : MonoBehaviour, IDraggable
{
    [SerializeField] private string defaultLayer = "Default";
    [SerializeField] private string dragLayer = "Dragging";
    [SerializeField] private string attachedLayer = "Dragging";

    [Header("Drag Sway")]
    [SerializeField] private bool enableDragSway = true;
    [SerializeField] private float swayAnglePerSpeed = 2.2f;
    [SerializeField] private float swayMaxAngle = 16f;
    [SerializeField] private float swayStiffness = 90f;
    [SerializeField] private float swayDamping = 12f;

    private SpriteRenderer[] renderers;
    private bool isBeingDragged;
    private bool isAttachedToCat;
    private Quaternion baseLocalRotation;
    private float swayDegrees;
    private float swayAngularVelocity;
    private Vector2 lastDragPos;
    private bool hasDragSwaySample;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseLocalRotation = transform.localRotation;
    }

    public void OnBeginDrag()
    {
        isBeingDragged = true;
        hasDragSwaySample = false;
        swayDegrees = 0f;
        swayAngularVelocity = 0f;
        ApplySwayRotation();
        SetSortingLayer(dragLayer);
    }

    public void OnDrag(Vector2 mousePos)
    {
        if (!isBeingDragged) return;

        UpdateDragSway(mousePos);
        transform.position = mousePos;
    }

    public void OnEndDrag()
    {
        isBeingDragged = false;
        if (!isAttachedToCat) SetSortingLayer(defaultLayer);
    }

    public void AttachToCat(Transform attachPoint)
    {
        if (attachPoint == null) return;

        isBeingDragged = false;
        isAttachedToCat = true;

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D collider in colliders)
        {
            collider.enabled = false;
        }

        transform.SetParent(attachPoint, false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        swayDegrees = 0f;
        swayAngularVelocity = 0f;
        SetSortingLayer(attachedLayer);
    }

    private void Update()
    {
        SettleDragSway();
    }

    private void UpdateDragSway(Vector2 pointerWorldPos)
    {
        if (!enableDragSway) return;

        if (!hasDragSwaySample)
        {
            lastDragPos = pointerWorldPos;
            hasDragSwaySample = true;
            return;
        }

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;

        float horizontalSpeed = (pointerWorldPos.x - lastDragPos.x) / dt;
        lastDragPos = pointerWorldPos;
        float targetAngle = Mathf.Clamp(-horizontalSpeed * swayAnglePerSpeed, -swayMaxAngle, swayMaxAngle);
        StepSwaySpring(targetAngle, dt);
    }

    private void StepSwaySpring(float targetAngle, float dt)
    {
        swayAngularVelocity += (-swayStiffness * (swayDegrees - targetAngle) - swayDamping * swayAngularVelocity) * dt;
        swayDegrees += swayAngularVelocity * dt;
        ApplySwayRotation();
    }

    private void ApplySwayRotation()
    {
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, swayDegrees);
    }

    private void SettleDragSway()
    {
        if (isBeingDragged || (swayDegrees == 0f && swayAngularVelocity == 0f)) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;

        StepSwaySpring(0f, dt);
        if (Mathf.Abs(swayDegrees) < 0.01f && Mathf.Abs(swayAngularVelocity) < 0.01f)
        {
            swayDegrees = 0f;
            swayAngularVelocity = 0f;
            ApplySwayRotation();
        }
    }

    private void SetSortingLayer(string layerName)
    {
        if (renderers == null) return;
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null) renderer.sortingLayerName = layerName;
        }
    }
}