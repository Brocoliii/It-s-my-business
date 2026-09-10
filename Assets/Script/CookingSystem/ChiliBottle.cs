using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChiliBottle : MonoBehaviour, IDraggable
{
    [Header("Shake Settings")]
    [SerializeField] private float requiredShake = 150f;
    [SerializeField] private float minShakeDelta = 2f;
    [SerializeField] private float seasoningCooldown = 0.5f;

    [Header("Snap Back")]
    [SerializeField] private float snapMoveDuration = 0.2f;

    [Tooltip("Reset shake progress if the player stops shaking.")]
    [SerializeField] private float shakeResetDelay = 0.4f;

    [Header("Pickup")]
    [Tooltip("ระยะเวลาที่ขวดค่อย ๆ หมุนเข้าท่าเขย่าตอนเพิ่งคว้าขึ้นมา (0 = หมุนทันทีแบบเดิม)")]
    [SerializeField] private float pickupRotationDuration = 0.15f;

    [Header("Shake Rotation")]
    [SerializeField] private float baseDragRotation = 165f;
    [SerializeField] private float shakeRotationAngle = 20f;
    [SerializeField] private float shakeRotationSpeed = 12f;

    [Header("Layer Settings")]
    public string defaultLayer = "Tools";
    public string dragLayer = "Dragging";

    [Header("Drag Sprite")]
    [SerializeField] private Sprite dragSprite;

    [Header("Powder Effect")]
    [SerializeField] private ChiliPowderEffect powderEffect;

    private Vector3 startPos;
    private SpriteRenderer sr;
    private Sprite defaultSprite;

    private float shakeProgress;
    private float lastShakeDirection;
    private float lastSeasoningTime = -999f;
    private float lastShakeMoveTime;

    private float shakeRotationOffset;
    private float targetShakeRotationOffset;

    private Quaternion grabStartRotation;
    private float pickupRotationBlend;

    private Coroutine snapMoveCoroutine;

    private void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
            defaultSprite = sr.sprite;

        if (powderEffect == null)
            powderEffect = GetComponent<ChiliPowderEffect>();
        if (powderEffect == null)
            powderEffect = gameObject.AddComponent<ChiliPowderEffect>();
    }

    public void OnBeginDrag()
    {
        // เผื่อคว้าขวดขึ้นมาอีกครั้งระหว่างที่มันกำลังลอยกลับตำแหน่งเดิมอยู่ ต้องหยุด animation เก่าก่อน ไม่งั้นจะแย่งกันควบคุมตำแหน่ง
        if (snapMoveCoroutine != null)
        {
            StopCoroutine(snapMoveCoroutine);
            snapMoveCoroutine = null;
        }

        shakeProgress = 0f;
        lastShakeDirection = 0f;
        lastShakeMoveTime = Time.time;
        shakeRotationOffset = 0f;
        targetShakeRotationOffset = 0f;

        if (sr != null)
        {
            sr.sortingLayerName = dragLayer;

            if (dragSprite != null)
                sr.sprite = dragSprite;
        }

        // ค่อย ๆ หมุนเข้าท่าเขย่าจากมุมที่ขวดวางอยู่ แทนที่จะสะบัดเข้าท่าในเฟรมเดียวพร้อมกับที่ตัวขวดเลื่อนเข้าหาเมาส์
        grabStartRotation = transform.rotation;
        pickupRotationBlend = pickupRotationDuration > 0f ? 0f : 1f;
        ApplyDragRotation();

        // ดังตอนคว้าขวดขึ้นมาเท่านั้น ไม่เกี่ยวกับตอนเขย่าใส่พริก (นั่นคือ SoundIds.ChiliShake)
        SoundManager.Instance.PlaySFX(SoundIds.ChiliBottlePickup, transform.position);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;

        if (Mouse.current == null)
            return;

        float deltaY = Mouse.current.delta.ReadValue().y;
        float absDeltaY = Mathf.Abs(deltaY);

        // Reset progress if player stopped shaking.
        if (Time.time - lastShakeMoveTime > shakeResetDelay)
        {
            shakeProgress = 0f;
            lastShakeDirection = 0f;
            targetShakeRotationOffset = 0f;
        }

        if (absDeltaY >= minShakeDelta)
        {
            lastShakeMoveTime = Time.time;

            float direction = Mathf.Sign(deltaY);

            // จังหวะเขย่า 1 ครั้ง = ตอนเมาส์กลับทิศ (หรือครั้งแรกที่เริ่มขยับ) ใช้เป็นจังหวะของเสียงด้วย
            bool shookThisFrame = false;

            if (lastShakeDirection != 0f && direction != lastShakeDirection)
            {
                shakeProgress += absDeltaY;
                targetShakeRotationOffset = direction * shakeRotationAngle;
                shookThisFrame = true;

                if (powderEffect != null)
                    powderEffect.SpawnPuff();
            }
            else if (lastShakeDirection == 0f)
            {
                shakeProgress += absDeltaY * 0.5f;
                targetShakeRotationOffset = direction * shakeRotationAngle;
                shookThisFrame = true;
            }

            lastShakeDirection = direction;

            bool seasonedThisFrame = false;

            if (shakeProgress >= requiredShake)
            {
                if (TryApplySeasoning(mousePos))
                {
                    shakeProgress = 0f;
                    seasonedThisFrame = true;
                }
            }

            // เขย่าเปล่า ๆ ก็ต้องได้ยินเสียงขวด ผู้เล่นจะได้รู้ว่ากำลังเขย่าอยู่จริง
            // ยกเว้นจังหวะที่พริกลงบนอาหารสำเร็จ จังหวะนั้นปล่อยให้เป็นเสียง chili_shake ตัวเดียว ไม่ให้ทับกัน
            if (shookThisFrame && !seasonedThisFrame)
            {
                SoundManager.Instance.PlaySFX(SoundIds.ChiliBottleShake, transform.position);
            }
        }

        shakeRotationOffset = Mathf.Lerp(shakeRotationOffset, targetShakeRotationOffset, Time.deltaTime * shakeRotationSpeed);

        if (pickupRotationBlend < 1f)
            pickupRotationBlend = Mathf.Clamp01(pickupRotationBlend + Time.deltaTime / pickupRotationDuration);

        ApplyDragRotation();
    }

    // ท่าเขย่าคือปลายทาง แต่ช่วงแรกหลังคว้ายังผสมกับมุมตอนวางอยู่ เพื่อให้หมุนเข้าท่าแบบไหลลื่น
    private void ApplyDragRotation()
    {
        Quaternion dragRotation = Quaternion.Euler(0, 0, baseDragRotation + shakeRotationOffset);

        transform.rotation = pickupRotationBlend >= 1f
            ? dragRotation
            : Quaternion.Slerp(grabStartRotation, dragRotation, pickupRotationBlend);
    }

    private bool TryApplySeasoning(Vector2 mousePos)
    {
        if (Time.time < lastSeasoningTime + seasoningCooldown)
            return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);

        foreach (var hit in hits)
        {
            FoodInstance food = hit.collider.GetComponent<FoodInstance>();

            if (food != null &&
                food.currentSeasoning != null &&
                food.CanSeason() &&
                food.spicyLevel < 3)
            {
                food.AddSpicy();

                lastSeasoningTime = Time.time;

                // Small feedback
                transform.position += Vector3.up * 0.2f;

                if (powderEffect != null)
                    powderEffect.Burst();

                return true;
            }
        }

        return false;
    }

    public void OnEndDrag()
    {
        shakeProgress = 0f;
        lastShakeDirection = 0f;
        shakeRotationOffset = 0f;
        targetShakeRotationOffset = 0f;

        if (snapMoveCoroutine != null)
            StopCoroutine(snapMoveCoroutine);

        snapMoveCoroutine = StartCoroutine(SmoothSnapBack());
    }

    // ลอยนุ่มๆ กลับไปตำแหน่ง/หมุนตั้งต้น แทนที่จะกระโดดวาร์ปทันที แล้วค่อยสลับ layer/sprite กลับตอนถึงที่แล้ว
    private IEnumerator SmoothSnapBack()
    {
        Vector3 fromPos = transform.position;
        Quaternion fromRot = transform.rotation;
        float elapsed = 0f;

        while (elapsed < snapMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapMoveDuration);
            t = t * t * (3f - 2f * t); // smoothstep เพื่อให้เริ่ม/หยุดนุ่มนวล ไม่กระตุก

            transform.position = Vector3.Lerp(fromPos, startPos, t);
            transform.rotation = Quaternion.Slerp(fromRot, Quaternion.identity, t);

            yield return null;
        }

        transform.position = startPos;
        transform.rotation = Quaternion.identity;

        if (sr != null)
        {
            sr.sortingLayerName = defaultLayer;

            if (dragSprite != null)
                sr.sprite = defaultSprite;
        }

        snapMoveCoroutine = null;
    }
}