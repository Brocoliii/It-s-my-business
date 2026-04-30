using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    [Header("การตั้งค่าลูกค้า")]
    public GameObject[] customerPrefabs; 
    public Transform[] customerSlots; 
    public float spawnCooldown = 3f;
    private int currentActiveCustomerCount = 0;

    [Header("การตั้งค่าตอนเริ่มเกม")]
    [Tooltip("เวลารอต่ำสุดก่อนลูกค้าคนแรกจะมา")]
    public float minInitialDelay = 2f;
    [Tooltip("เวลารอสูงสุดก่อนลูกค้าคนแรกจะมา")]
    public float maxInitialDelay = 5f;

    [Header("จำนวนการสั่งอาหาร")]
    [Tooltip("สั่งน้อยสุดกี่ไม้")]
    public int minFoodPerOrder = 1;
    [Tooltip("สั่งเยอะสุดกี่ไม้")]
    public int maxFoodPerOrder = 3;

    private Customer[] activeCustomers; 
    
    private void Start()
    {
        activeCustomers = new Customer[customerSlots.Length];
       
    }

    public void StartSpawningCustomers()
    {
        StartCoroutine(InitialSpawnRoutine());
    }

    public void SpawnCustomer(int slotIndex)
    {
        if (GameManager.Instance.allCluesCollected) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null || currentStage.availableMenu.Count == 0 || customerPrefabs.Length == 0) return;

        int randomCustomerIndex = Random.Range(0, customerPrefabs.Length);
        GameObject newCustomerObj = Instantiate(customerPrefabs[randomCustomerIndex], customerSlots[slotIndex].position, Quaternion.identity);
        Customer newCustomer = newCustomerObj.GetComponent<Customer>();

        OrderData randomOrder = new OrderData();
        randomOrder.wantedFoods = new List<FoodData>(); 

        int orderAmount = Random.Range(minFoodPerOrder, maxFoodPerOrder + 1);
        for (int i = 0; i < orderAmount; i++)
        {
            randomOrder.wantedFoods.Add(currentStage.availableMenu[Random.Range(0, currentStage.availableMenu.Count)]);
        }

        randomOrder.wantedSpicyLevel = Random.Range(0, currentStage.maxSpicyLevel + 1);

        if (currentStage.allowSauce) randomOrder.wantedSauce = (Random.value > 0.5f);
        else randomOrder.wantedSauce = false;

        newCustomer.Init(randomOrder, slotIndex, this);
        activeCustomers[slotIndex] = newCustomer;
        currentActiveCustomerCount++;
    }

    public void OnCustomerLeft(int slotIndex)
    {
        activeCustomers[slotIndex] = null;
        currentActiveCustomerCount--;

        if (GameManager.Instance.allCluesCollected && currentActiveCustomerCount <= 0)
        {
            GameManager.Instance.StartEndOfDaySequence();
        }
        else
        {
            StartCoroutine(SpawnCooldownRoutine(slotIndex));
        }
    }

    private IEnumerator SpawnCooldownRoutine(int slotIndex)
    {
      
        float randomCooldown = spawnCooldown + Random.Range(0f, 2f);
        yield return new WaitForSeconds(randomCooldown);
        SpawnCustomer(slotIndex);
    }

    private IEnumerator InitialSpawnRoutine()
    {
        for (int i = 0; i < customerSlots.Length; i++)
        {
            float randomWait = Random.Range(minInitialDelay, maxInitialDelay);
            yield return new WaitForSeconds(randomWait);

            SpawnCustomer(i);
        }
    }
}