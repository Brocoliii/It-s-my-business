using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GrillSlotUI : MonoBehaviour
{
    [Header("References")]
    public GrillStation myGrillStation;
    public int slotIndex = 0;

    public Canvas myCanvas;
    public Image bgImage;
    public Image fillImage;

    [Header("Colors (ตั้งค่าสีตามนี้ได้เลย)")]
    public Color defaultBgColor = new Color(0, 0, 0, 0.5f);
    public Color cookingColor = Color.yellow;
    public Color cookedBgColor = Color.green;
    public Color burntColor = Color.red;

    [Header("Side A / Side B Circles")]
    public Image sideACircleImage;
    public Image sideBCircleImage;
    public Color sideRawColor = Color.white;
    public Color sideCookedColor = Color.green;
    public Color sideBurntColor = Color.red;

    [Header("Cooking Side Arrow (ชี้ด้านที่กำลังแนบเตาอยู่)")]
    public RectTransform arrowIndicator;
    public RectTransform arrowPosSideA;
    public RectTransform arrowPosSideB;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.05f;
    private Vector3 originalPos;

    [Header("Flip Animation")]
    public float flipPunchScale = 1.15f;
    public float flipPunchDuration = 0.18f;
    public float arrowMoveDuration = 0.15f;
    private Vector3 originalScale;
    private FoodInstance subscribedFood;
    private Coroutine flipPunchCoroutine;
    private Coroutine arrowMoveCoroutine;
    private bool isArrowAnimating = false;

    private void Start()
    {
        if (myCanvas == null) myCanvas = GetComponent<Canvas>();
        if (fillImage != null) fillImage.type = Image.Type.Filled;

        originalPos = transform.localPosition;
        originalScale = transform.localScale;
    }

    private void OnDisable()
    {
        if (subscribedFood != null)
        {
            subscribedFood.OnFlipped -= HandleFoodFlipped;
            subscribedFood = null;
        }
    }

    private void Update()
    {
        if (myGrillStation == null || myGrillStation.foodsOnSlots == null) return;

        FoodInstance food = myGrillStation.foodsOnSlots[slotIndex];

        if (food != subscribedFood)
        {
            if (subscribedFood != null) subscribedFood.OnFlipped -= HandleFoodFlipped;
            subscribedFood = food;
            if (subscribedFood != null) subscribedFood.OnFlipped += HandleFoodFlipped;
        }

        if (food == null)
        {
            myCanvas.enabled = false;
            ResetShake();
            ResetSideCircles();
            if (arrowIndicator != null) arrowIndicator.gameObject.SetActive(false);
            return;
        }

        myCanvas.enabled = true;

        float cookTime = food.GetData().cookTime;
        float burnTime = food.GetData().burnTime;
        float timer = food.CurrentCookTimer;

        UpdateSideCircle(sideACircleImage, food.SideAProgress, cookTime, burnTime);
        UpdateSideCircle(sideBCircleImage, food.SideBProgress, cookTime, burnTime);
        UpdateCookingArrow(food);

        if (timer < cookTime)
        {
            if (bgImage != null) bgImage.color = defaultBgColor;
            fillImage.color = cookingColor;
            fillImage.fillAmount = Mathf.Clamp01(timer / cookTime);
            ResetShake();
        }
        else if (timer >= cookTime && timer < burnTime)
        {
            if (bgImage != null) bgImage.color = cookedBgColor;
            fillImage.color = burntColor;

            float timePassedSinceCooked = timer - cookTime;
            float timeUntilBurnt = burnTime - cookTime;
            float riskProgress = timePassedSinceCooked / timeUntilBurnt;

            fillImage.fillAmount = Mathf.Clamp01(riskProgress);

            if (riskProgress >= 0.6f) ApplyShake();
            else ResetShake();
        }
        else if (timer >= burnTime)
        {
            if (bgImage != null) bgImage.color = burntColor;
            fillImage.color = burntColor;
            fillImage.fillAmount = 1f;

            ApplyShake();
        }
    }

    // สีวงกลม Side A / Side B: ขาว (ดิบ) > เขียว (สุก) > แดง (ไหม้)
    private void UpdateSideCircle(Image circle, float progress, float cookTime, float burnTime)
    {
        if (circle == null) return;

        if (progress >= burnTime) circle.color = sideBurntColor;
        else if (progress >= cookTime) circle.color = sideCookedColor;
        else circle.color = sideRawColor;
    }

    private void ResetSideCircles()
    {
        if (sideACircleImage != null) sideACircleImage.color = sideRawColor;
        if (sideBCircleImage != null) sideBCircleImage.color = sideRawColor;
    }

    // ด้านที่แนบเตาอยู่คือด้านที่ "ไม่ได้หงายขึ้น" (isFacingSideA = true แปลว่า A หงายขึ้น, B แนบเตา)
    private void UpdateCookingArrow(FoodInstance food)
    {
        if (arrowIndicator == null) return;

        RectTransform target = food.IsFacingSideA ? arrowPosSideB : arrowPosSideA;
        if (target == null) return;

        arrowIndicator.gameObject.SetActive(true);
        if (!isArrowAnimating) arrowIndicator.position = target.position;
    }

    // เรียกตอน FoodInstance พลิกด้านจริง (กลางแอนิเมชั่นพลิกของตัวอาหาร) เพื่อเล่นเอฟเฟกต์ตอบสนองบน UI ช่องเตา
    private void HandleFoodFlipped()
    {
        if (flipPunchCoroutine != null) StopCoroutine(flipPunchCoroutine);
        flipPunchCoroutine = StartCoroutine(FlipPunchAnimation());

        if (arrowIndicator != null)
        {
            if (arrowMoveCoroutine != null) StopCoroutine(arrowMoveCoroutine);
            arrowMoveCoroutine = StartCoroutine(MoveArrowSmoothly());
        }
    }

    // บีบ-ขยายช่อง UI สั้นๆ ให้ความรู้สึกว่าเตาสั่นตอบรับตอนผู้เล่นพลิกอาหาร
    private IEnumerator FlipPunchAnimation()
    {
        float elapsed = 0f;

        while (elapsed < flipPunchDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / flipPunchDuration);
            float arc = Mathf.Sin(t * Mathf.PI);
            float scale = 1f + arc * (flipPunchScale - 1f);
            transform.localScale = originalScale * scale;
            yield return null;
        }

        transform.localScale = originalScale;
        flipPunchCoroutine = null;
    }

    // ให้ลูกศรค่อยๆ เลื่อนไปด้านใหม่แทนการเทเลพอร์ตทันที จะได้เห็นชัดว่ากำลังพลิกไปด้านไหน
    private IEnumerator MoveArrowSmoothly()
    {
        RectTransform target = subscribedFood != null && subscribedFood.IsFacingSideA ? arrowPosSideB : arrowPosSideA;
        if (target == null) yield break;

        isArrowAnimating = true;
        Vector3 start = arrowIndicator.position;
        Vector3 end = target.position;
        float elapsed = 0f;

        while (elapsed < arrowMoveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / arrowMoveDuration);
            t = t * t * (3f - 2f * t);
            arrowIndicator.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        arrowIndicator.position = end;
        isArrowAnimating = false;
        arrowMoveCoroutine = null;
    }

    private void ApplyShake()
    {
        float shakeX = Random.Range(-1f, 1f) * shakeIntensity;
        float shakeY = Random.Range(-1f, 1f) * shakeIntensity;
        transform.localPosition = originalPos + new Vector3(shakeX, shakeY, 0);
    }

    private void ResetShake()
    {
        if (transform.localPosition != originalPos)
        {
            transform.localPosition = originalPos;
        }
    }
}