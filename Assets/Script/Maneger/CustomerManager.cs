using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("ตั้งค่าลูกค้า (สุ่มหน้าตา)")]
    [Tooltip("ใส่ Prefab ลูกค้าหลายๆ แบบ")]
    public GameObject[] customerPrefabs;
    public Transform[] customerSlots;
    public float spawnCooldown = 3f;

    [Header("ตั้งค่าความยากประจำด่าน (Game Stage)")]
    [Tooltip("ของที่ขายในด่านนี้ (เช่น หมู, เห็ด)")]
    public List<FoodData> availableMenu;
    [Tooltip("ด่านนี้อนุญาตให้มีความเผ็ดสูงสุดระดับไหน? (0 = ไม่มีพริก, 3 = เผ็ดสุด)")]
    public int maxSpicyLevel = 3;
    [Tooltip("ด่านนี้มีซอสให้สั่งหรือยัง? (ถ้าติ๊กออก ลูกค้าจะไม่สั่งซอสเลย)")]
    public bool allowSauce = true;

    private Customer[] activeCustomers;

    private void Start()
    {
        activeCustomers = new Customer[customerSlots.Length];

        for (int i = 0; i < customerSlots.Length; i++)
        {
            SpawnCustomer(i);
        }
    }

    private void SpawnCustomer(int slotIndex)
    {
        if (availableMenu.Count == 0 || customerPrefabs.Length == 0)
        {
            Debug.LogError("ลืมใส่ Menu หรือ Customer Prefabs ใน Manager หรือเปล่า?");
            return;
        }

        int randomCustomerIndex = Random.Range(0, customerPrefabs.Length);
        GameObject prefabToSpawn = customerPrefabs[randomCustomerIndex];

        GameObject newCustomerObj = Instantiate(prefabToSpawn, customerSlots[slotIndex].position, Quaternion.identity);
        Customer newCustomer = newCustomerObj.GetComponent<Customer>();

        OrderData randomOrder = new OrderData();

        randomOrder.wantedFood = availableMenu[Random.Range(0, availableMenu.Count)];

        randomOrder.wantedSpicyLevel = Random.Range(0, maxSpicyLevel + 1);

        if (allowSauce)
            randomOrder.wantedSauce = (Random.value > 0.5f);
        else
            randomOrder.wantedSauce = false; 

        newCustomer.Init(randomOrder, slotIndex, this);
        activeCustomers[slotIndex] = newCustomer;
    }

    public void OnCustomerLeft(int slotIndex)
    {
        activeCustomers[slotIndex] = null;
        StartCoroutine(SpawnCooldownRoutine(slotIndex));
    }

    private IEnumerator SpawnCooldownRoutine(int slotIndex)
    {
        yield return new WaitForSeconds(spawnCooldown);
        SpawnCustomer(slotIndex);
    }
}