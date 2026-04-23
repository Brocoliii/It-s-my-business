using UnityEngine;

public class SeasoningStation : MonoBehaviour
{
    // อาหารที่กำลังวางอยู่บนเขียงตอนนี้
    public FoodInstance currentFood;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FoodInstance food = collision.GetComponent<FoodInstance>();
        // ถ้านำอาหารมาวาง ให้จำไว้ว่าคือชิ้นไหน
        if (food != null) currentFood = food;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        FoodInstance food = collision.GetComponent<FoodInstance>();
        // ถ้าหยิบอาหารออกไป ให้ลืมชิ้นนั้นซะ
        if (food != null && currentFood == food) currentFood = null;
    }
}
