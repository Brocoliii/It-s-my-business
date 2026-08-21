using UnityEngine;

public class TrashBin : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private SpriteRenderer binRenderer;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.red;

    private void Awake()
    {
        if (binRenderer == null) binRenderer = GetComponent<SpriteRenderer>();
    }

    // เรียกทุกเฟรมตอนลากของมาอยู่เหนือ/ออกจากถังขยะ เพื่อสลับสีเตือน
    public void SetHighlighted(bool isHighlighted)
    {
        if (binRenderer == null) return;
        binRenderer.color = isHighlighted ? highlightColor : normalColor;
    }

    public void TrashFood(FoodInstance food)
    {
        if (food == null) return;

        if (food.currentGrill != null)
        {
            food.currentGrill.RemoveFood(food);
        }

        if (food.currentSeasoning != null)
        {
            food.currentSeasoning.RemoveFood(food);
        }

        // ทิ้งทั้งชิ้น (รวม Root เปล่าที่ครอบอยู่) ไม่ใช่ทิ้งแค่ตัวภาพ
        Destroy(food.RootObject);
    }

    public void TrashCup(Cup cup)
    {
        if (cup == null) return;

        // อาหารในถ้วยถูกย้ายไปเป็นลูกของถ้วยแล้ว ทิ้งถ้วยทีเดียวหายไปพร้อมกันหมด
        Destroy(cup.gameObject);
    }
}