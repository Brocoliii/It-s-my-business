using UnityEngine;
using System.Collections;
public class FoodInstance : MonoBehaviour, IDraggable, IClickable
{
    [SerializeField] private FoodData data;
    public enum CookState { Raw, Medium, Cooked, Burnt }

    [Header("Current Progress")]
    [SerializeField] private float sideAProgress = 0f;
    [SerializeField] private float sideBProgress = 0f;
    [SerializeField] private bool isFacingSideA = true;

    [SerializeField] private float _burnTime = 10f;

    [Header("Layer Settings")]
    public string defaultLayer = "Food";
    public string dragLayer = "Dragging";

    [Header("Flip Settings")]
    // กลับด้าน B ให้ภาพเป็นกระจกเงา ผู้เล่นจะได้เห็นชัดว่าตอนนี้พลิกอีกด้านขึ้นมาแล้ว
    [SerializeField] private bool mirrorSpriteWhenFlipped = true;
    [SerializeField] private float flipDuration = 0.35f;
    [SerializeField] private float flipHopHeight = 0.18f;
    [SerializeField] private float flipTiltAngle = 12f;
    [SerializeField] private float landingSquashAmount = 0.15f;
    [SerializeField] private float landingSquashDuration = 0.08f;
    public float BurnTime => _burnTime;
    public CookState CurrentState => currentState;
    private CookState currentState = CookState.Raw;

    // ความคืบหน้าการสุกของแต่ละด้าน แยกกัน ใช้โชว์ Side A / Side B UI บนเตา
    public float SideAProgress => sideAProgress;
    public float SideBProgress => sideBProgress;
    public bool IsFacingSideA => isFacingSideA;

    // แจ้งเตือนตอนพลิกด้านจริง (กลางแอนิเมชั่น) ให้ UI ภายนอก (เช่น GrillSlotUI) เล่นเอฟเฟกต์ตามได้
    public event System.Action OnFlipped;

    [Header("Seasoning (เครื่องปรุง)")]
    public int spicyLevel = 0;
    public bool hasSauce = false;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer sauceRenderer;
    [SerializeField] private SpriteRenderer spicyRenderer;

    [Header("Shadow (โชว์เฉพาะตอนอยู่บนโต๊ะปรุงรส)")]
    [SerializeField] private GameObject shadowObject;

    [Header("Smoke (ควันตอนย่าง)")]
    [SerializeField] private bool enableGrillSmoke = true;
    [SerializeField] private FoodSmokeEffect smokeEffect;

    [Header("Drag Sway (ส่ายตามเมาส์ตอนลาก)")]
    [Tooltip("ปิดไว้ถ้าอยากให้ชิ้นนี้ลากแล้วนิ่งแข็งเหมือนเดิม")]
    [SerializeField] private bool enableDragSway = true;

    [Tooltip("องศาที่เอียงต่อความเร็วเมาส์ 1 หน่วยโลกต่อวินาที")]
    [SerializeField] private float swayAnglePerSpeed = 2.2f;

    [Tooltip("เพดานการเอียง กันไม่ให้สะบัดจนตีลังกาตอนเหวี่ยงเมาส์แรง ๆ")]
    [SerializeField] private float swayMaxAngle = 16f;

    [Tooltip("ความแข็งของสปริง ยิ่งมากยิ่งวิ่งตามเมาส์ไว")]
    [SerializeField] private float swayStiffness = 90f;

    [Tooltip("แรงหน่วง ยิ่งน้อยยิ่งแกว่งค้างหลายรอบกว่าจะนิ่ง")]
    [SerializeField] private float swayDamping = 12f;

    [Header("Snap Animation")]
    [SerializeField] private float snapMoveDuration = 0.15f;

    [Header("Tray Pickup Animation")]
    [SerializeField] private float trayPopDuration = 0.2f;
    [SerializeField] private float trayPopOvershoot = 1.15f;

    [HideInInspector] public Vector3 startDragPos;
    [HideInInspector] public GrillStation currentGrill;
    [HideInInspector] public SeasoningStation currentSeasoning;

    private bool isOnGrill = false;
    private bool isBeingDragged = false;
    private bool isFlipping = false;
    private SpriteRenderer sr;
    private SpriteRenderer shadowRenderer;
    private Transform foodRoot;
    private Coroutine snapMoveCoroutine;
    private Coroutine trayPopCoroutine;

