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
    public float baseHeightOffset = 0.6f;
    public float stackHeightSpacing = 0.2f;

    private SpriteRenderer sr;
    private Vector3 startPos;

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
    }

    private void Start()
    {
        startPos = transform.position;
        ApplySortingSettings(defaultSortingOrder);
    }

    private void OnValidate()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        ApplySortingSettings(defaultSortingOrder);
    }

    public void AddFood(FoodInstance food)
    {
        FoodInstanceData newData = new FoodInstanceData
        {
            data = food.GetData(),
            spicy = food.spicyLevel,
            sauce = food.hasSauce,
            state = food.GetCurrentState()
        };
        contents.Add(newData);
        StartCoroutine(SuckFoodIntoCup(food.gameObject));
    }

    private IEnumerator SuckFoodIntoCup(GameObject foodObj)
    {
        if (foodObj.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        foodObj.transform.SetParent(this.transform);

        float duration = 0.2f;
        float elapsed = 0;

        Vector3 startLocalPos = foodObj.transform.localPosition;
        Vector3 startScale = foodObj.transform.localScale;
        Quaternion startRotation = foodObj.transform.localRotation;

        int itemIndex = contents.Count - 1;
        Vector3 targetScale = new Vector3(inCupScale, inCupScale, 1f);
        Quaternion targetRotation = Quaternion.Euler(0, 0, inCupRotationZ);

        float randomX = Random.Range(-0.05f, 0.05f);
        Vector3 targetLocalPos = new Vector3(randomX, baseHeightOffset + (itemIndex * stackHeightSpacing), 0);

        SpriteRenderer foodSr = foodObj.GetComponent<SpriteRenderer>();
        if (foodSr != null && sr != null)
        {
            foodSr.sortingOrder = sr.sortingOrder + 1 + itemIndex;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            foodObj.transform.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, t);
            foodObj.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            foodObj.transform.localRotation = Quaternion.Lerp(startRotation, targetRotation, t);
            yield return null;
        }

        foodObj.transform.localPosition = targetLocalPos;
        foodObj.transform.localScale = targetScale;
        foodObj.transform.localRotation = targetRotation;
    }

    public void ClearCup()
    {
        contents.Clear();
        foreach (Transform child in transform) Destroy(child.gameObject);
    }

    public void OnBeginDrag()
    {
        //ApplySortingSettings(dragSortingOrder);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;
    }

    public void OnEndDrag()
    {
        transform.position = startPos;
        //ApplySortingSettings(defaultSortingOrder);
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
            if (child.TryGetComponent<SpriteRenderer>(out var childSr))
            {
                childSr.sortingOrder = order + index;
                index++;
            }
        }
    }
}