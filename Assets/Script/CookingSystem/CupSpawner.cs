using UnityEngine;

public class CupSpawner : MonoBehaviour, IClickable
{
    [Header("การตั้งค่าการเสก")]
    public GameObject cupPrefab;     // ลาก Prefab ถ้วยมาใส่
    public Transform spawnPoint;     // จุดที่อยากให้ถ้วยไปเกิด (เช่น บนโต๊ะ)

    private GameObject currentCupInScene; // เช็คว่ามีถ้วยวางอยู่แล้วหรือยัง

    public void OnClick()
    {
        // ถ้าบนโต๊ะยังไม่มีถ้วย หรือถ้วยเก่าโดนส่งให้ลูกค้าไปแล้ว
        if (currentCupInScene == null)
        {
            SpawnNewCup();
        }
        else
        {
            Debug.Log("มีถ้วยวางอยู่บนโต๊ะแล้วนะ!");
        }
    }

    private void SpawnNewCup()
    {
        currentCupInScene = Instantiate(cupPrefab, spawnPoint.position, Quaternion.identity);
        // ในอนาคตอาจเพิ่มแอนิเมชั่นตอนถ้วยเด้งออกมาตรงนี้ได้
    }
}