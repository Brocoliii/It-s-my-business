using UnityEngine;
using System.Collections;

public class FoodTray : MonoBehaviour
{
    [Header("ตั้งค่าถาดวัตถุดิบ")]
    [Tooltip("ลาก Prefab ของไม้หมาล่าชนิดนั้นๆ มาใส่ตรงนี้")]
    [SerializeField] private GameObject foodPrefab;
    [SerializeField] private Transform spawnPoint;

    [Header("Pickup Animation (ถาดเด้งตอนของถูกหยิบออกไป)")]
    [Tooltip("ตัวที่จะเล่นแอนิเมชัน ถ้าไม่ตั้งจะใช้ตัวถาดเอง")]
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float pickupAnimDuration = 0.25f;
    [SerializeField] private float pickupSquashAmount = 0.12f;

    private Coroutine pickupAnimCoroutine;
    private Vector3 baseScale;
    private bool hasCachedBaseScale;

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

        // ให้ทั้งถาดและวัตถุดิบเด้งตอบรับพร้อมกัน ผู้เล่นจะได้รู้สึกว่าหยิบของออกมาจริงๆ
        PlayPickupAnimation();
        foodInstance.PlayTrayPickupAnimation();

        return foodInstance;
    }

    // ถาดยุบตัวแวบนึงแล้วเด้งกลับ เหมือนสั่นตอบรับตอนของถูกหยิบออกไป
    private void PlayPickupAnimation()
    {
        Transform target = visualRoot != null ? visualRoot : transform;

        // แคชขนาดจริงไว้ครั้งแรกเท่านั้น ไม่งั้นถ้าสแปมคลิกรัวๆ ระหว่างที่ยังยุบตัวไม่ทันเด้งกลับ
        // ค่าที่อ่านมาจะเพี้ยนไปเรื่อยๆ (เอาขนาดตอนยุบค้างมาอ้างอิงแทนขนาดจริง) ทำให้ถาดค่อยๆ เล็กลง/ผิดรูปสะสม
        if (!hasCachedBaseScale)
        {
            baseScale = target.localScale;
            hasCachedBaseScale = true;
        }

        if (pickupAnimCoroutine != null) StopCoroutine(pickupAnimCoroutine);
        pickupAnimCoroutine = StartCoroutine(PickupSquash(target));
    }

    private IEnumerator PickupSquash(Transform target)
    {
        float elapsed = 0f;

        while (elapsed < pickupAnimDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / pickupAnimDuration);
            float squash = Mathf.Sin(t * Mathf.PI) * pickupSquashAmount;
            target.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
            yield return null;
        }

        target.localScale = baseScale;
        pickupAnimCoroutine = null;
    }
}