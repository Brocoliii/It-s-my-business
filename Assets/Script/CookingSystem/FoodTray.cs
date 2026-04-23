using UnityEngine;

public class FoodTray : MonoBehaviour
{
    [Header("ตั้งค่าถาดวัตถุดิบ")]
    [Tooltip("ลาก Prefab ของไม้หมาล่าชนิดนั้นๆ มาใส่ตรงนี้")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Transform spawnPoint;

    public FoodInstance SpawnFood()
    {
        if (foodPrefab == null) return null;
        Vector3 finalSpawnPos = spawnPoint != null ? spawnPoint.position : transform.position;

        GameObject newFoodObj = Instantiate(foodPrefab, transform.position, Quaternion.identity);
        return newFoodObj.GetComponent<FoodInstance>();
    }
}