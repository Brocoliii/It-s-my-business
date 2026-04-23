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
        
        UpdateVisual();
    }

    // ระบบ IDraggable
    public void OnBeginDrag()
    {
        isBeingDragged = true;
        if (TryGetComponent<SpriteRenderer>(out var sr)) sr.sortingOrder = 10;
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
        if (isFacingSideA) sideAProgress += Time.deltaTime;
        else sideBProgress += Time.deltaTime;
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
        spicyLevel++;
        Debug.Log($"{gameObject.name} เผ็ดระดับ: {spicyLevel}");
        // (อนาคตสามารถสั่งเปลี่ยนรูป Sprite ให้มีผงพริกติดอยู่ได้ที่นี่)
    }
    public void ApplySauce()
    {
        hasSauce = true;
        Debug.Log($"{gameObject.name} ทาซอสเรียบร้อย!");
        // (อนาคตสั่งเปลี่ยนสี Sprite ให้ออกเข้มๆ ฉ่ำๆ ได้ที่นี่)
    }

}