using UnityEngine;
using UnityEngine.UI; // ✨ เรียกใช้ UI

[System.Serializable]
public class OrderData
{
    public FoodData wantedFood;
    public int wantedSpicyLevel;
    public bool wantedSauce;
}

public class Customer : MonoBehaviour
{
    [Header("ข้อมูลลูกค้า")]
    public float maxPatience = 90f;
    private float currentPatience;

    [Header("UI ออเดอร์ (ลากจากในลูกของตัวเองมาใส่)")]
    public Animator bubbleAnimator; // แอนิเมเตอร์ของ OrderBubble (ถ้าทำเอฟเฟกต์เด้งไว้)
    public Image foodIcon;          // รูปหมู/เนื้อ ที่สั่ง
    public Image spicyIcon;         // รูปพริก
    public Image sauceIcon;         // รูปซอส
    public Image patienceFill;      // หลอดเวลาสีเขียว (PatienceBar_Fill)

    [Header("ฐานข้อมูลรูปภาพ (ลากรูปจาก Project มาใส่)")]
    public Sprite[] spicySprites;   // ใส่รูปพริกระดับ 1, 2, 3 ตามลำดับ
    public Sprite sauceSprite;      // ใส่รูปแปรงทาซอส

    [HideInInspector] public OrderData myOrder;
    [HideInInspector] public int mySlotIndex;
    private CustomerManager manager;
    private bool isLeaving = false;

    public void Init(OrderData order, int slotIndex, CustomerManager mgr)
    {
        myOrder = order;
        mySlotIndex = slotIndex;
        manager = mgr;
        currentPatience = maxPatience;
        isLeaving = false;

        // ✨ แปลงค่าซอสให้เป็นคำอ่านง่ายๆ
        string sauceText = order.wantedSauce ? "ทาซอสด้วย" : "ไม่เอาซอส";

        // ✨ ยิงข้อความ Debug สีสันสดใสอ่านง่ายๆ ขึ้น Console
        Debug.Log($"<color=orange>[ออเดอร์ช่อง {slotIndex}]</color> ลูกค้าสั่ง: <b>{order.wantedFood.foodName}</b> | เผ็ดระดับ: <b>{order.wantedSpicyLevel}</b> | <b>{sauceText}</b>");
    }
    private void Update()
    {
        if (isLeaving) return;

        // นับเวลาถอยหลัง
        currentPatience -= Time.deltaTime;

        // ✨ อัปเดตหลอด UI แนวตั้ง (0.0 ถึง 1.0)
        if (patienceFill != null)
        {
            patienceFill.fillAmount = currentPatience / maxPatience;

            // ลูกเล่น: เปลี่ยนสีหลอดตามเวลาที่เหลือ
            if (patienceFill.fillAmount > 0.5f) patienceFill.color = Color.green; // เกินครึ่งสีเขียว
            else if (patienceFill.fillAmount > 0.25f) patienceFill.color = new Color(1f, 0.5f, 0f); // เหลือน้อยสีส้ม
            else patienceFill.color = Color.red; // ใกล้หมดเวลาสีแดง!
        }

        // หมดเวลา ลูกค้าหนี
        if (currentPatience <= 0)
        {
            Leave(false);
        }
    }

    public void ReceiveCup(Cup cup)
    {
        if (isLeaving || cup.contents.Count == 0) return;

        bool isCorrect = true;
        var foodInCup = cup.contents[0];

        if (foodInCup.data != myOrder.wantedFood) isCorrect = false;
        if (foodInCup.spicy != myOrder.wantedSpicyLevel) isCorrect = false;
        if (foodInCup.sauce != myOrder.wantedSauce) isCorrect = false;
        if (foodInCup.state != FoodInstance.CookState.Cooked) isCorrect = false;

        if (isCorrect)
        {
            Debug.Log("ถูกต้อง!");
            Destroy(cup.gameObject);
            Leave(true);
        }
        else
        {
            Debug.Log("ทำผิด!");
            Destroy(cup.gameObject);
            Leave(false);
        }
    }
    

    private void Leave(bool isSatisfied)
    {
        isLeaving = true;

        if (bubbleAnimator != null) bubbleAnimator.SetTrigger("PopOut"); 

        manager.OnCustomerLeft(mySlotIndex);
        Destroy(gameObject, 0.5f);
    }
}