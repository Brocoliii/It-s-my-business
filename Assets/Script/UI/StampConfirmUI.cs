using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>Where the ink mark lands once the stamp hits the paper.</summary>
public enum ImprintPlacement
{
    /// <summary>Under the stamp, at the spot the player pressed.</summary>
    WhereStamped,

    /// <summary>Always the position the imprint was placed at in the scene.</summary>
    FixedPosition,
}

/// <summary>
/// Drag-and-drop stamp used to confirm a choice instead of a plain button.
/// Hold left mouse on the stamp, drag it over the stamping area and release:
/// releasing inside the area confirms, releasing anywhere else sends the stamp home.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class StampConfirmUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("References")]
    [Tooltip("The valid stamping area (BaseStamp). Releasing here confirms the action.")]
    public RectTransform baseStamp;

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

    [Header("Drag Grip")]
    [Tooltip("Keep the spot the player actually grabbed under the cursor instead of using Cursor Anchor.")]
    public bool keepGrabOffset;

    [Tooltip("Which point of the stamp sits under the cursor while dragging. (0,0) = bottom left, (0.5,0.5) = middle, (1,1) = top right.")]
    public Vector2 cursorAnchor = new Vector2(0.5f, 0.5f);

    [Tooltip("Extra nudge of that grip point, in the stamp's own pixels.")]
    public Vector2 cursorOffset;

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

    [Header("Imprint Placement")]
    [Tooltip("Where the ink mark shows up. Where Stamped = under the stamp, wherever the player pressed. Fixed Position = always the spot the imprint sits at in the scene.")]
    public ImprintPlacement imprintPlacement = ImprintPlacement.WhereStamped;

    [Tooltip("Nudge of the ink mark, in the imprint parent's pixels. Works with both placement modes.")]
    public Vector2 imprintOffset;

    [Header("Appear Animation")]
    [Tooltip("Duration of the pop-in when the stamp first appears (e.g. when the notebook opens).")]
    public float appearDuration = 0.35f;

    [Header("Disappear Animation")]
    [Tooltip("Duration of the lift-away once the stamp has already been pressed. 0 = disappear instantly.")]
    public float disappearDuration = 0.3f;

    [Tooltip("How far the stamp slides while it leaves, in its parent's pixels. Positive Y = up, away from the paper.")]
    public Vector2 disappearOffset = new Vector2(0f, 160f);

    [Tooltip("Scale the stamp shrinks to while it leaves.")]
    public float disappearScale = 0.7f;

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

    private Vector2 grabAnchor = new Vector2(0.5f, 0.5f);

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

    // ปิดสมุดทั้งที่ยังคว้าตราปั๊มค้างอยู่ OnEndDrag จะไม่ถูกเรียกเลย
    // ถ้าไม่เก็บกวาดตรงนี้ เมาส์จะค้างเป็นรูปมือกำทั้งที่ในมือไม่มีอะไรแล้ว
    private void OnDisable()
    {
        NotifyCursorHeld(false);
    }

    // บอกตัวจัดการรูปเมาส์ว่าตอนนี้ตราปั๊มอยู่ในมือผู้เล่นหรือเปล่า
    // ไม่มี CursorManager ในเกมก็ไม่เป็นไร ตราปั๊มยังลากได้เหมือนเดิมทุกอย่าง
    private void NotifyCursorHeld(bool isHeld)
    {
        if (CursorManager.Instance == null) return;

        if (isHeld) CursorManager.Instance.BeginUiDrag(this);
        else CursorManager.Instance.EndUiDrag(this);
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
        NotifyCursorHeld(false);

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

    /// <summary>Same end state as ResetStamp, but pops in from zero scale instead of snapping there instantly.</summary>
    public void PlayAppearAnimation()
    {
        EnsureInitialized();

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isDragging = false;
        isLocked = false;
        NotifyCursorHeld(false);

        ReturnToHomeParent(false);
        rectTransform.SetSiblingIndex(homeSiblingIndex);
        rectTransform.anchoredPosition = homeAnchoredPosition;

        ApplyDragSprite(false);
        ApplyZoneHighlight(false);
        ShowImprint(false);
        if (stampImprint != null) stampImprint.rectTransform.anchoredPosition = homeImprintPosition;

        // ห้ามลากตราปั๊มได้ระหว่างที่มันกำลังเด้งเข้ามา ไม่งั้นผู้เล่นอาจคว้าโดนตอนตัวยังจางๆ อยู่
        canvasGroup.blocksRaycasts = false;

        activeRoutine = StartCoroutine(AppearRoutine());
    }

    // เด้งขึ้นมาจากขนาดศูนย์แบบ ease-out back พร้อมจางเข้า จังหวะเดียวกับที่สมุด/รายการอื่นในสมุดใช้ตอนโผล่มา
    private IEnumerator AppearRoutine()
    {
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        float duration = Mathf.Max(0.01f, appearDuration);
        float time = 0f;

        while (time < duration)
        {
            // unscaledDeltaTime เผื่อสมุดเปิดไปพร้อมกับหน้าที่หยุดเวลาเกม
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            rectTransform.localScale = homeScale * EaseOutBack(t);
            canvasGroup.alpha = t;

            yield return null;
        }

        rectTransform.localScale = homeScale;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        activeRoutine = null;
    }

    // ยกตราปั๊มออกจากกระดาษหลังปั๊มเสร็จ ย่อ-เลื่อนออก-จางหาย ให้เห็นรอยหมึกที่เพิ่งลงไป
    private IEnumerator DisappearRoutine()
    {
        if (disappearDuration <= 0f)
        {
            HideStamp();
            yield break;
        }

        // ผู้เล่นคว้าตราปั๊มระหว่างที่มันกำลังลอยออกไม่ได้
        canvasGroup.blocksRaycasts = false;

        Vector2 fromPosition = rectTransform.anchoredPosition;
        Vector2 toPosition = fromPosition + disappearOffset;
        Vector3 fromScale = rectTransform.localScale;
        Vector3 toScale = homeScale * disappearScale;

        float time = 0f;

        while (time < disappearDuration)
        {
            // unscaledDeltaTime เหมือนจังหวะอื่นของตราปั๊ม เผื่อสมุดหยุดเวลาเกมไว้
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / disappearDuration);

            // ease-in back ทำให้ตราปั๊มย่อลงนิดหนึ่งก่อนดีดออก เป็นภาพกลับด้านของตอนเด้งเข้ามา
            float eased = EaseInBack(t);

            rectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
            rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);

            // จางช้าตอนต้นเพื่อให้ทันเห็นว่ามันเคลื่อนออกไป ไม่ใช่หายไปเฉยๆ
            canvasGroup.alpha = 1f - t * t;

            yield return null;
        }

        HideStamp();
    }

    // ease-out back: พุ่งเลยเป้าไปนิดหนึ่งแล้วค่อยดีดกลับ ค่าเดียวกับที่ NotebookManager ใช้กับรายการในสมุด
    private static float EaseOutBack(float t)
    {
        float overshoot = 1.70158f;
        float easedT = t - 1f;

        return 1f + (overshoot + 1f) * Mathf.Pow(easedT, 3f) + overshoot * Mathf.Pow(easedT, 2f);
    }

    // ease-in back: ถอยหลังนิดหนึ่งก่อนพุ่งออก เป็นด้านกลับของ EaseOutBack
    private static float EaseInBack(float t)
    {
        float overshoot = 1.70158f;

        return t * t * ((overshoot + 1f) * t - overshoot);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked || eventData.button != PointerEventData.InputButton.Left) return;

        // read the grab point before the reparent and the drag scale move the rect around
        CaptureGrabAnchor(eventData);

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        isDragging = true;
        NotifyCursorHeld(true);

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
        NotifyCursorHeld(false);
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
        if (baseStamp == null) return false;

        Camera cam = GetEventCamera();

        return RectTransformUtility.RectangleContainsScreenPoint(baseStamp, eventData.position, cam);
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
        PlaceImprint();
        ShowImprint(true);

        // เสียงปั๊มดังตอนหมึกลงกระดาษพอดี ไม่ใช่ตอนเริ่มกดลงมา
        SoundManager.Instance.PlaySFX(SoundIds.UiStamp);

        // let the player read the hit for a beat, then lift the stamp away to reveal the ink mark
        if (holdOnPaperDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(holdOnPaperDuration);
        }

        yield return DisappearRoutine();

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

    // the stamp hangs off the cursor by its grip point, so what the player sees is what the zone test reads
    private void CenterOnPointer(PointerEventData eventData)
    {
        Vector3 pointerWorld;
        if (!TryGetPointerWorldPoint(eventData, rectTransform.parent as RectTransform, out pointerWorld)) return;

        rectTransform.position = pointerWorld - GetPivotToGripOffset();
    }

    // where on the stamp the player closed their hand, kept as 0..1 of the rect so the drag scale cannot skew it
    private void CaptureGrabAnchor(PointerEventData eventData)
    {
        grabAnchor = cursorAnchor;
        if (!keepGrabOffset) return;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, eventData.pressPosition, GetEventCamera(), out local)) return;

        Vector2 normalized = Rect.PointToNormalized(rectTransform.rect, local);

        // a press just outside the rect (a soft sprite edge) would otherwise fling the stamp off the cursor
        grabAnchor = new Vector2(Mathf.Clamp01(normalized.x), Mathf.Clamp01(normalized.y));
    }

    // a stamp whose pivot is not on the grip point would hang off the cursor without this
    private Vector3 GetPivotToGripOffset()
    {
        Vector2 grip = keepGrabOffset ? grabAnchor : cursorAnchor;

        return GetPivotToPointOffset(rectTransform, grip, cursorOffset);
    }

    private Vector3 GetPivotToCenterOffset()
    {
        return GetPivotToPointOffset(rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero);
    }

    // distance from a rect's pivot to some normalized point on it, in world units at its current scale
    private static Vector3 GetPivotToPointOffset(RectTransform target, Vector2 normalizedPoint, Vector2 extraOffset)
    {
        Rect rect = target.rect;
        Vector2 pivot = target.pivot;

        Vector3 pointLocal = new Vector3(
            (normalizedPoint.x - pivot.x) * rect.width + extraOffset.x,
            (normalizedPoint.y - pivot.y) * rect.height + extraOffset.y,
            0f);

        return target.TransformPoint(pointLocal) - target.position;
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

    private void PlaceImprint()
    {
        if (stampImprint == null) return;

        RectTransform imprintRect = stampImprint.rectTransform;

        if (imprintPlacement == ImprintPlacement.FixedPosition)
        {
            // the spot the imprint was authored at in the scene, so the mark always lands on the same line of the paper
            imprintRect.anchoredPosition = homeImprintPosition + imprintOffset;
            return;
        }

        // the ink mark belongs under the stamp, so it lands wherever the player pressed
        PlaceImprintCenterAt(rectTransform.position + GetPivotToCenterOffset());
        imprintRect.anchoredPosition += imprintOffset;
    }

    private void PlaceImprintCenterAt(Vector3 worldCenter)
    {
        if (stampImprint == null) return;

        RectTransform imprintRect = stampImprint.rectTransform;

        imprintRect.position = worldCenter - GetPivotToPointOffset(imprintRect, new Vector2(0.5f, 0.5f), Vector2.zero);
    }
}