    // ปลายทางของแอนิเมชันดูดเข้าช่องที่กำลังเล่นอยู่
    // ต้องจำไว้ เพราะถ้าผู้เล่นคว้าชิ้นนี้ระหว่างที่มันยังเลื่อนไม่ถึงช่อง
    // จุดที่ควรดีดกลับคือ "ช่องปลายทาง" ไม่ใช่ตำแหน่งกลางทางที่บังเอิญโดนคว้า
    private Vector3 snapTargetPos;
    private bool isSnapping = false;

    // ท่าตั้งต้นของภาพตอนวางนิ่ง ๆ แอนิเมชันทุกตัวเอียงจากจุดนี้ ไม่ใช่เขียนทับ localRotation ตรง ๆ
    // ไม่งั้นตัวที่เขียนทีหลังจะลบล้างตัวก่อนหน้าทิ้ง (เช่น ส่ายตอนลากไปทับเอียงตอนพลิก)
    private Quaternion baseLocalRotation = Quaternion.identity;
    private float flipTiltDegrees;
    private float swayDegrees;
    private float swayAngularVelocity;

    // ตำแหน่งเมาส์เฟรมก่อนหน้า ใช้วัดว่าลากไปเร็วแค่ไหนเพื่อแปลงเป็นองศาที่เอียง
    private Vector2 lastDragPos;
    private bool hasDragSwaySample;

    // เสียงย่างที่ดังวนอยู่ตลอดเวลาที่ชิ้นนี้อยู่บนเตา
    // ต้องเก็บ handle ไว้ เพราะเสียงวนไม่หยุดเอง ถ้าไม่มีใครสั่งหยุดมันจะดังค้างไปทั้งเกม
    private SoundHandle sizzleHandle = SoundHandle.None;

    // ระดับความสุกครั้งล่าสุดที่ส่งเสียงแจ้งไปแล้ว
    // มีไว้ให้เสียง "สุกแล้ว"/"ไหม้แล้ว" ดังครั้งเดียวตอนเปลี่ยนระดับ ไม่ใช่ดังรัวทุกเฟรม
    private CookState lastSoundedState = CookState.Raw;

    // prefab บางตัวทำเป็น Root เปล่าๆ แล้วเอา FoodInstance ไปไว้ที่ลูก เช่น Pork(Empty) > VisualPork(FoodInstance)
    // เวลาจะย้าย/ทิ้ง/ยัดลงถ้วย ต้องทำกับ "ตัวแม่" ไม่งั้น Root เปล่าจะค้างอยู่ใน Hierarchy
    public Transform FoodRoot => foodRoot != null ? foodRoot : transform;
    public GameObject RootObject => FoodRoot.gameObject;

    // ตำแหน่งของวัตถุดิบทั้งชิ้น (ไม่ใช่เฉพาะตัวภาพ) ใช้ตอนวาง/ดูดเข้าช่อง
    public Vector3 Position
    {
        get => FoodRoot.position;
        set => FoodRoot.position = value;
    }

    // ใช้ตอนดูดอาหารเข้าช่อง (เตาย่าง/โต๊ะปรุงรส) หรือดีดกลับจุดเดิม
    // จะได้ค่อยๆ เลื่อนเข้าตำแหน่งแบบนุ่มนวล ไม่ใช่การกระโดดไปทันที
    public void SnapToPosition(Vector3 targetPos)
    {
        if (snapMoveCoroutine != null) StopCoroutine(snapMoveCoroutine);

        if (!gameObject.activeInHierarchy)
        {
            Position = targetPos;
            isSnapping = false;
            return;
        }

        snapTargetPos = targetPos;
        isSnapping = true;
        snapMoveCoroutine = StartCoroutine(SmoothMoveTo(targetPos));
    }

    private IEnumerator SmoothMoveTo(Vector3 targetPos)
    {
        Vector3 start = FoodRoot.position;
        float elapsed = 0f;

        while (elapsed < snapMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapMoveDuration);
            t = t * t * (3f - 2f * t); // smoothstep เพื่อให้เริ่ม/หยุดนุ่มนวล ไม่กระตุก
            FoodRoot.position = Vector3.Lerp(start, targetPos, t);
            yield return null;
        }

