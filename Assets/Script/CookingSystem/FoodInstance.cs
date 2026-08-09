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
    public float BurnTime => _burnTime;
    public CookState CurrentState => currentState;
    private CookState currentState = CookState.Raw;

    [Header("Seasoning (เครื่องปรุง)")]
    public int spicyLevel = 0;   
    public bool hasSauce = false;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer mainRenderer;
    [SerializeField] private SpriteRenderer sauceRenderer;
    [SerializeField] private SpriteRenderer spicyRenderer;

    [Header("Smoke (ควันตอนย่าง)")]
    [SerializeField] private bool enableGrillSmoke = true;
    [SerializeField] private FoodSmokeEffect smokeEffect;

    [HideInInspector] public Vector3 startDragPos;
    [HideInInspector] public GrillStation currentGrill;
    [HideInInspector] public SeasoningStation currentSeasoning;

    private bool isOnGrill = false;
    private bool isBeingDragged = false;
    private bool isFlipping = false;
    private SpriteRenderer sr;
    private Transform foodRoot;

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
        ResolveFoodRoot();
        SetupSmokeEffect();

        // ตั้งค่าภาพตั้งแต่ Awake (ไม่ใช่ Start) เพราะตอนหยิบจากถาด เราต้องรู้ขนาดภาพจริง
        // ทันทีในเฟรมเดียวกับที่ Instantiate เพื่อคำนวณจุดกึ่งกลางให้ตรงเมาส์
        if (sauceRenderer != null) sauceRenderer.gameObject.SetActive(false);
        if (spicyRenderer != null) spicyRenderer.gameObject.SetActive(false);
        UpdateVisual();
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
        float flipSpeed = 10f;

        while (transform.localScale.x > 0.01f)
        {
            transform.localScale -= new Vector3(flipSpeed * Time.deltaTime, 0, 0);
            yield return null;
        }

        Flip(); 

        while (transform.localScale.x < originalScale.x)
        {
            transform.localScale += new Vector3(flipSpeed * Time.deltaTime, 0, 0);
            yield return null;
        }

        transform.localScale = originalScale;
        isFlipping = false;
    }

    // ระบบทำอาหาร
    public void Flip() 
    {
        isFacingSideA = !isFacingSideA;
        UpdateVisual(); 
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
    public void AddSpicy()
    {
        if (spicyLevel < 3) 
        {
            spicyLevel++;
            UpdateSpicyVisual();
        }
    }
    public void ApplySauce()
    {
        if (!hasSauce)
        {
            hasSauce = true;
            if (sauceRenderer != null && data.sauceSprite != null)
            {
                sauceRenderer.sprite = data.sauceSprite;
                sauceRenderer.gameObject.SetActive(true); 
            }
            else
            {
                Debug.LogError("ลืมลาก Overlay_Sauce มาใส่ใน Inspector");
            }
        }
    }

    private void UpdateSpicyVisual()
    {
        if (spicyRenderer == null) return;

        spicyRenderer.gameObject.SetActive(true); 

        if (spicyLevel == 1) spicyRenderer.sprite = data.spicyLevel1Sprite;
        else if (spicyLevel == 2) spicyRenderer.sprite = data.spicyLevel2Sprite;
        else if (spicyLevel >= 3) spicyRenderer.sprite = data.spicyLevel3Sprite;
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

}