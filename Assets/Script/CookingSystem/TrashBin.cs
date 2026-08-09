using UnityEngine;

public class TrashBin : MonoBehaviour
{
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