        FoodRoot.position = targetPos;
        snapMoveCoroutine = null;
        isSnapping = false;
    }

    // เรียกตอนผู้เล่น "แค่คลิก" ชิ้นที่ยังจองช่องอยู่ (ไม่ได้ลากออกจากช่องจริง)
    // ระหว่างกดเมาส์ค้าง ตัวอาหารจะขยับตามเมาส์ไปเล็กน้อยเสมอ และ OnBeginDrag ก็หยุด
    // แอนิเมชันดูดเข้าช่องที่ค้างอยู่ทิ้งด้วย ถ้าไม่ดึงกลับตรงนี้ อาหารจะค้างเยื้องจากช่อง
    // แล้วสะสมทีละนิดทุกครั้งที่คลิก จนหลุดออกไปนอกช่องไปเลย
    public void ReturnToStationSlot()
    {
        if (currentGrill != null && currentGrill.TryGetSlotPosition(this, out Vector3 grillSlotPos))
        {
            SnapToPosition(grillSlotPos);
            return;
        }

        if (currentSeasoning != null && currentSeasoning.TryGetSlotPosition(this, out Vector3 seasoningSlotPos))
        {
            SnapToPosition(seasoningSlotPos);
            return;
        }

        // ไม่ได้อยู่ช่องไหนเลย (ลอยอยู่บนโต๊ะเฉยๆ) ก็ให้กลับไปจุดเดิมตอนก่อนกด
        SnapToPosition(startDragPos);
    }

    // เรียกจาก FoodTray ตอนเพิ่งหยิบวัตถุดิบออกมา ให้ตัวเนื้อผุดขึ้นมาแบบเด้งๆ แทนที่จะโผล่มาเฉยๆ
    public void PlayTrayPickupAnimation()
    {
        if (trayPopCoroutine != null) StopCoroutine(trayPopCoroutine);
        trayPopCoroutine = StartCoroutine(TrayPickupPop());
    }

    private IEnumerator TrayPickupPop()
    {
        Vector3 targetScale = transform.localScale;
        Vector3 overshootScale = targetScale * trayPopOvershoot;
        transform.localScale = Vector3.zero;

        // แบ่งสองช่วง: พุ่งขึ้นเกินขนาดจริงก่อน แล้วค่อยหดกลับมาพอดี ให้ความรู้สึกเด้งมีน้ำหนัก
        float growDuration = trayPopDuration * 0.6f;
        float settleDuration = trayPopDuration - growDuration;

        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / growDuration);
            t = t * t * (3f - 2f * t); // smoothstep
            transform.localScale = Vector3.LerpUnclamped(Vector3.zero, overshootScale, t);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < settleDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / settleDuration);
            t = t * t * (3f - 2f * t);
            transform.localScale = Vector3.Lerp(overshootScale, targetScale, t);
            yield return null;
        }

        transform.localScale = targetScale;
        trayPopCoroutine = null;
    }

    // ให้คนที่ Instantiate prefab มาบอกได้ว่าตัวแม่จริงๆ คืออันไหน (แม่นกว่าการเดาเอง)
    public void SetFoodRoot(Transform root)
    {
        if (root == null || !transform.IsChildOf(root)) return;
        foodRoot = root;
    }

    private void ResolveFoodRoot()
    {
        Transform candidate = transform;
        while (candidate.parent != null && IsFoodWrapper(candidate.parent))
        {
            candidate = candidate.parent;
        }
        foodRoot = candidate;
    }

    // ตัวห่อของวัตถุดิบ = GameObject เปล่า (มีแค่ Transform) ที่มีวัตถุดิบอยู่ข้างในชิ้นเดียว
    // เช็คสองข้อนี้กันพลาดไปนับเอา "กล่องรวมของ" หรือช่องวางบนเตาว่าเป็นตัวแม่ของวัตถุดิบ
    private static bool IsFoodWrapper(Transform target)
    {
        return target.GetComponents<Component>().Length == 1
            && target.GetComponentsInChildren<FoodInstance>(true).Length == 1;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseLocalRotation = transform.localRotation;
        if (shadowObject != null) shadowRenderer = shadowObject.GetComponent<SpriteRenderer>();
        ResolveFoodRoot();
        SetupSmokeEffect();

        // ตั้งค่าภาพตั้งแต่ Awake (ไม่ใช่ Start) เพราะตอนหยิบจากถาด เราต้องรู้ขนาดภาพจริง
        // ทันทีในเฟรมเดียวกับที่ Instantiate เพื่อคำนวณจุดกึ่งกลางให้ตรงเมาส์
        if (sauceRenderer != null) sauceRenderer.gameObject.SetActive(false);
        if (spicyRenderer != null) spicyRenderer.gameObject.SetActive(false);
        if (shadowObject != null) shadowObject.SetActive(false);
        UpdateVisual();
    }

    // เรียกจาก SeasoningStation ตอนวาง/ยกอาหารออกจากช่อง เพื่อโชว์เงาเฉพาะตอนอยู่บนโต๊ะปรุงรสเท่านั้น
    public void SetShadowVisible(bool visible)
    {
        if (shadowObject != null) shadowObject.SetActive(visible);
    }

    // ระยะเยื้องระหว่าง origin ของ object กับจุดกึ่งกลางของภาพ
    // ใช้ตอนหยิบจากถาด เพื่อให้ตัววัตถุดิบมาอยู่ "กลางเมาส์" พอดี ไม่เยื้องไปข้างๆ
    public Vector2 VisualCenterOffset
    {
        get
        {
            SpriteRenderer renderer = mainRenderer != null ? mainRenderer : sr;
            if (renderer == null || renderer.sprite == null) return Vector2.zero;

            // ใช้ sprite.bounds (local space) แทน renderer.bounds (world space)
            // เพราะตอนเพิ่ง Instantiate ออกมา ค่า renderer.bounds ยังไม่อัปเดตตามตำแหน่งใหม่
            // ทำให้คำนวณระยะเยื้องผิด แล้ววัตถุดิบไปโผล่ห่างจากเมาส์
            Vector3 localCenter = renderer.sprite.bounds.center;
            if (renderer.flipX) localCenter.x = -localCenter.x;
            if (renderer.flipY) localCenter.y = -localCenter.y;

            Vector3 worldCenter = renderer.transform.TransformPoint(localCenter);
            return (Vector2)(FoodRoot.position - worldCenter);
        }
    }

    // ระบบ IDraggable
    public void OnBeginDrag()
    {
        // ถ้าคว้าตอนที่ยังเลื่อนเข้าช่องไม่เสร็จ ให้ถือว่าจุดเดิมคือช่องปลายทาง
        // ไม่ใช่ตำแหน่งกลางทาง ไม่งั้นตอนดีดกลับมันจะกลับไปค้างกลางอากาศแทนที่จะเข้าช่อง
        startDragPos = isSnapping ? snapTargetPos : FoodRoot.position;

        if (snapMoveCoroutine != null)
        {
            StopCoroutine(snapMoveCoroutine);
            snapMoveCoroutine = null;
        }
        isSnapping = false;

        isBeingDragged = true;
        // ยังไม่รู้ว่าเมาส์อยู่ตรงไหน รอให้ UpdateDragSway เก็บตำแหน่งแรกให้ก่อน
        // ถ้าเดาเอาจากตำแหน่งตัวเอง เฟรมแรกจะได้ระยะกระโดดก้อนใหญ่ (ระยะเยื้องตอนคว้า) แล้วสะบัดแรงเกินจริง
        hasDragSwaySample = false;
        SetSortingLayer(dragLayer);

        // เสียงหยิบไม่ได้อยู่ตรงนี้ เพราะ OnBeginDrag ดังทุกครั้งที่เริ่มลาก
        // (รวมตอนหยิบของที่วางบนเตา/โต๊ะปรุงรสอยู่แล้ว) เสียงจริงย้ายไปอยู่ที่ FoodTray
        // ให้ดังเฉพาะตอนหยิบวัตถุดิบใหม่ออกจากถาดเท่านั้น

        if (mainRenderer != null) mainRenderer.sortingOrder = 1;
        if (sauceRenderer != null) sauceRenderer.sortingOrder = 2;
        if (spicyRenderer != null) spicyRenderer.sortingOrder = 3;
    }

    public void OnDrag(Vector2 mousePos)
    {
        if (isBeingDragged && !IsBeingStolen) FoodRoot.position = mousePos;
    }

    // ลากไปทางไหน ชิ้นเนื้อก็ถ่วงเอียงไปทางตรงข้ามเหมือนถูกลากผ่านอากาศ แล้วแกว่งกลับเองตอนเมาส์หยุด
    //
    // ต้องรับ "ตำแหน่งเมาส์ดิบ" ไม่ใช่ตำแหน่งที่ตัวอาหารจะไปวาง เพราะ InputManager คิดระยะเยื้อง
    // จาก VisualCenterOffset ใหม่ทุกเฟรม ซึ่งขยับตามมุมที่เราเพิ่งเอียงไป ถ้าเอาค่านั้นมาวัดความเร็ว
    // การส่ายจะกลายเป็นตัวป้อนความเร็วให้ตัวเอง แล้วสั่นเพี้ยนไปเรื่อย ๆ
    public void UpdateDragSway(Vector2 pointerWorldPos)
    {
        if (!enableDragSway || !isBeingDragged) return;

        if (!hasDragSwaySample)
        {
            lastDragPos = pointerWorldPos;
            hasDragSwaySample = true;
            return;
        }

        // dt ตอนเฟรมกระตุก (โหลดของ/สลับหน้าต่าง) อาจก้อนใหญ่มากจนสปริงคำนวณเกินตัวแล้วดีดกระจุย
        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;

        float horizontalSpeed = (pointerWorldPos.x - lastDragPos.x) / dt;
        lastDragPos = pointerWorldPos;

        // ลากไปขวา ชิ้นเนื้อต้องเอียงทวนไปทางซ้าย (ติดลบ) เหมือนโดนลมต้าน
        float targetAngle = Mathf.Clamp(-horizontalSpeed * swayAnglePerSpeed, -swayMaxAngle, swayMaxAngle);
        StepSwaySpring(targetAngle, dt);
    }

    // สปริงหน่วง: ดึงเข้าหามุมเป้าหมายแต่มีมวล เลยตามมาช้า ๆ และเลยเป้าไปนิดก่อนจะนิ่ง
    private void StepSwaySpring(float targetAngle, float dt)
    {
        swayAngularVelocity += (-swayStiffness * (swayDegrees - targetAngle) - swayDamping * swayAngularVelocity) * dt;
        swayDegrees += swayAngularVelocity * dt;
        ApplyVisualRotation();
    }

    private void ApplyVisualRotation()
    {
        transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, flipTiltDegrees + swayDegrees);
    }

    public void OnEndDrag()
    {
        isBeingDragged = false;
        SetSortingLayer(defaultLayer);
    }
    // Click
    public void OnClick()
    {
        if (isOnGrill && !isFlipping)
        {
            StartCoroutine(FlipAnimation());
        }
    }

    private void SetSortingLayer(string layerName)
    {
        if (mainRenderer != null) mainRenderer.sortingLayerName = layerName;
        if (sauceRenderer != null) sauceRenderer.sortingLayerName = layerName;
        if (spicyRenderer != null) spicyRenderer.sortingLayerName = layerName;
    }
    private IEnumerator FlipAnimation()
    {
        isFlipping = true;

        Vector3 originalScale = transform.localScale;
        Vector3 originalPosition = transform.localPosition;

        // ทิศทางเอียงสลับกันทุกครั้งที่พลิก จะได้ไม่รู้สึกซ้ำๆ เหมือนโดนดีดไปทางเดียวตลอด
        float tiltDirection = isFacingSideA ? 1f : -1f;

        float elapsed = 0f;
        bool hasSwappedFace = false;

        // ตำแหน่งที่แอนิเมชันนี้เขียนลงไปเองครั้งล่าสุด
        // ถ้าเฟรมถัดมาอ่านได้ไม่ตรงกับค่านี้ แปลว่ามีคนอื่น (แอนิเมชันดูดเข้าช่อง) ย้ายมันระหว่างทาง
        Vector3 lastWrittenPosition = originalPosition;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);
            float arc = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0 ตลอดช่วงพลิก ใช้ทำทั้งกระโดดขึ้นและโค้งเอียง

            // บีบความกว้างจนเหลือ 0 ตรงกลาง (มองเหมือนพลิกด้านข้าง) แล้วคลายกลับ = ภาพลวงตาการหมุน 180 องศา
            float squash = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
            transform.localScale = new Vector3(originalScale.x * squash, originalScale.y, originalScale.z);

            // ย้ายจุดอ้างอิงตามคนอื่นที่ขยับตัวอาหารระหว่างพลิก
            // ไม่งั้นตอนจบแอนิเมชันจะดีดกลับไปจุดเดิมตอนเริ่มพลิก แล้วลบล้างการดูดเข้าช่องทิ้งทั้งหมด
            originalPosition += transform.localPosition - lastWrittenPosition;

            // กระโดดขึ้นเป็นโค้งพาราโบลา ให้ดูเหมือนถูกดีดขึ้นจริงๆ ไม่ใช่แค่บีบแบนอยู่กับที่
            transform.localPosition = originalPosition + new Vector3(0f, arc * flipHopHeight, 0f);
            lastWrittenPosition = transform.localPosition;

            // เอียงเล็กน้อยระหว่างลอยขึ้น แล้วคลายกลับตรงตอนลงพื้น เสริมความรู้สึกว่ากำลังหมุนตัวอยู่
            flipTiltDegrees = arc * flipTiltAngle * tiltDirection;
            ApplyVisualRotation();

            if (!hasSwappedFace && t >= 0.5f)
            {
                Flip();
                hasSwappedFace = true;
            }

            yield return null;
        }

        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        flipTiltDegrees = 0f;
        ApplyVisualRotation();

        yield return StartCoroutine(LandingSquash(originalScale));

        isFlipping = false;
    }

    // บีบแป้นเนื้อแบนลงแวบเดียวตอนตกกระทบเตา แล้วเด้งกลับ ให้รู้สึกถึงน้ำหนักตอนลงพื้น
    private IEnumerator LandingSquash(Vector3 baseScale)
    {
        float elapsed = 0f;

        while (elapsed < landingSquashDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / landingSquashDuration);
            float squash = Mathf.Sin(t * Mathf.PI) * landingSquashAmount;

            transform.localScale = new Vector3(
                baseScale.x * (1f + squash),
                baseScale.y * (1f - squash),
                baseScale.z);

            yield return null;
        }

        transform.localScale = baseScale;
    }

    // ระบบทำอาหาร
    public void Flip()
    {
        isFacingSideA = !isFacingSideA;
        UpdateVisual();
        OnFlipped?.Invoke();

        SoundManager.Instance.PlaySFX(SoundIds.FoodFlip, FoodRoot.position);
    }

    // ตัวควันคอยอ่านค่านี้เพื่อรู้ว่าตอนนี้ต้องพ่นควันอยู่ไหม
    public bool IsOnGrill => isOnGrill;
    public bool IsBeingStolen { get; private set; }

    public void SetBeingStolen(bool state)
    {
        IsBeingStolen = state;
    }

    public void SetGrilling(bool state)
    {
        // กันสั่งค่าเดิมซ้ำ ไม่งั้นจะไปเปิดเสียงย่างซ้อนกันหลายชั้นบนชิ้นเดียว
        if (isOnGrill == state) return;

        isOnGrill = state;

        if (state)
        {
            // ผูกเสียงไว้กับตัวอาหาร เสียงจะได้เลื่อนตามตอนผู้เล่นลากชิ้นนี้ไปมา
            sizzleHandle = SoundManager.Instance.PlayLoop(SoundIds.GrillSizzle, FoodRoot);
        }
        else
        {
            sizzleHandle.Stop(0.2f);
            sizzleHandle = SoundHandle.None;
        }
    }

    private void OnDestroy()
    {
        // ชิ้นนี้อาจถูกทิ้งลงถังขยะหรือยัดลงถ้วยตอนที่ยังย่างอยู่
        // ถ้าไม่หยุดตรงนี้ เสียงย่างจะดังค้างต่อทั้งที่ไม่มีอะไรอยู่บนเตาแล้ว
        sizzleHandle.Stop(0.2f);
        sizzleHandle = SoundHandle.None;
    }

    // หาตัวพ่นควันที่ติดมากับ prefab ก่อน ถ้าไม่มีก็ใส่ให้เอง จะได้ไม่ต้องไปเซ็ต prefab ทีละชิ้น
    private void SetupSmokeEffect()
    {
        if (!enableGrillSmoke) return;

        if (smokeEffect == null) smokeEffect = FoodRoot.GetComponentInChildren<FoodSmokeEffect>(true);
        if (smokeEffect == null) smokeEffect = gameObject.AddComponent<FoodSmokeEffect>();

        smokeEffect.SetFood(this);
    }

    private void Update()
    {
        if (isOnGrill && !isBeingDragged)
        {
            UpdateCooking();
        }

        SettleDragSway();
    }

    // ปล่อยมือแล้วยังเอียงค้างอยู่ ให้แกว่งกลับเข้าที่เองต่อ (สปริงตัวเดิม แค่เปลี่ยนเป้าเป็น 0 องศา)
    private void SettleDragSway()
    {
        if (isBeingDragged) return;
        if (swayDegrees == 0f && swayAngularVelocity == 0f) return;

        float dt = Mathf.Min(Time.deltaTime, 0.05f);
        if (dt <= 0f) return;

        StepSwaySpring(0f, dt);

        // ใกล้ศูนย์แล้วก็ตัดจบ ไม่ปล่อยให้สั่นเศษ ๆ ค้างไว้กินเฟรมไปเรื่อย
        if (Mathf.Abs(swayDegrees) < 0.01f && Mathf.Abs(swayAngularVelocity) < 0.01f)
        {
            swayDegrees = 0f;
            swayAngularVelocity = 0f;
            ApplyVisualRotation();
        }
    }

    private void UpdateCooking()
    {
        if (isFacingSideA)
        {
            sideBProgress += Time.deltaTime;
        }
        else
        {
            sideAProgress += Time.deltaTime;
        }

        UpdateVisual();
        CheckCookStateSound();
    }

    // ส่งเสียงบอกตอนความสุกข้ามระดับ ผู้เล่นจะได้รู้ตัวโดยไม่ต้องจ้องหมูทุกชิ้นตลอดเวลา
    private void CheckCookStateSound()
    {
        if (data == null) return;

        CookState state = GetCurrentState();
        if (state == lastSoundedState) return;

        lastSoundedState = state;

        if (state == CookState.Cooked)
        {
            SoundManager.Instance.PlaySFX(SoundIds.FoodCooked, FoodRoot.position);
        }
        else if (state == CookState.Burnt)
        {
            SoundManager.Instance.PlaySFX(SoundIds.FoodBurnt, FoodRoot.position);
        }
    }

    private void UpdateVisual()
    {
        ApplyFacingFlip();

        if (sr == null || data == null) return;


        float currentFaceProgress = isFacingSideA ? sideAProgress : sideBProgress;

        if (currentFaceProgress >= data.burnTime) sr.sprite = data.burntSprite;
        else if (currentFaceProgress >= data.cookTime) sr.sprite = data.cookedSprite;
        else if (currentFaceProgress >= data.mediumTime) sr.sprite = data.mediumSprite;
        else sr.sprite = data.rawSprite;
    }

    // พลิกภาพทุกชั้น (ตัวเนื้อ + ซอส + พริก) ให้ไปทางเดียวกัน ไม่งั้น overlay จะค้างอยู่ด้านเดิม
    private void ApplyFacingFlip()
    {
        bool mirrored = mirrorSpriteWhenFlipped && !isFacingSideA;

        SetRendererFlip(sr, mirrored);
        SetRendererFlip(mainRenderer, mirrored);
        SetRendererFlip(sauceRenderer, mirrored);
        SetRendererFlip(spicyRenderer, mirrored);
        SetRendererFlip(shadowRenderer, mirrored);
    }

    private static void SetRendererFlip(SpriteRenderer renderer, bool mirrored)
    {
        if (renderer != null) renderer.flipX = mirrored;
    }
    public CookState GetCurrentState()
    {
        if (sideAProgress > data.burnTime || sideBProgress > data.burnTime) return CookState.Burnt;
        if (sideAProgress >= data.cookTime && sideBProgress >= data.cookTime) return CookState.Cooked;
        if (sideAProgress >= data.mediumTime || sideBProgress >= data.mediumTime) return CookState.Medium;
        return CookState.Raw;
    }
    //ซอส
    // ดิบ/ไหม้ ปรุงรสไม่ได้ ต้องผ่านการย่างให้สุกระดับหนึ่งก่อน ทั้งสองด้าน ไม่ใช่แค่ด้านใดด้านหนึ่ง
    public bool CanSeason()
    {
        if (sideAProgress > data.burnTime || sideBProgress > data.burnTime) return false;
        return sideAProgress >= data.mediumTime && sideBProgress >= data.mediumTime;
    }
    public void AddSpicy()
    {
        if (!CanSeason()) return;

        if (spicyLevel < 3)
        {
            spicyLevel++;
            UpdateSpicyVisual();

            SoundManager.Instance.PlaySFX(SoundIds.ChiliShake, FoodRoot.position);
        }
    }
    public void ApplySauce()
    {
        if (!CanSeason()) return;

        if (!hasSauce)
        {
            hasSauce = true;
            if (sauceRenderer != null && data.sauceSprite != null)
            {
                sauceRenderer.sprite = data.sauceSprite;
                sauceRenderer.gameObject.SetActive(true);
                // Preview may have pushed sauceRenderer above spicyRenderer to crossfade the merged sprite in;
                // once applied, spicyRenderer itself switches to the merged sprite, so sauce goes back underneath.
                if (spicyRenderer != null) sauceRenderer.sortingOrder = spicyRenderer.sortingOrder - 1;
                SetSauceAlpha(1f);
            }
            else
            {
                Debug.LogError("ลืมลาก Overlay_Sauce มาใส่ใน Inspector");
            }

            // เสียงตอนซอสติดสำเร็จ คนละตัวกับ sauce_brush ที่ SauceBrush เล่นให้ทุกจังหวะที่ปาด
            // จังหวะสุดท้าย SauceBrush จะข้ามเสียงปาดให้ ตรงนี้เลยได้ดังเดี่ยว ๆ ไม่ทับกัน
            SoundManager.Instance.PlaySFX(SoundIds.SauceDone, FoodRoot.position);

            // ซอสมาทีหลังพริกได้ ต้องรีเฟรชภาพพริกให้เป็นเวอร์ชันผสมซอสด้วย
            if (spicyLevel > 0) UpdateSpicyVisual();
        }
    }

    // เรียกระหว่างแปรงกำลังลากอยู่ เพื่อค่อยๆ ให้ซอสจางๆ ปรากฏขึ้นตามตำแหน่งที่แปรงผ่าน
    // ก่อนซอสจะติดจริงตอนแปรงครบจำนวนครั้งที่กำหนด
    public void SetSaucePreview(float progress)
    {
        if (hasSauce || sauceRenderer == null || data == null) return;

        Sprite previewSprite = GetSaucePreviewSprite();
        if (previewSprite == null) return;

        if (progress <= 0f)
        {
            sauceRenderer.gameObject.SetActive(false);
            return;
        }

        sauceRenderer.sprite = previewSprite;
        sauceRenderer.gameObject.SetActive(true);
        SetSauceAlpha(Mathf.Clamp01(progress));

        // Crossfade the merged sprite in over the plain spicy overlay (which stays put underneath)
        // instead of popping the plain sprite away and fading up from nothing.
        if (spicyLevel > 0 && spicyRenderer != null) sauceRenderer.sortingOrder = spicyRenderer.sortingOrder + 1;
    }

    private Sprite GetSaucePreviewSprite()
    {
        if (spicyLevel <= 0) return data.sauceSprite;
        if (spicyLevel == 1) return data.sauceSpicyLevel1Sprite;
        if (spicyLevel == 2) return data.sauceSpicyLevel2Sprite;
        return data.sauceSpicyLevel3Sprite;
    }

    private void SetSauceAlpha(float alpha)
    {
        Color c = sauceRenderer.color;
        c.a = alpha;
        sauceRenderer.color = c;
    }

    private void UpdateSpicyVisual()
    {
        if (spicyRenderer == null) return;

        spicyRenderer.gameObject.SetActive(true);

        if (hasSauce)
        {
            if (spicyLevel == 1) spicyRenderer.sprite = data.sauceSpicyLevel1Sprite;
            else if (spicyLevel == 2) spicyRenderer.sprite = data.sauceSpicyLevel2Sprite;
            else if (spicyLevel >= 3) spicyRenderer.sprite = data.sauceSpicyLevel3Sprite;
        }
        else
        {
            if (spicyLevel == 1) spicyRenderer.sprite = data.spicyLevel1Sprite;
            else if (spicyLevel == 2) spicyRenderer.sprite = data.spicyLevel2Sprite;
            else if (spicyLevel >= 3) spicyRenderer.sprite = data.spicyLevel3Sprite;
        }
    }
    public FoodData GetData()
    {
        return data;
    }
    public float CurrentCookTimer
    {
        get
        {
            return isFacingSideA ? sideBProgress : sideAProgress;
        }
    }

    // ปุ่ม Debug: บังคับให้สุกทันทีทั้งสองด้าน (ใช้ตอนทดสอบสถานีปรุงรส)
    public void ForceCook()
    {
        if (data == null) return;

        sideAProgress = data.cookTime;
        sideBProgress = data.cookTime;
        UpdateVisual();
    }

}