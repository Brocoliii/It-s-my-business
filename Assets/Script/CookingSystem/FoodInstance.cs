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

    [Header("Seasoning (เครื่องปรุง)")]
    public int spicyLevel = 0;   
    public bool hasSauce = false;

    [Header("Visual Components")]
    [SerializeField] private SpriteRenderer mainRenderer;  
    [SerializeField] private SpriteRenderer sauceRenderer; 
    [SerializeField] private SpriteRenderer spicyRenderer;

    [HideInInspector] public Vector3 startDragPos;
    [HideInInspector] public GrillStation currentGrill;
    [HideInInspector] public SeasoningStation currentSeasoning;

    private bool isOnGrill = false;
    private bool isBeingDragged = false;
    private bool isFlipping = false;
    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        if (sauceRenderer != null) sauceRenderer.gameObject.SetActive(false);
        if (spicyRenderer != null) spicyRenderer.gameObject.SetActive(false);
        UpdateVisual();
    }

    // ระบบ IDraggable
    public void OnBeginDrag()
    {
        startDragPos = transform.position; 
        isBeingDragged = true;

        if (mainRenderer != null) mainRenderer.sortingOrder = 10;
        if (sauceRenderer != null) sauceRenderer.sortingOrder = 11;
        if (spicyRenderer != null) spicyRenderer.sortingOrder = 12;
    }

    public void OnDrag(Vector2 mousePos)
    {
        if (isBeingDragged) transform.position = mousePos;
    }

    public void OnEndDrag()
    {
        isBeingDragged = false;
        if (TryGetComponent<SpriteRenderer>(out var sr)) sr.sortingOrder = 5;
    }
    // Click
    public void OnClick()
    {
        // พลิกได้เฉพาะตอนอยู่บนเตา และไม่ได้กำลังพลิกอยู่
        if (isOnGrill && !isFlipping)
        {
            StartCoroutine(FlipAnimation());
        }
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

    public void SetGrilling(bool state) { isOnGrill = state; }

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
        if (sr == null || data == null) return;

        
        float currentFaceProgress = isFacingSideA ? sideAProgress : sideBProgress;

        if (currentFaceProgress >= data.burnTime) sr.sprite = data.burntSprite;
        else if (currentFaceProgress >= data.cookTime) sr.sprite = data.cookedSprite;
        else if (currentFaceProgress >= data.mediumTime) sr.sprite = data.mediumSprite;
        else sr.sprite = data.rawSprite;
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
        if (spicyLevel < 3) // ล็อคความเผ็ดไว้สูงสุดที่ 3 ระดับ
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
            // เช็คว่าเราลากตัว Overlay_Sauce มาใส่ในช่อง sauceRenderer หรือยัง
            if (sauceRenderer != null && data.sauceSprite != null)
            {
                sauceRenderer.sprite = data.sauceSprite;
                sauceRenderer.gameObject.SetActive(true); // แค่เปิดใช้งานวัตถุที่มีอยู่แล้ว
            }
            else
            {
                Debug.LogError("ลืมลาก Overlay_Sauce มาใส่ใน Inspector หรือเปล่า?");
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
}