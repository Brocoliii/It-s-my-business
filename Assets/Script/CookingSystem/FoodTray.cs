using UnityEngine;

public class FoodTray : MonoBehaviour
{
    [Header("ตั้งค่าถาดวัตถุดิบ")]
    [Tooltip("ลาก Prefab ของไม้หมาล่าชนิดนั้นๆ มาใส่ตรงนี้")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Transform spawnPoint;

    public FoodInstance SpawnFood()
    {
        Vector3 finalSpawnPos = spawnPoint != null ? spawnPoint.position : transform.position;
        return SpawnFoodAt(finalSpawnPos);
    }

    // ปล่อยวัตถุดิบออกมาที่ตำแหน่งที่กำหนด (ใช้ตอนหยิบจากถาด ให้ไปโผล่ตรงเมาส์เลย)
    public FoodInstance SpawnFoodAt(Vector3 worldPosition)
    {
        if (foodPrefab == null) return null;

        // คุม Z ให้เท่ากับถาดเสมอ กันวัตถุดิบหลุดไปอยู่หน้า/หลังกล้อง
        worldPosition.z = spawnPoint != null ? spawnPoint.position.z : transform.position.z;

        GameObject newFoodObj = Instantiate(foodPrefab, worldPosition, Quaternion.identity);

        // เผื่อ prefab วาง FoodInstance ไว้ที่ลูก ไม่ได้อยู่ที่ root
        FoodInstance foodInstance = newFoodObj.GetComponent<FoodInstance>();
        if (foodInstance == null) foodInstance = newFoodObj.GetComponentInChildren<FoodInstance>(true);

        if (foodInstance == null)
        {
            // ถ้าไม่มี FoodInstance = ไม่มีตัวรับ IDraggable วัตถุดิบจะเกิดมาแล้วค้างอยู่กับที่ ลากไม่ได้
            Debug.LogError($"[FoodTray] prefab '{foodPrefab.name}' ไม่มีสคริปต์ FoodInstance ติดอยู่ เลยลากไม่ได้ ให้ใส่ FoodInstance ที่ตัว root ของ prefab", this);
            Destroy(newFoodObj);
            return null;
        }

        // บอกให้ชัดว่าตัวแม่คือ root ที่เพิ่งเกิดมา เวลาลาก/ทิ้ง จะได้ยกไปทั้งชิ้น ไม่เหลือ root เปล่าค้างใน Hierarchy
        foodInstance.SetFoodRoot(newFoodObj.transform);

        return foodInstance;
    }
}