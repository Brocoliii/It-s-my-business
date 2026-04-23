using UnityEngine;
using System.Collections;

public class SauceBrush : MonoBehaviour, IClickable
{
    [SerializeField] private SeasoningStation station; // ลากเขียงสีเขียวมาใส่

    public void OnClick()
    {
        StartCoroutine(BounceEffect());

        // เช็คว่ามีอาหารวางบนเขียงไหม ถ้ามีให้ทาซอส!
        if (station != null && station.currentFood != null)
        {
            station.currentFood.ApplySauce();
        }
    }

    private IEnumerator BounceEffect()
    {
        Vector3 originalScale = transform.localScale;
        transform.localScale = originalScale * 1.1f; // พองนิดหน่อย
        transform.Rotate(0, 0, -15f);                // เอียงขวาเหมือนกำลังทา
        yield return new WaitForSeconds(0.1f);
        transform.localScale = originalScale;
        transform.localRotation = Quaternion.identity; // กลับตรงเหมือนเดิม
    }
}