using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Drag-and-drop stamp used to confirm a choice instead of a plain button.
/// Hold left mouse on the stamp, drag it over the stamping area and release:
/// releasing inside the area confirms, releasing anywhere else sends the stamp home.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StampConfirmUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The valid stamping area. Releasing here confirms the action.")]
    public RectTransform stampZone;

    [Tooltip("Optional parent used while dragging so the stamp draws on top. Defaults to the canvas root.")]
    public RectTransform dragLayer;

    [Tooltip("Optional ink mark revealed on the paper after a successful stamp.")]
    public Graphic stampImprint;

    [Header("Sprites")]
    [Tooltip("The stamp's own Image. Left empty = the Image on this object.")]
    public Image stampImage;

    [Tooltip("Sprite while the stamp rests on its pad. Left empty = whatever the Image already uses.")]
    public Sprite idleSprite;

    [Tooltip("Sprite while the player is holding the stamp.")]
    public Sprite dragSprite;

    [Header("Zone Feedback")]
    public Graphic zoneHighlight;
    public Color zoneIdleColor = new Color(1f, 1f, 1f, 0.25f);
    public Color zoneReadyColor = new Color(1f, 0.25f, 0.25f, 0.75f);

    [Header("Feel")]
    [Tooltip("Scale of the stamp while it is held in the air.")]
    public float dragScale = 1.15f;
    [Tooltip("Scale of the stamp at the moment it hits the paper.")]
    public float pressScale = 0.85f;
    public float pressDuration = 0.1f;
    [Tooltip("How long the stamp stays on the paper before it disappears and shows the ink mark.")]
    public float holdOnPaperDuration = 0.25f;
    [Tooltip("How fast the stamp flies back home when released outside the area.")]
    public float returnSpeed = 14f;

    [Header("Events")]
    public UnityEvent onStampConfirmed;
    public UnityEvent onStampCancelled;

    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;

    private Transform homeParent;
    private int homeSiblingIndex;
    private Vector2 homeAnchoredPosition;
    private Vector3 homeScale;
    private Vector2 homeImprintPosition;

    private bool isInitialized;
    private bool isDragging;
    private bool isLocked;
    private Coroutine activeRoutine;

    public bool IsLocked { get { return isLocked; } }

    private void Awake()
    {
        EnsureInitialized();

        ApplyZoneHighlight(false);
        ShowImprint(false);
    }

    // the stamp usually sits inside a tab that starts disabled, so Awake may not have run yet
    private void EnsureInitialized()
    {
        if (isInitialized) return;
        isInitialized = true;

        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>(true);

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (stampImage == null) stampImage = GetComponent<Image>();
        if (idleSprite == null && stampImage != null) idleSprite = stampImage.sprite;

        homeParent = rectTransform.parent;
        homeSiblingIndex = rectTransform.GetSiblingIndex();
        homeAnchoredPosition = rectTransform.anchoredPosition;
        homeScale = rectTransform.localScale;

        if (stampImprint != null) homeImprintPosition = stampImprint.rectTransform.anchoredPosition;
    }

    /// <summary>Puts the stamp back on its pad and makes it draggable again.</summary>
    public void ResetStamp()
    {
        EnsureInitialized();

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isDragging = false;
        isLocked = false;

        ReturnToHomeParent(false);
        rectTransform.SetSiblingIndex(homeSiblingIndex);
        rectTransform.anchoredPosition = homeAnchoredPosition;
        rectTransform.localScale = homeScale;

        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        ApplyDragSprite(false);
        ApplyZoneHighlight(false);
        ShowImprint(false);

        if (stampImprint != null) stampImprint.rectTransform.anchoredPosition = homeImprintPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || eventData.button != PointerEventData.InputButton.Left) return;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isDragging = true;

        RectTransform layer = ResolveDragLayer();
        if (layer != null && rectTransform.parent != layer)
        {
            rectTransform.SetParent(layer, true);
        }

        rectTransform.SetAsLastSibling();
        rectTransform.localScale = homeScale * dragScale;
        ApplyDragSprite(true);

        // let the drop area behind the stamp stay visible to the pointer
        canvasGroup.blocksRaycasts = false;

        CenterOnPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        CenterOnPointer(eventData);
        ApplyZoneHighlight(IsOverStampZone(eventData));
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        ApplyZoneHighlight(false);

        if (IsOverStampZone(eventData))
        {
            isLocked = true;
            activeRoutine = StartCoroutine(StampRoutine());
        }
        else
        {
            // the stamp is out of the player's hand again, so it goes back to its resting look
            ApplyDragSprite(false);

            activeRoutine = StartCoroutine(ReturnRoutine());
            onStampCancelled.Invoke();
        }
    }

    private bool IsOverStampZone(PointerEventData eventData)
    {
        if (stampZone == null) return false;

        Camera cam = GetEventCamera();

        return RectTransformUtility.RectangleContainsScreenPoint(stampZone, eventData.position, cam);
    }

    private IEnumerator StampRoutine()
    {
        // press exactly where the player let go instead of snapping to the middle of the zone
        Vector3 pressPosition = rectTransform.position;

        // rejoin the notebook hierarchy so the stamp hides with it
        ReturnToHomeParent(true);
        rectTransform.SetAsLastSibling();
        rectTransform.position = pressPosition;

        Vector3 liftedScale = homeScale * dragScale;
        Vector3 pressedScale = homeScale * pressScale;

        yield return ScaleTo(liftedScale, pressedScale, pressDuration);

        // read the center after the squash, so the mark sits under the stamp as it looks on the paper
        PlaceImprintCenterAt(rectTransform.position + GetPivotToCenterOffset());
        ShowImprint(true);

        // let the player read the hit for a beat, then lift the stamp away to reveal the ink mark
        if (holdOnPaperDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdOnPaperDuration);
        }

        HideStamp();

        activeRoutine = null;
        onStampConfirmed.Invoke();
    }

    private IEnumerator ReturnRoutine()
    {
        // keep the stamp where the player dropped it, then slide it home from there
        ReturnToHomeParent(true);
        rectTransform.SetSiblingIndex(homeSiblingIndex);

        while (Vector2.Distance(rectTransform.anchoredPosition, homeAnchoredPosition) > 0.5f)
        {
            float t = 1f - Mathf.Exp(-returnSpeed * Time.unscaledDeltaTime);

            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, homeAnchoredPosition, t);
            rectTransform.localScale = Vector3.Lerp(rectTransform.localScale, homeScale, t);

            yield return null;
        }

        rectTransform.anchoredPosition = homeAnchoredPosition;
        rectTransform.localScale = homeScale;
        activeRoutine = null;
    }

    private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration)
    {
        if (duration <= 0f)
        {
            rectTransform.localScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // unscaledDeltaTime so the stamp still animates while the notebook pauses the game
            elapsed += Time.unscaledDeltaTime;
            rectTransform.localScale = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        rectTransform.localScale = to;
    }

    private void ReturnToHomeParent(bool keepWorldPosition)
    {
        if (homeParent != null && rectTransform.parent != homeParent)
        {
            rectTransform.SetParent(homeParent, keepWorldPosition);
        }
    }

    private RectTransform ResolveDragLayer()
    {
        if (dragLayer != null) return dragLayer;
        if (parentCanvas != null) return parentCanvas.rootCanvas.transform as RectTransform;

        return null;
    }

    // the stamp rides centered on the cursor, so what the player sees is what the zone test reads
    private void CenterOnPointer(PointerEventData eventData)
    {
        Vector3 pointerWorld;
        if (!TryGetPointerWorldPoint(eventData, rectTransform.parent as RectTransform, out pointerWorld)) return;

        rectTransform.position = pointerWorld - GetPivotToCenterOffset();
    }

    // a stamp whose pivot is not in the middle would hang off the cursor without this
    private Vector3 GetPivotToCenterOffset()
    {
        Rect rect = rectTransform.rect;
        Vector2 pivot = rectTransform.pivot;

        Vector3 centerLocal = new Vector3((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height, 0f);

        return rectTransform.TransformPoint(centerLocal) - rectTransform.position;
    }

    private bool TryGetPointerWorldPoint(PointerEventData eventData, RectTransform space, out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (space == null) return false;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(space, eventData.position, GetEventCamera(), out worldPoint);
    }

    private Camera GetEventCamera()
    {
        if (parentCanvas == null) return null;

        // overlay canvases must be queried with a null camera
        return parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera;
    }

    private void ApplyZoneHighlight(bool isReady)
    {
        if (zoneHighlight == null) return;

        zoneHighlight.color = isReady ? zoneReadyColor : zoneIdleColor;
    }

    private void ApplyDragSprite(bool isDragged)
    {
        if (stampImage == null) return;

        Sprite wanted = isDragged && dragSprite != null ? dragSprite : idleSprite;
        if (wanted != null)
        {
            stampImage.sprite = wanted;
        }
    }

    // alpha instead of SetActive: deactivating this object would kill the routine mid-stamp
    private void HideStamp()
    {
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    private void ShowImprint(bool isVisible)
    {
        if (stampImprint == null) return;

        stampImprint.gameObject.SetActive(isVisible);
    }

    // the ink mark belongs under the stamp, so it lands wherever the player pressed
    private void PlaceImprintCenterAt(Vector3 worldCenter)
    {
        if (stampImprint == null) return;

        RectTransform imprintRect = stampImprint.rectTransform;
        Rect rect = imprintRect.rect;
        Vector2 pivot = imprintRect.pivot;

        Vector3 centerLocal = new Vector3((0.5f - pivot.x) * rect.width, (0.5f - pivot.y) * rect.height, 0f);
        Vector3 pivotToCenter = imprintRect.TransformPoint(centerLocal) - imprintRect.position;

        imprintRect.position = worldCenter - pivotToCenter;
    }
}
