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
    [HideInInspector] public bool tutorialSingleCustomerMode;
    [HideInInspector] public bool tutorialCustomerHasSpawned;
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
    [Tooltip("โควตาอาหารรวมของลูกค้าทุกคนที่อยู่บนจอพร้อมกัน เช่นตั้ง 3 ถ้าคนแรกสั่ง 2 คนถัดไปจะสั่งได้แค่ 1 / ถ้าคนแรกสั่ง 1 คนถัดไปสั่งได้ถึง 2 (ใส่ 0 หรือติดลบ = ไม่จำกัด)")]
    public int maxTotalFoodOnScreen = 3;

    private Customer[] activeCustomers;
    private int[] activeCustomerPrefabIndex;
    private int[] activeCustomerFoodCount;
    private int currentTotalFoodOrdered = 0;
    private bool customersHiddenForInvestigation = false;
    private bool deliveryHighlightActive = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        activeCustomers = new Customer[customerSlots.Length];
        activeCustomerPrefabIndex = new int[customerSlots.Length];
        activeCustomerFoodCount = new int[customerSlots.Length];
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

    public void SetAllCustomersDeliveryHighlight(bool isHighlighted)
    {
        deliveryHighlightActive = isHighlighted;
        if (activeCustomers == null) return;

        foreach (Customer customer in activeCustomers)
        {
            if (customer != null) customer.SetDeliveryHighlight(isHighlighted);
        }
    }

    public void StartSpawningCustomers()
    {
        StartCoroutine(InitialSpawnRoutine());
    }

    public void SpawnCustomer(int slotIndex)
    {
        if (GameManager.Instance.stopSpawningCustomers) return;

        if (tutorialSingleCustomerMode && tutorialCustomerHasSpawned) return;

        // จบเกมไปแล้ว (แพ้/ชนะ) ห้ามมีลูกค้าใหม่เดินเข้ามาทับหน้าจอแพ้
        // คิวเกิดจาก SpawnCooldownRoutine ที่ตั้งไว้ก่อนแพ้ยังเดินต่ออยู่ ต้องกันตรงนี้
        if (GameManager.Instance.CurrentState != GameManager.GameState.Playing) return;

        StageConfig currentStage = GameManager.Instance.GetCurrentStage();
        if (currentStage == null || currentStage.availableMenu.Count == 0 || customerPrefabs.Length == 0) return;

        // โควตาอาหารรวมบนจอ: ลูกค้าใหม่สั่งได้เท่าที่เหลือจากที่ลูกค้าคนอื่นสั่งไปแล้วเท่านั้น
        // ถ้าโควตาที่เหลือยังไม่พอสั่งขั้นต่ำ ให้รอแล้วค่อยลองใหม่ ยังไม่เกิดลูกค้าในช่องนี้
        int minThisOrder = GetMinFoodThisOrder();
        int remainingFoodBudget = GetRemainingFoodBudget();
        if (remainingFoodBudget < minThisOrder)
        {
            StartCoroutine(SpawnCooldownRoutine(slotIndex));
            return;
        }

        int randomCustomerIndex = PickUnusedCustomerPrefabIndex(slotIndex);

        Vector3 standPosition = customerSlots[slotIndex].position;
        bool hasSpawnPoint = customerSpawnPoints != null && slotIndex < customerSpawnPoints.Length && customerSpawnPoints[slotIndex] != null;
        Vector3 spawnPosition = hasSpawnPoint ? customerSpawnPoints[slotIndex].position : standPosition;

        GameObject newCustomerObj = Instantiate(customerPrefabs[randomCustomerIndex], spawnPosition, Quaternion.identity);
        Customer newCustomer = newCustomerObj.GetComponent<Customer>();
        activeCustomerPrefabIndex[slotIndex] = randomCustomerIndex;

        OrderData randomOrder = new OrderData();
        randomOrder.wantedFoods = new List<FoodData>();

        int maxThisOrder = Mathf.Min(maxFoodPerOrder, remainingFoodBudget);
        if (maxThisOrder < minThisOrder) maxThisOrder = minThisOrder;

        int orderAmount = Random.Range(minThisOrder, maxThisOrder + 1);
        for (int i = 0; i < orderAmount; i++)
        {
            randomOrder.wantedFoods.Add(currentStage.availableMenu[Random.Range(0, currentStage.availableMenu.Count)]);
        }

        if (tutorialSingleCustomerMode)
        {
            randomOrder.wantedSpicyLevel = 3;
            randomOrder.wantedSauce = true;
        }
        else
        {
            randomOrder.wantedSpicyLevel = Random.Range(0, currentStage.maxSpicyLevel + 1);
            randomOrder.wantedSauce = currentStage.allowSauce && Random.value > 0.5f;
        }

        newCustomer.Init(randomOrder, slotIndex, this, standPosition);
        newCustomer.SetDeliveryHighlight(deliveryHighlightActive);

        // ถ้าผู้เล่นกำลังกดค้างสืบสวนอยู่พอดี ลูกค้าที่เพิ่งเกิดใหม่ต้องซ่อนทันที ไม่ใช่โผล่มาเป็นปกติก่อน
        if (customersHiddenForInvestigation) newCustomer.SetCustomerVisible(false, animate: false);

        activeCustomers[slotIndex] = newCustomer;
        if (tutorialSingleCustomerMode) tutorialCustomerHasSpawned = true;
        activeCustomerFoodCount[slotIndex] = orderAmount;
        currentTotalFoodOrdered += orderAmount;
        currentActiveCustomerCount++;
    }

    // จำนวนไม้ที่ยังสั่งเพิ่มได้ตอนนี้ = โควตารวม ลบด้วยของที่ลูกค้าบนจอสั่งค้างไว้
    // ตั้ง maxTotalFoodOnScreen เป็น 0 หรือติดลบ = ปิดระบบโควตา (ใช้ maxFoodPerOrder ตามเดิม)
    private int GetRemainingFoodBudget()
    {
        if (maxTotalFoodOnScreen <= 0) return maxFoodPerOrder;
        return Mathf.Max(0, maxTotalFoodOnScreen - currentTotalFoodOrdered);
    }

    // กันกรณีตั้งโควตารวมไว้น้อยกว่าจำนวนสั่งขั้นต่ำ ซึ่งจะทำให้ไม่มีลูกค้าเกิดเลยตลอดกาล
    private int GetMinFoodThisOrder()
    {
        int min = Mathf.Max(1, minFoodPerOrder);
        if (maxTotalFoodOnScreen > 0) min = Mathf.Min(min, maxTotalFoodOnScreen);
        return min;
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

    public void OnCustomerLeft(int slotIndex, bool isSatisfied)
    {
        activeCustomers[slotIndex] = null;
        activeCustomerPrefabIndex[slotIndex] = -1;
        // คืนโควตาอาหารที่ลูกค้าคนนี้จองไว้ ให้ลูกค้าคนต่อไปสั่งได้
        currentTotalFoodOrdered = Mathf.Max(0, currentTotalFoodOrdered - activeCustomerFoodCount[slotIndex]);
        activeCustomerFoodCount[slotIndex] = 0;
        currentActiveCustomerCount--;

        if (tutorialSingleCustomerMode && !isSatisfied)
            tutorialCustomerHasSpawned = false;

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