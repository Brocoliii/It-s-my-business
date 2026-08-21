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
            return;
        }

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
        if (snapMoveCoroutine != null)
        {
            StopCoroutine(snapMoveCoroutine);
            snapMoveCoroutine = null;
        }

        startDragPos = FoodRoot.position;
        isBeingDragged = true;
        SetSortingLayer(dragLayer);

        if (mainRenderer != null) mainRenderer.sortingOrder = 1;
        if (sauceRenderer != null) sauceRenderer.sortingOrder = 2;
        if (spicyRenderer != null) spicyRenderer.sortingOrder = 3;
    }

    public void OnDrag(Vector2 mousePos)
    {
        if (isBeingDragged) FoodRoot.position = mousePos;
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
        Quaternion originalRotation = transform.localRotation;

        // ทิศทางเอียงสลับกันทุกครั้งที่พลิก จะได้ไม่รู้สึกซ้ำๆ เหมือนโดนดีดไปทางเดียวตลอด
        float tiltDirection = isFacingSideA ? 1f : -1f;

        float elapsed = 0f;
        bool hasSwappedFace = false;

        while (elapsed < flipDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipDuration);
            float arc = Mathf.Sin(t * Mathf.PI); // 0 -> 1 -> 0 ตลอดช่วงพลิก ใช้ทำทั้งกระโดดขึ้นและโค้งเอียง

            // บีบความกว้างจนเหลือ 0 ตรงกลาง (มองเหมือนพลิกด้านข้าง) แล้วคลายกลับ = ภาพลวงตาการหมุน 180 องศา
            float squash = Mathf.Abs(Mathf.Cos(t * Mathf.PI));
            transform.localScale = new Vector3(originalScale.x * squash, originalScale.y, originalScale.z);

            // กระโดดขึ้นเป็นโค้งพาราโบลา ให้ดูเหมือนถูกดีดขึ้นจริงๆ ไม่ใช่แค่บีบแบนอยู่กับที่
            transform.localPosition = originalPosition + new Vector3(0f, arc * flipHopHeight, 0f);

            // เอียงเล็กน้อยระหว่างลอยขึ้น แล้วคลายกลับตรงตอนลงพื้น เสริมความรู้สึกว่ากำลังหมุนตัวอยู่
            transform.localRotation = originalRotation * Quaternion.Euler(0f, 0f, arc * flipTiltAngle * tiltDirection);

            if (!hasSwappedFace && t >= 0.5f)
            {
                Flip();
                hasSwappedFace = true;
            }

            yield return null;
        }

        transform.localScale = originalScale;
        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;

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
    }

    // ตัวควันคอยอ่านค่านี้เพื่อรู้ว่าตอนนี้ต้องพ่นควันอยู่ไหม
    public bool IsOnGrill => isOnGrill;

    public void SetGrilling(bool state) { isOnGrill = state; }

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