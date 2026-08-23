using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [Header("การตั้งค่าลูกค้า")]
    public GameObject[] customerPrefabs;
    public Transform[] customerSlots;
    [Tooltip("จุดที่ลูกค้าจะโผล่ออกมาก่อนเดินเข้ามายัง customerSlots (เรียงตามช่องเดียวกัน) ถ้าช่องไหนไม่ใส่ไว้ จะโผล่ที่จุดยืนเลยโดยไม่เดินเข้ามา")]
    public Transform[] customerSpawnPoints;
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
    private int[] activeCustomerPrefabIndex;
    private bool customersHiddenForInvestigation = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        activeCustomers = new Customer[customerSlots.Length];
        activeCustomerPrefabIndex = new int[customerSlots.Length];
        for (int i = 0; i < activeCustomerPrefabIndex.Length; i++) activeCustomerPrefabIndex[i] = -1;
    }

    // ซ่อน/แสดงลูกค้าทุกคนพร้อมกัน (ทั้งตัวลูกค้าและ Canvas ออเดอร์) ใช้ตอนผู้เล่นกำลังสืบสวนอยู่
    public void SetAllCustomerCanvasesVisible(bool isVisible)
    {
        customersHiddenForInvestigation = !isVisible;

        if (activeCustomers == null) return;

        foreach (Customer customer in activeCustomers)
        {
            if (customer != null) customer.SetCustomerVisible(isVisible);
        }
    }

    public void StartSpawningCustomers()
    {
        StartCoroutine(InitialSpawnRoutine());
    }

    public void SpawnCustomer(int slotIndex)
    {
        if (GameManager.Instance.stopSpawningCustomers) return;

        // จบเกมไปแล้ว (แพ้/ชนะ) ห้ามมีลูกค้าใหม่เดินเข้ามาทับหน้าจอแพ้
        // คิวเกิดจาก SpawnCooldownRoutine ที่ตั้งไว้ก่อนแพ้ยังเดินต่ออยู่ ต้องกันตรงนี้
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null || currentStage.availableMenu.Count == 0 || customerPrefabs.Length == 0) return;

        int randomCustomerIndex = PickUnusedCustomerPrefabIndex(slotIndex);

        Vector3 standPosition = customerSlots[slotIndex].position;
        bool hasSpawnPoint = customerSpawnPoints != null && slotIndex < customerSpawnPoints.Length && customerSpawnPoints[slotIndex] != null;
        Vector3 spawnPosition = hasSpawnPoint ? customerSpawnPoints[slotIndex].position : standPosition;

        GameObject newCustomerObj = Instantiate(customerPrefabs[randomCustomerIndex], spawnPosition, Quaternion.identity);
        Customer newCustomer = newCustomerObj.GetComponent<Customer>();
        activeCustomerPrefabIndex[slotIndex] = randomCustomerIndex;

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

        newCustomer.Init(randomOrder, slotIndex, this, standPosition);

        // ถ้าผู้เล่นกำลังกดค้างสืบสวนอยู่พอดี ลูกค้าที่เพิ่งเกิดใหม่ต้องซ่อนทันที ไม่ใช่โผล่มาเป็นปกติก่อน
        if (customersHiddenForInvestigation) newCustomer.SetCustomerVisible(false, animate: false);

        activeCustomers[slotIndex] = newCustomer;
        currentActiveCustomerCount++;
    }

    // สุ่มพรีแฟบลูกค้าโดยเลี่ยงหน้าตาที่กำลังนั่งอยู่ในช่องอื่นตอนนี้ กันลูกค้าหน้าเดิมโผล่ซ้ำพร้อมกัน
    // ถ้าพรีแฟบไม่พอ (ช่องเยอะกว่าจำนวนหน้าตาที่มี) จะสุ่มจากทั้งหมดแทน
    private int PickUnusedCustomerPrefabIndex(int slotIndex)
    {
        List<int> availableIndices = new List<int>();
        for (int i = 0; i < customerPrefabs.Length; i++)
        {
            bool isActiveElsewhere = false;
            for (int s = 0; s < activeCustomerPrefabIndex.Length; s++)
            {
                if (s != slotIndex && activeCustomerPrefabIndex[s] == i)
                {
                    isActiveElsewhere = true;
                    break;
                }
            }

            if (!isActiveElsewhere) availableIndices.Add(i);
        }

        if (availableIndices.Count == 0) return Random.Range(0, customerPrefabs.Length);
        return availableIndices[Random.Range(0, availableIndices.Count)];
    }

    public void OnCustomerLeft(int slotIndex)
    {
        activeCustomers[slotIndex] = null;
        activeCustomerPrefabIndex[slotIndex] = -1;
        currentActiveCustomerCount--;

        if (GameManager.Instance.stopSpawningCustomers && currentActiveCustomerCount <= 0)
        {
            GameManager.Instance.StartEndOfDaySequence();
        }
        else
        {
            StartCoroutine(SpawnCooldownRoutine(slotIndex));
        }
    }

    // GameManager เรียกตอนสั่งปิดร้าน (เบาะแสครบ หรือเบาะแสหมดกอง)
    // ถ้าจังหวะนั้นบนจอไม่มีลูกค้าเหลืออยู่เลย ต้องจบวันเองทันที เพราะจะไม่มี OnCustomerLeft มาปลุกอีกแล้ว
    public void NotifyShopClosed()
    {
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;
        if (currentActiveCustomerCount > 0) return;

        GameManager.Instance.StartEndOfDaySequence();
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