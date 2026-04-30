using UnityEngine;
using UnityEngine.UI;

public class FoodCookUI : MonoBehaviour
{
    [Header("References")]
    public FoodInstance foodInstance;
    public Image fillImage;

    [Header("Colors")]
    public Color rawColor = Color.yellow;
    public Color cookedColor = Color.green;
    public Color burntColor = Color.red;

    private Transform mainCam;

    // ✨ เพิ่มตัวแปรสำหรับเก็บ Canvas เพื่อเอาไว้สั่งเปิด/ปิด
    private Canvas myCanvas;

    private void Start()
    {
        if (foodInstance == null)
        {
            foodInstance = GetComponentInParent<FoodInstance>();
        }

        if (fillImage != null) fillImage.type = Image.Type.Filled;
        if (Camera.main != null) mainCam = Camera.main.transform;

        // ✨ ดึงคอมโพเนนต์ Canvas ในตัวเองมาเก็บไว้
        myCanvas = GetComponent<Canvas>();
    }

    private void Update()
    {
        if (foodInstance == null || fillImage == null) return;

        // ✨ 1. เช็คว่าชิ้นเนื้อนี้กำลังวางอยู่บนเตาหรือไม่?
        bool isOnGrill = (foodInstance.currentGrill != null);

        // ✨ 2. ถ้าอยู่บนเตาให้เปิด UI ถ้าไม่ได้อยู่ให้ซ่อน UI ซะ
        if (myCanvas != null)
        {
            myCanvas.enabled = isOnGrill;
        }

        // ✨ 3. ถ้าไม่ได้อยู่บนเตา ก็หยุดโค้ดบรรทัดล่างไว้แค่นี้ (ช่วยประหยัดการประมวลผลของเกม)
        if (!isOnGrill) return;

        // --- ระบบตรรกะหลอดเลื่อนและเปลี่ยนสี (เหมือนเดิม) ---
        float cookTime = foodInstance.GetData().cookTime;
        float burnTime = foodInstance.BurnTime;
        float timer = foodInstance.CurrentCookTimer;
        FoodInstance.CookState state = foodInstance.CurrentState;

        switch (state)
        {
            case FoodInstance.CookState.Raw:
                fillImage.color = rawColor;
                fillImage.fillAmount = Mathf.Clamp01(timer / cookTime);
                break;
            case FoodInstance.CookState.Cooked:
                fillImage.color = cookedColor;
                fillImage.fillAmount = 1f;
                break;
            case FoodInstance.CookState.Burnt:
                fillImage.color = burntColor;
                fillImage.fillAmount = Mathf.Clamp01(1f - (timer / burnTime));
                break;
        }
    }

    private void LateUpdate()
    {
        if (mainCam != null)
        {
            transform.rotation = mainCam.rotation;
        }
    }
}