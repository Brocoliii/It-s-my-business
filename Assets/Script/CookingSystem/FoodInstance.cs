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
        SetSortingLayer(dragLayer);

        if (mainRenderer != null) mainRenderer.sortingOrder = 1;
        if (sauceRenderer != null) sauceRenderer.sortingOrder = 2;
        if (spicyRenderer != null) spicyRenderer.sortingOrder = 3;
    }

    public void OnDrag(Vector2 mousePos)
    {
        if (isBeingDragged) transform.position = mousePos;
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