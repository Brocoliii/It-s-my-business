using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class SauceBrush : MonoBehaviour, IDraggable
{
    [Header("Brush Settings")]
    [Tooltip("How far the mouse must move in one direction before it counts as part of a shake (small = light shakes count)")]
    [SerializeField] private float requiredDrag = 20f;
    [Tooltip("Number of up/down shakes needed before sauce is applied")]
    [SerializeField] private int requiredStrokes = 4;

    [Header("Layer Settings")]
    public string defaultLayer = "Tools";
    public string dragLayer = "Dragging";

    [Header("Snap Back")]
    [SerializeField] private float snapMoveDuration = 0.2f;
    [Tooltip("Extra height the brush hops up before dropping back down onto its holder")]
    [SerializeField] private float snapArcHeight = 0.3f;

    [Header("Pickup")]
    [Tooltip("ระยะเวลาที่แปรงค่อย ๆ หมุนเข้าท่าจับตอนเพิ่งคว้าขึ้นมา (0 = หมุนทันทีแบบเดิม)")]
    [SerializeField] private float pickupRotationDuration = 0.15f;

    [Header("Preview Hold")]
    [Tooltip("แปรงหลุดออกนอกอาหารได้นานแค่ไหน ก่อนที่ซอสจาง ๆ กับจังหวะที่ปาดค้างไว้จะถูกล้างทิ้ง")]
    [SerializeField] private float previewHoldDuration = 1f;
    [Tooltip("ระยะเวลาที่ซอส preview ค่อย ๆ จางหายหลังหมดเวลาค้าง (0 = หายวับทันที)")]
    [SerializeField] private float previewFadeOutDuration = 0.25f;

    [Header("Brushing Shake")]
    [Tooltip("How many degrees the brush tilts up/down while actively brushing food")]
    [SerializeField] private float shakeAngle = 8f;
    [Tooltip("How quickly the tilt eases toward its target angle")]
    [SerializeField] private float shakeSpeed = 12f;

    [Header("Success Feedback")]
    [Tooltip("Sprite that pops up above the food when the sauce is applied successfully.")]
    [SerializeField] private Sprite successSprite;
    [SerializeField] private Vector3 successSpriteOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField] private float successSpriteDuration = 0.7f;
    [SerializeField] private float successSpriteRise = 0.35f;
    [SerializeField] private float successSpriteScale = 1f;
    [SerializeField] private string successSpriteSortingLayer = "Dragging";
    [SerializeField] private int successSpriteSortingOrder = 20;

    private static readonly Collider2D[] overlapResults = new Collider2D[16];
    private static readonly ContactFilter2D overlapFilter = new ContactFilter2D().NoFilter();

    private Vector3 startPos;
    private float segmentDistance = 0f;
    private int lastDirection = 0;
    private int strokeCount = 0;
    // จังหวะปาดล่าสุดที่เล่นเสียงไปแล้ว กันไม่ให้เสียงดังรัวทุกเฟรมที่แปรงทาบอยู่บนอาหาร
    private int soundedStrokeCount = 0;
    private FoodInstance previewFood;
    // แปรงทาบอยู่บนอาหารจริง ๆ ในเฟรมล่าสุดไหม (previewFood ยังค้างอยู่ได้แม้แปรงจะหลุดออกไปแล้ว)
    private bool brushTouchingFood = false;
    private float offFoodTime = 0f;
    // ความเข้มซอส preview ล่าสุดตอนที่แปรงยังทาบอยู่ ใช้ค้างภาพไว้ตอนแปรงหลุดออกนอกอาหาร
    private float heldPreviewProgress = 0f;
    private float currentRockAngle = 0f;
    private Quaternion grabStartRotation;
    private float pickupRotationBlend;
    private SpriteRenderer sr;
    private Collider2D brushCollider;
    private Coroutine snapMoveCoroutine;

    private void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
        brushCollider = GetComponent<Collider2D>();
        if (sr != null) sr.sortingLayerName = defaultLayer;
    }

    public void OnBeginDrag()
    {
        // เผื่อคว้าแปรงขึ้นมาอีกครั้งระหว่างที่มันกำลังลอยกลับตำแหน่งเดิมอยู่ ต้องหยุด animation เก่าก่อน ไม่งั้นจะแย่งกันควบคุมตำแหน่ง
        if (snapMoveCoroutine != null)
        {
            StopCoroutine(snapMoveCoroutine);
            snapMoveCoroutine = null;
        }

        ClearPreviewState();
        currentRockAngle = 0f;

        if (sr != null) sr.sortingLayerName = dragLayer;

        // ค่อย ๆ หมุนเข้าท่าจับจากมุมที่แปรงวางอยู่ แทนที่จะสะบัดเข้าท่าในเฟรมเดียวพร้อมกับที่ตัวแปรงเลื่อนเข้าหาเมาส์
        grabStartRotation = transform.rotation;
        pickupRotationBlend = pickupRotationDuration > 0f ? 0f : 1f;
        ApplyDragRotation();

        // ดังตอนคว้าแปรงขึ้นมาเท่านั้น ไม่เกี่ยวกับตอนทาซอสติด (นั่นคือ SoundIds.SauceBrush)
        SoundManager.Instance.PlaySFX(SoundIds.SauceBrushPickup, transform.position);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;

        float deltaY = Mouse.current.delta.ReadValue().y;
        int direction = deltaY > 0.01f ? 1 : (deltaY < -0.01f ? -1 : 0);

        // นับเฉพาะตอนแปรงทาบอยู่บนอาหาร ระหว่างช่วงค้าง preview ที่แปรงหลุดออกไปให้แช่ความคืบหน้าไว้เฉย ๆ
        // ไม่งั้นสะบัดแปรงกลางอากาศแล้ววกกลับเข้ามาจะทำให้ซอสติดทันที
        if (direction != 0 && brushTouchingFood)
        {
            if (lastDirection != 0 && direction != lastDirection)
            {
                // Mouse reversed course - count it as a brush stroke if it moved far enough first
                if (segmentDistance >= requiredDrag)
                {
                    strokeCount++;
                }
                segmentDistance = 0f;
            }

            segmentDistance += Mathf.Abs(deltaY);
            lastDirection = direction;
        }

        UpdateBrushTarget();

        // Tilt the brush toward the direction it's currently being dragged while brushing food, easing back to neutral otherwise
        float targetRockAngle = 0f;
        if (brushTouchingFood)
        {
            if (direction == 1) targetRockAngle = shakeAngle;
            else if (direction == -1) targetRockAngle = -shakeAngle;
            else targetRockAngle = currentRockAngle;
        }
        currentRockAngle = Mathf.Lerp(currentRockAngle, targetRockAngle, Time.deltaTime * shakeSpeed);

        if (pickupRotationBlend < 1f)
            pickupRotationBlend = Mathf.Clamp01(pickupRotationBlend + Time.deltaTime / pickupRotationDuration);

        ApplyDragRotation();
    }

    // ท่าจับตอนลากคือปลายทาง แต่ช่วงแรกหลังคว้ายังผสมกับมุมตอนวางอยู่ เพื่อให้หมุนเข้าท่าแบบไหลลื่น
    private void ApplyDragRotation()
    {
        Quaternion dragRotation = Quaternion.Euler(0, 0, -15f + currentRockAngle);

        transform.rotation = pickupRotationBlend >= 1f
            ? dragRotation
            : Quaternion.Slerp(grabStartRotation, dragRotation, pickupRotationBlend);
    }

    public void OnEndDrag()
    {
        if (snapMoveCoroutine != null)
            StopCoroutine(snapMoveCoroutine);

        if (!gameObject.activeInHierarchy)
        {
            transform.position = startPos;
            transform.rotation = Quaternion.identity;
            if (sr != null) sr.sortingLayerName = defaultLayer;
        }
        else
        {
            snapMoveCoroutine = StartCoroutine(SmoothSnapBack());
        }

        // วางแปรงลงคือตั้งใจเลิกทา ต้องล้าง preview ทันที ไม่ใช้ช่วงค้างเหมือนตอนแปรงหลุดออกนอกอาหาร
        // (OnDrag หยุดเรียกแล้ว ถ้าปล่อยค้างไว้ซอสจาง ๆ จะติดอยู่บนอาหารตลอดไป)
        if (previewFood != null)
        {
            previewFood.SetSaucePreview(0f);
        }
        ClearPreviewState();
    }

    // ลอยนุ่มๆ กลับไปตำแหน่งตั้งต้น แทนที่จะกระโดดวาร์ปทันที แล้วค่อยสลับ layer กลับตอนถึงที่แล้ว
    private IEnumerator SmoothSnapBack()
    {
        Vector3 fromPos = transform.position;
        Quaternion fromRotation = transform.rotation;
        float elapsed = 0f;

        while (elapsed < snapMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapMoveDuration);
            t = t * t * (3f - 2f * t); // smoothstep เพื่อให้เริ่ม/หยุดนุ่มนวล ไม่กระตุก

            Vector3 pos = Vector3.Lerp(fromPos, startPos, t);
            pos.y += Mathf.Sin(t * Mathf.PI) * snapArcHeight; // ยกขึ้นก่อนแล้วค่อยตกลงมาที่จุดวาง แทนที่จะเลื่อนเป็นเส้นตรง
            transform.position = pos;
            transform.rotation = Quaternion.Lerp(fromRotation, Quaternion.identity, t);

            yield return null;
        }

        transform.position = startPos;
        transform.rotation = Quaternion.identity;

        if (sr != null) sr.sortingLayerName = defaultLayer;

        snapMoveCoroutine = null;
    }

    // ให้ซอสค่อยๆ จางขึ้นบนอาหารตรงตำแหน่งที่แปรงกำลังทาบอยู่ ตามจำนวนครั้งที่แปรงขึ้น-ลงไปแล้ว
    private void UpdateBrushTarget()
    {
        FoodInstance food = FindSeasonableFoodUnderBrush();
        brushTouchingFood = food != null;

        if (food == null)
        {
            HoldPreviewOffFood();
            return;
        }

        offFoodTime = 0f;

        if (food != previewFood)
        {
            if (previewFood != null) previewFood.SetSaucePreview(0f);
            previewFood = food;

            // Strokes done on the old food must not carry over and instantly apply to the new one
            strokeCount = 0;
            soundedStrokeCount = 0;
            segmentDistance = 0f;
            lastDirection = 0;
            heldPreviewProgress = 0f;
        }

        // จังหวะสุดท้ายต้องเช็คก่อนเสียงปาด แล้ว return ออกไปเลย
        // เพราะจังหวะนั้นเป็นของเสียง sauce_done ที่ ApplySauce เล่นให้ ถ้าปล่อยผ่านจะได้ยินสองเสียงทับกันในเฟรมเดียว
        if (strokeCount >= requiredStrokes)
        {
            previewFood.ApplySauce();
            ShowSuccessSprite(previewFood);
            ClearPreviewState();
            return;
        }

        // ดังทีละครั้งต่อ 1 จังหวะปาด (นับตอนเมาส์กลับทิศหลังลากไปไกลพอ) ไม่ใช่ดังรัวทุกเฟรม
        if (strokeCount > soundedStrokeCount)
        {
            soundedStrokeCount = strokeCount;
            SoundManager.Instance.PlaySFX(SoundIds.SauceBrush, previewFood.FoodRoot.position);
        }

        float segmentProgress = Mathf.Clamp01(segmentDistance / requiredDrag);
        heldPreviewProgress = (strokeCount + segmentProgress) / requiredStrokes;
        previewFood.SetSaucePreview(heldPreviewProgress);
    }

    // ปาดเลยขอบอาหารออกไปนิดเดียวไม่ควรล้างทุกอย่างทิ้งทันที
    // ค้างซอสจาง ๆ ไว้ครู่หนึ่งแล้วค่อยเฟดหาย ถ้าผู้เล่นวกกลับเข้ามาทันก็ปาดต่อจากความคืบหน้าเดิมได้เลย
    private void HoldPreviewOffFood()
    {
        if (previewFood == null) return;

        offFoodTime += Time.deltaTime;

        if (offFoodTime <= previewHoldDuration) return;

        float fade = previewFadeOutDuration > 0f
            ? 1f - Mathf.Clamp01((offFoodTime - previewHoldDuration) / previewFadeOutDuration)
            : 0f;

        if (fade <= 0f)
        {
            previewFood.SetSaucePreview(0f);
            ClearPreviewState();
            return;
        }

        previewFood.SetSaucePreview(heldPreviewProgress * fade);
    }

    private void ClearPreviewState()
    {
        previewFood = null;
        brushTouchingFood = false;
        offFoodTime = 0f;
        heldPreviewProgress = 0f;
        strokeCount = 0;
        soundedStrokeCount = 0;
        segmentDistance = 0f;
        lastDirection = 0;
    }

    // ใช้ collider ของหัวแปรงเองเช็คว่าทาบโดนวัตถุดิบชิ้นไหนอยู่ (แทนการยิง raycast จากจุดเมาส์จุดเดียว)
    // กันพลาดกรณีปลายแปรงหมุน/มีขนาดใหญ่กว่าจุดเมาส์
    private FoodInstance FindSeasonableFoodUnderBrush()
    {
        if (brushCollider == null) return null;

        Physics2D.SyncTransforms();
        int count = brushCollider.Overlap(overlapFilter, overlapResults);
        for (int i = 0; i < count; i++)
        {
            FoodInstance food = overlapResults[i].GetComponent<FoodInstance>();
            if (food != null && food.currentSeasoning != null && !food.hasSauce && food.CanSeason())
            {
                return food;
            }
        }
        return null;
    }

    // ป๊อปอัพ sprite เด้งขึ้นเหนืออาหารตอนทาซอสสำเร็จ (เหมือน combo sprite ของขวดพริก)
    private void ShowSuccessSprite(FoodInstance food)
    {
        if (food == null || successSprite == null)
            return;

        GameObject popupObject = new GameObject("SauceSuccessFeedback");
        SpriteRenderer popupRenderer = popupObject.AddComponent<SpriteRenderer>();
        popupRenderer.sprite = successSprite;
        popupRenderer.sortingLayerName = successSpriteSortingLayer;
        popupRenderer.sortingOrder = successSpriteSortingOrder;
        popupObject.transform.position = food.transform.position + successSpriteOffset;
        popupObject.transform.localScale = Vector3.one * successSpriteScale;

        // กันไว้เผื่อแปรงถูกปิด (deactivate) ระหว่าง animation ทำให้ coroutine หยุด popup จะได้ไม่ค้างอยู่ในฉาก
        Destroy(popupObject, successSpriteDuration + 0.1f);

        StartCoroutine(AnimateSuccessSprite(popupObject, popupRenderer));
    }

    private IEnumerator AnimateSuccessSprite(GameObject popupObject, SpriteRenderer popupRenderer)
    {
        float duration = Mathf.Max(0.01f, successSpriteDuration);
        Vector3 startPosition = popupObject.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (popupObject == null) yield break;

            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / duration);
            popupObject.transform.position = startPosition + Vector3.up * (successSpriteRise * progress);

            Color color = popupRenderer.color;
            color.a = 1f - progress;
            popupRenderer.color = color;

            yield return null;
        }

        if (popupObject != null)
            Destroy(popupObject);
    }
}