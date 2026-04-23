using UnityEngine;
using System.Collections;

public class ChiliBottle : MonoBehaviour, IClickable
{
    [SerializeField] private SeasoningStation station; // ลากเขียงสีเขียวมาใส่

    public void OnClick()
    {
        // 1. เล่นแอนิเมชันเด้งดึ๋ง
        StartCoroutine(BounceEffect());

        // 2. เช็คว่ามีอาหารวางอยู่บนเขียงไหม ถ้ามีให้เพิ่มความเผ็ด!
        if (station != null && station.currentFood != null)
        {
            station.currentFood.AddSpicy();
        }
    }

    // ฟังก์ชันทำแอนิเมชันขยายใหญ่แล้วหดกลับ (เหมือนเด้ง)
    private IEnumerator BounceEffect()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.2f; // พองขึ้น 20%
        yield return new WaitForSeconds(0.1f);       // รอ 0.1 วิ
        transform.localScale = originalScale;        // คืนขนาดเดิม
    }
}