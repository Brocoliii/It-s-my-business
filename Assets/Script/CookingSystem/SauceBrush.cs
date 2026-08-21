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

    [Header("Brushing Shake")]
    [Tooltip("How many degrees the brush tilts up/down while actively brushing food")]
    [SerializeField] private float shakeAngle = 8f;
    [Tooltip("How quickly the tilt eases toward its target angle")]
    [SerializeField] private float shakeSpeed = 12f;

    private static readonly Collider2D[] overlapResults = new Collider2D[16];
    private static readonly ContactFilter2D overlapFilter = new ContactFilter2D().NoFilter();

    private Vector3 startPos;
    private float segmentDistance = 0f;
    private int lastDirection = 0;
    private int strokeCount = 0;
    private FoodInstance previewFood;
    private float currentRockAngle = 0f;
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

        segmentDistance = 0f;
        lastDirection = 0;
        strokeCount = 0;
        previewFood = null;
        currentRockAngle = 0f;

        if (sr != null) sr.sortingLayerName = dragLayer;
        transform.rotation = Quaternion.Euler(0, 0, -15f);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;

        float deltaY = Mouse.current.delta.ReadValue().y;
        int direction = deltaY > 0.01f ? 1 : (deltaY < -0.01f ? -1 : 0);

        if (direction != 0)
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
        if (previewFood != null)
        {
            if (direction == 1) targetRockAngle = shakeAngle;
            else if (direction == -1) targetRockAngle = -shakeAngle;
            else targetRockAngle = currentRockAngle;
        }
        currentRockAngle = Mathf.Lerp(currentRockAngle, targetRockAngle, Time.deltaTime * shakeSpeed);
        transform.rotation = Quaternion.Euler(0, 0, -15f + currentRockAngle);
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

        if (previewFood != null)
        {
            previewFood.SetSaucePreview(0f);
            previewFood = null;
        }
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

        if (food != previewFood)
        {
            if (previewFood != null) previewFood.SetSaucePreview(0f);
            previewFood = food;

            // Strokes done on the old food must not carry over and instantly apply to the new one
            strokeCount = 0;
            segmentDistance = 0f;
            lastDirection = 0;
        }

        if (previewFood == null) return;

        if (strokeCount >= requiredStrokes)
        {
            previewFood.ApplySauce();
            previewFood = null;
            strokeCount = 0;
            segmentDistance = 0f;
            lastDirection = 0;
            return;
        }

        float segmentProgress = Mathf.Clamp01(segmentDistance / requiredDrag);
        float progress = (strokeCount + segmentProgress) / requiredStrokes;
        previewFood.SetSaucePreview(progress);
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
}