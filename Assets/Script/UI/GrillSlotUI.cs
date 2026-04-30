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

    [Header("Shake Settings")]
    public float shakeIntensity = 0.05f;
    private Vector3 originalPos;

    private void Start()
    {
        if (myCanvas == null) myCanvas = GetComponent<Canvas>();
        if (fillImage != null) fillImage.type = Image.Type.Filled;

        originalPos = transform.localPosition;
    }

    private void Update()
    {
        if (myGrillStation == null || myGrillStation.foodsOnSlots == null) return;

        FoodInstance food = myGrillStation.foodsOnSlots[slotIndex];

        if (food == null)
        {
            myCanvas.enabled = false;
            ResetShake();
            return;
        }

        myCanvas.enabled = true;

        float cookTime = food.GetData().cookTime;
        float burnTime = food.GetData().burnTime;
        float timer = food.CurrentCookTimer;

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