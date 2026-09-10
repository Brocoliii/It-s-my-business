using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Cup : MonoBehaviour, IDraggable
{
    [Header("ข้อมูลอาหารในถ้วย")]
    public List<FoodInstanceData> contents = new List<FoodInstanceData>();

    [Header("🎨 ระบบเลเยอร์ (เปลี่ยนเฉพาะถ้วย)")]
    public string sortingLayerName = "Default";
    public int defaultSortingOrder = 5;
    public int dragSortingOrder = 20;

    [Header("📏 ปรับแต่งภาพอาหารตอนลงถ้วย")]
    public float inCupScale = 0.5f;
    public float inCupRotationZ = 0f;
    public float inCupRotationRandomRange = 10f;
    public float baseHeightOffset = 0.6f;
    public float stackHeightSpacing = 0.2f;
    public float dropHeight = 1.5f;
    public float rotateDuration = 0.15f;
    public float dropDuration = 0.25f;

    [Header("Spawn Animation (ตอนถ้วยเพิ่งเกิดขึ้นมา)")]
    [SerializeField] private bool playSpawnAnimation = true;
    [SerializeField] private float spawnDuration = 0.3f;
    [SerializeField] private float spawnDropHeight = 0.6f;
    [SerializeField] private float spawnOvershoot = 0.2f;
    [Tooltip("สัดส่วนเวลาของช่วง \"ลอยขึ้น\" เทียบกับเวลารวมทั้งหมด ที่เหลือคือช่วง \"ลากไปวาง\" ที่ cupArea")]
    [Range(0.05f, 0.95f)]
    [SerializeField] private float spawnRiseRatio = 0.35f;
    [Tooltip("สไปรท์ที่ใช้ตอนถ้วยกำลังลอย/ลากไป cupArea (ถ้าไม่ตั้งค่าไว้จะใช้สไปรท์ปกติ)")]
    [SerializeField] private Sprite spawnFlySprite;

    [Header("Drag Sprite")]
    [SerializeField] private Sprite dragSprite;

    [Header("Snap Back")]
    [SerializeField] private float snapMoveDuration = 0.2f;

    [Header("Drop Area")]
    [SerializeField] private Collider2D dropArea;
    [SerializeField] private float dropPadding = 0.2f;

    private SpriteRenderer sr;
    private Vector3 startPos;
    private Sprite defaultSprite;
    private bool spawnAnimationTriggered = false;
    private Coroutine snapMoveCoroutine;
    private Coroutine spawnAnimationCoroutine;
    private Vector3 spawnAnimationScale;

    [System.Serializable]
    public struct FoodInstanceData
    {
        public FoodData data;
        public int spicy;
        public bool sauce;
        public FoodInstance.CookState state;
    }

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) defaultSprite = sr.sprite;

        if (dropArea == null) dropArea = GetComponent<Collider2D>();
        if (dropArea == null) dropArea = GetComponentInChildren<Collider2D>();
    }

    private void Start()
    {
        // ถ้า PlaySpawnAnimation ถูกเรียกมาก่อนแล้ว (เช่นจาก CupSpawner) จะได้ตั้ง startPos = cupArea ไว้แล้ว
        // ห้ามทับด้วย transform.position ตรงนี้ เพราะตอนนี้ตัวถ้วยถูกย้ายไปอยู่ที่ spawnPoint ชั่วคราวระหว่างเล่น animation อยู่
        if (!spawnAnimationTriggered)
        {
            startPos = transform.position;
        }

        ApplySortingSettings(defaultSortingOrder);

        // ถ้าไม่มีใครเรียก PlaySpawnAnimation มาก่อน (เช่น ถ้วยที่วางไว้ในฉากตั้งแต่แรก ไม่ได้เกิดจาก Spawner)
        // ให้เล่นแบบร่วงลงมาตรงๆ ที่ตำแหน่งตัวเอง แทนที่จะไม่มี animation เลย
        if (playSpawnAnimation && !spawnAnimationTriggered)
        {
            spawnAnimationCoroutine = StartCoroutine(DropAnimation(startPos + Vector3.up * spawnDropHeight, startPos));
        }
    }

    // เรียกจากภายนอก (เช่น CupSpawner) ตอนอยากให้ถ้วยโผล่ที่จุดหนึ่ง (จุดเกิด) แล้วลอยขึ้น/ลากไปตกที่อีกจุดหนึ่ง (พื้นที่วางถ้วยจริง)
    // ต้องเรียกทันทีหลัง Instantiate ในเฟรมเดียวกัน (ก่อน Start ของถ้วยทำงาน) ไม่งั้นจะชนกับ animation อัตโนมัติใน Start()
    public void PlaySpawnAnimation(Vector3 fromPosition, Vector3 toPosition)
    {
        spawnAnimationTriggered = true;
        startPos = toPosition;
        spawnAnimationCoroutine = StartCoroutine(RiseAndDragAnimation(fromPosition, toPosition));
    }

    // ตัดจบแอนิเมชันตอนเกิดกลางคัน (เช่น ผู้เล่นคว้าถ้วยตั้งแต่มันยังลอยไปไม่ถึง cupArea)
    // ต้องคืนค่าที่แอนิเมชันแอบเปลี่ยนไว้ให้ครบเอง ทั้งขนาดที่กำลังไล่อยู่และสไปรท์ตอนลอย
    // ไม่งั้นถ้วยจะค้างเป็นสไปรท์ตอนลอย/ขนาดผิด แถมโดนแย่งตำแหน่งกับการลากไปจนกว่าแอนิเมชันจะจบเอง
    private void CancelSpawnAnimation()
    {
        if (spawnAnimationCoroutine == null) return;

        StopCoroutine(spawnAnimationCoroutine);
        spawnAnimationCoroutine = null;

        transform.localScale = spawnAnimationScale;

        if (sr != null && spawnFlySprite != null)
            sr.sprite = defaultSprite;
    }

    // ตกลงมาจาก fromPosition แล้วเด้งเกินขนาดจริงเล็กน้อยตอนถึง toPosition (ใช้กับถ้วยที่วางไว้ในฉากเองตั้งแต่แรก ไม่ได้เกิดจาก Spawner)
    private IEnumerator DropAnimation(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 targetScale = transform.localScale;
        spawnAnimationScale = targetScale;

        transform.localScale = Vector3.zero;
        transform.position = fromPosition;

        float elapsed = 0f;
        while (elapsed < spawnDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / spawnDuration);

            // เด้งโตเกินขนาดจริงแวบเดียวกลางทาง (sin โค้งขึ้นแล้วลง) แล้วค่อยหดกลับสู่ขนาดปกติตอนจบ
            float overshoot = 1f + Mathf.Sin(t * Mathf.PI) * spawnOvershoot;
            transform.localScale = targetScale * (t * overshoot);

            float dropT = 1f - Mathf.Pow(1f - t, 3f); // ease-out: เคลื่อนเร็วตอนแรก ช้าลงตอนใกล้ถึงพื้น เหมือนโดนเบรก
            transform.position = Vector3.Lerp(fromPosition, toPosition, dropT);

            yield return null;
        }

        transform.localScale = targetScale;
        transform.position = toPosition;

        spawnAnimationCoroutine = null;
    }

    // เกิดที่ fromPosition แล้วลอยขึ้นตรงๆ ก่อน (เหมือนถูกหยิบขึ้นมา) จากนั้นค่อยลากข้ามไปวางที่ toPosition (cupArea)
    // แยกเป็น 2 เฟส แทนที่จะเป็นเส้นตรงเฉียงๆ จาก spawnPoint ไป cupArea เลย
    private IEnumerator RiseAndDragAnimation(Vector3 fromPosition, Vector3 toPosition)
    {
        Vector3 targetScale = transform.localScale;
        spawnAnimationScale = targetScale;
        Vector3 risePosition = fromPosition + Vector3.up * spawnDropHeight;

        transform.localScale = targetScale;
        transform.position = fromPosition;

        if (sr != null && spawnFlySprite != null)
            sr.sprite = spawnFlySprite;

        float riseDuration = spawnDuration * spawnRiseRatio;
        float dragDuration = spawnDuration - riseDuration;

        // เฟส 1: ลอยขึ้นตรงๆ ที่ขนาดปกติ ที่จุดเกิด (spawnPoint) ยังไม่ขยับไปทางไหน
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / riseDuration);

            float riseT = 1f - Mathf.Pow(1f - t, 2f); // ease-out: พุ่งขึ้นเร็วตอนแรก ช้าลงตอนใกล้จุดสูงสุด
            transform.position = Vector3.Lerp(fromPosition, risePosition, riseT);

            yield return null;
        }
        transform.position = risePosition;

        // เฟส 2: ลากจากจุดที่ลอยขึ้นมา ข้ามไปวางที่ cupArea ที่ขนาดปกติตลอด
        elapsed = 0f;
        while (elapsed < dragDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dragDuration);

            float dragT = t * t * (3f - 2f * t); // ease-in-out: ค่อยๆ เร่งแล้วค่อยๆ เบรกตอนใกล้ถึง cupArea
            transform.position = Vector3.Lerp(risePosition, toPosition, dragT);

            yield return null;
        }

        transform.position = toPosition;

        if (sr != null && spawnFlySprite != null)
            sr.sprite = defaultSprite;

        spawnAnimationCoroutine = null;
    }

    private void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        ApplySortingSettings(defaultSortingOrder);
    }

    // ระยะเยื้องระหว่าง origin ของถ้วยกับจุดกึ่งกลางของภาพถ้วย
    // ใช้ตอนเริ่มลาก เพื่อให้ถ้วยมาเกาะ "กลางเมาส์" พอดี ไม่ว่าจะคลิกโดนตรงขอบไหนของถ้วยก็ตาม
    // (pivot ของ prefab มักอยู่ที่ก้นถ้วย ถ้าใช้ pivot ตรงๆ ถ้วยจะห้อยอยู่ใต้เมาส์)
    public Vector2 VisualCenterOffset
    {
        get
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return Vector2.zero;

            // ใช้ sprite.bounds (local space) แทน renderer.bounds (world space)
            // เพราะตอนเพิ่งสลับเป็น dragSprite ค่า renderer.bounds ยังไม่อัปเดตในเฟรมนั้น
            Vector3 localCenter = sr.sprite.bounds.center;
            if (sr.flipX) localCenter.x = -localCenter.x;
            if (sr.flipY) localCenter.y = -localCenter.y;

            Vector3 worldCenter = sr.transform.TransformPoint(localCenter);
            return (Vector2)(transform.position - worldCenter);
        }
    }

    // เขตที่ถือว่า "ปล่อยอาหารลงถ้วย" ได้ กว้างกว่า collider ของถ้วยเองตามค่า dropPadding
    // เพราะ collider ถ้วยถูกใช้คู่กับการลากถ้วย เลยขยายให้ใหญ่เกินตัวถ้วยไม่ได้
    // ถ้าไม่มีเขตนี้ ผู้เล่นต้องเล็งให้เมาส์โดนตัวถ้วยพอดี ทั้งที่ภาพอาหารทับถ้วยอยู่แล้ว
    public bool IsWithinDropArea(Vector2 worldPos)
    {
        if (dropArea == null)
        {
            return Vector2.Distance(worldPos, transform.position) <= 1f;
        }

        // เทียบเฉพาะระนาบ XY เท่านั้น ห้ามใช้ bounds.Contains ตรงๆ
        // เพราะ Collider2D.bounds หนาแกน z เป็นศูนย์ และวางอยู่ที่ค่า z ของ transform
        // ส่วนจุดที่ส่งเข้ามาถูกตัด z ทิ้งเป็น 0 เสมอ (มาจาก Vector2)
        // ถ้วยที่ถูกดันไปที่ z อื่นเพื่อจัดเลเยอร์ภาพ จึงไม่มีวันอยู่ในเขตวางเลยสักครั้ง
        Bounds bounds = dropArea.bounds;
        return worldPos.x >= bounds.min.x - dropPadding
            && worldPos.x <= bounds.max.x + dropPadding
            && worldPos.y >= bounds.min.y - dropPadding
            && worldPos.y <= bounds.max.y + dropPadding;
    }

    public void AddFood(FoodInstance food)
    {
        FoodInstance.CookState cookState = food.GetCurrentState();

        // ปรุงรสได้ (มีซอสหรือระดับความเผ็ด) แปลว่าสุกในระดับที่ใช้ได้แล้ว
        // พอลงถ้วยให้นับเป็นสุกปกติเลย ไม่งั้นจะค้างสถานะ Medium ทั้งที่ปรุงรสไปแล้ว
        if (food.hasSauce || food.spicyLevel > 0)
        {
            cookState = FoodInstance.CookState.Cooked;
        }

        FoodInstanceData newData = new FoodInstanceData
        {
            data = food.GetData(),
            spicy = food.spicyLevel,
            sauce = food.hasSauce,
            state = cookState
        };
        contents.Add(newData);
        StartCoroutine(SuckFoodIntoCup(food));
    }

    private IEnumerator SuckFoodIntoCup(FoodInstance food)
    {
        // ย้ายทั้งชิ้น (เผื่อ prefab เป็น root เปล่าครอบตัวภาพไว้) ไม่งั้น root เปล่าจะค้างอยู่นอกถ้วย
        Transform foodObj = food.FoodRoot;
        foreach (Collider2D col in foodObj.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foodObj.SetParent(this.transform);

        float elapsed = 0;

        Vector3 startScale = foodObj.localScale;
        Quaternion startRotation = foodObj.localRotation;

        int itemIndex = contents.Count - 1;
        Vector3 targetScale = new Vector3(inCupScale, inCupScale, 1f);
        float randomRotationZ = inCupRotationZ + Random.Range(-inCupRotationRandomRange, inCupRotationRandomRange);
        Quaternion targetRotation = Quaternion.Euler(0, 0, randomRotationZ);

        float randomX = Random.Range(-0.05f, 0.05f);
        Vector3 targetLocalPos = new Vector3(randomX, baseHeightOffset + (itemIndex * stackHeightSpacing), 0);

        // ให้อาหารเริ่มจากด้านบนถ้วยก่อน แล้วค่อยๆ ตกลงมาด้านล่างเข้าไปในถ้วย
        // แทนที่จะวาร์ปเป็นเส้นตรงจากจุดที่ปล่อยเมาส์ ไม่ว่าจะปล่อยตรงไหนบนถ้วยก็ตาม
        Vector3 startLocalPos = targetLocalPos + Vector3.up * dropHeight;
        foodObj.localPosition = startLocalPos;

        // GetComponentsInChildren (พหูพจน์) เพราะอาหารมีได้หลายเลเยอร์ภาพ (หลัก/ซอส/พริก)
        // ถ้าจับแค่ตัวเดียวจะเหลือบางเลเยอร์ค้างอยู่ sortingLayer เดิม (เช่น "Dragging") แล้วโผล่ผิดที่
        SpriteRenderer[] foodRenderers = foodObj.GetComponentsInChildren<SpriteRenderer>(true);
        if (foodRenderers.Length > 0 && sr != null)
        {
            int baseOrder = sr.sortingOrder + 1 + itemIndex;
            for (int i = 0; i < foodRenderers.Length; i++)
            {
                foodRenderers[i].sortingLayerName = sortingLayerName;
                foodRenderers[i].sortingOrder = baseOrder + i;
            }
        }

        // เฟส 1: หมุนให้เสร็จก่อน ระหว่างนี้ตำแหน่งยังค้างอยู่ด้านบนถ้วย ยังไม่ตก
        elapsed = 0;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotateDuration);
            foodObj.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }
        foodObj.localRotation = targetRotation;

        // เฟส 2: หมุนเสร็จแล้วค่อยปล่อยให้ตกลงมาเข้าถ้วย
        elapsed = 0;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            float fallT = t * t; // เร่งความเร็วขึ้นเรื่อยๆ ให้เหมือนตกตามแรงโน้มถ่วง

            foodObj.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, fallT);
            foodObj.localScale = Vector3.Lerp(startScale, targetScale, t);
            yield return null;
        }

        foodObj.localPosition = targetLocalPos;
        foodObj.localScale = targetScale;
        foodObj.localRotation = targetRotation;

        // ดังตอนตกถึงที่แล้วเท่านั้น ถ้าไปเล่นตั้งแต่ AddFood จะได้ยินก่อนภาพตกลงถ้วยจริงประมาณ 0.4 วินาที
        SoundManager.Instance.PlaySFX(SoundIds.FoodIntoCup, foodObj.position);
    }

    public void ClearCup()
    {
        contents.Clear();
        foreach (Transform child in transform) Destroy(child.gameObject);
    }

    public void OnBeginDrag()
    {
        //ApplySortingSettings(dragSortingOrder);

        // คว้าถ้วยตั้งแต่มันยังลอยจากจุดเกิดไปไม่ถึงที่วาง ต้องตัดจบแอนิเมชันเกิดก่อน
        // ไม่งั้นมันจะแย่งคุมตำแหน่งกับเมาส์ แล้วตอนจบยังสลับสไปรท์ทับ dragSprite ที่เพิ่งตั้งไว้อีก
        CancelSpawnAnimation();

        // เผื่อคว้าถ้วยขึ้นมาอีกครั้งระหว่างที่มันกำลังลอยกลับตำแหน่งเดิมอยู่ ต้องหยุด animation เก่าก่อน ไม่งั้นจะแย่งกันควบคุมตำแหน่ง
        if (snapMoveCoroutine != null)
        {
            StopCoroutine(snapMoveCoroutine);
            snapMoveCoroutine = null;
        }

        if (sr != null && dragSprite != null)
            sr.sprite = dragSprite;
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;
    }

    public void OnEndDrag()
    {
        if (snapMoveCoroutine != null)
            StopCoroutine(snapMoveCoroutine);

        if (!gameObject.activeInHierarchy)
        {
            transform.position = startPos;
            if (sr != null && dragSprite != null)
                sr.sprite = defaultSprite;
            return;
        }

        snapMoveCoroutine = StartCoroutine(SmoothSnapBack());
    }

    // ลอยนุ่มๆ กลับไปตำแหน่งตั้งต้น แทนที่จะกระโดดวาร์ปทันที แล้วค่อยสลับ sprite กลับตอนถึงที่แล้ว
    private IEnumerator SmoothSnapBack()
    {
        Vector3 fromPos = transform.position;
        float elapsed = 0f;

        while (elapsed < snapMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapMoveDuration);
            t = t * t * (3f - 2f * t); // smoothstep เพื่อให้เริ่ม/หยุดนุ่มนวล ไม่กระตุก

            transform.position = Vector3.Lerp(fromPos, startPos, t);

            yield return null;
        }

        transform.position = startPos;
        //ApplySortingSettings(defaultSortingOrder);

        if (sr != null && dragSprite != null)
            sr.sprite = defaultSprite;

        snapMoveCoroutine = null;
    }

    private void ApplySortingSettings(int order)
    {
        if (sr != null)
        {
            sr.sortingLayerName = sortingLayerName;
            sr.sortingOrder = order;
        }

        int index = 1;
        foreach (Transform child in transform)
        {
            // อาหารบางชิ้นเป็น root เปล่าครอบตัวภาพไว้ ต้องมองเข้าไปข้างในด้วย
            SpriteRenderer childSr = child.GetComponentInChildren<SpriteRenderer>();
            if (childSr != null)
            {
                childSr.sortingOrder = order + index;
                index++;
            }
        }
    }
}