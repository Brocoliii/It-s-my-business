using UnityEngine;

public class SeasoningStation : MonoBehaviour
{
    [Header("จุดทาซอส (ลาก Empty Object 3 อันมาใส่)")]
    public Transform[] slots = new Transform[3];
    private FoodInstance[] foodsOnSlots = new FoodInstance[3];

    [Header("Drop Area")]
    [SerializeField] private Collider2D dropArea;
    [SerializeField] private float dropPadding = 0.2f;

    [Header("ระยะการดูดเข้าช่อง (ปรับลดลงถ้ารู้สึกว่าดูดไกลไป)")]
    public float snapRadius = 0.5f;

    // ให้ debug/สคริปต์ภายนอกอ่านได้ว่าตอนนี้มีอาหารอยู่ในช่องไหนบ้าง
    public FoodInstance[] FoodsOnSlots
    {
        get
        {
            EnsureSlotArraySize();
            return foodsOnSlots;
        }
    }

    private void Awake()
    {
        if (dropArea == null) dropArea = GetComponent<Collider2D>();
        if (dropArea == null) dropArea = GetComponentInChildren<Collider2D>();

        EnsureSlotArraySize();
    }

    // ช่องจองต้องมีจำนวนเท่ากับช่องวางเสมอ ไม่งั้นการเข้าถึงคู่กันด้วย index เดียวจะหลุดขอบ
    // (ค่าตั้งต้นเป็น 3 แต่ slots ปรับจำนวนได้จาก Inspector)
    private void EnsureSlotArraySize()
    {
        if (slots == null) slots = new Transform[0];
        if (foodsOnSlots != null && foodsOnSlots.Length == slots.Length) return;

        FoodInstance[] resized = new FoodInstance[slots.Length];

        if (foodsOnSlots != null)
        {
            int carryOver = Mathf.Min(foodsOnSlots.Length, resized.Length);
            for (int i = 0; i < carryOver; i++) resized[i] = foodsOnSlots[i];
        }

        foodsOnSlots = resized;
    }

    public bool IsWithinDropArea(Vector2 worldPos)
    {
        if (dropArea == null)
        {
            return Vector2.Distance(worldPos, transform.position) <= 1f;
        }

        // เทียบเฉพาะระนาบ XY เท่านั้น ห้ามใช้ bounds.Contains ตรงๆ
        // เพราะ Collider2D.bounds หนาแกน z เป็นศูนย์ และวางอยู่ที่ค่า z ของ transform
        // ส่วนจุดที่ส่งเข้ามาถูกตัด z ทิ้งเป็น 0 เสมอ (มาจาก Vector2)
        // โต๊ะที่ถูกดันไปที่ z อื่นเพื่อจัดเลเยอร์ภาพ จึงไม่มีวันอยู่ในเขตวางเลยสักครั้ง
        Bounds bounds = dropArea.bounds;
        return worldPos.x >= bounds.min.x - dropPadding
            && worldPos.x <= bounds.max.x + dropPadding
            && worldPos.y >= bounds.min.y - dropPadding
            && worldPos.y <= bounds.max.y + dropPadding;
    }

    public bool TrySnapToSlot(FoodInstance food, out Vector3 snapPos)
    {
        return TrySnapToSlot(food, food.Position, out snapPos, false);
    }

    // รับตำแหน่งที่จะใช้เช็คระยะแยกจาก food.Position ปัจจุบัน
    // (เคสดีดกลับ startDragPos ยังไม่ได้ขยับจริงตอนเช็ค เพราะ SnapToPosition เป็นแอนิเมชันค่อยๆ เลื่อน)
    public bool TrySnapToSlot(FoodInstance food, Vector2 checkPosition, out Vector3 snapPos)
    {
        return TrySnapToSlot(food, checkPosition, out snapPos, false);
    }

    // ignoreSnapRadius: ผู้เล่นปล่อยมือในเขต Drop Area แล้ว = ตั้งใจวางลงโต๊ะแน่ๆ
    // ให้ดูดเข้าช่องว่างที่ใกล้ที่สุดไปเลย ไม่ต้องวัด snapRadius ซ้ำ
    // ถ้ายังวัดอยู่ Drop Area จะเป็นได้แค่ "ตัวกรองให้เขตวางแคบลง" เพราะเขตที่วางสำเร็จจริง
    // คือส่วนที่ Drop Area ทับกับวงกลมรัศมี snapRadius รอบช่องเท่านั้น ขยาย collider ยังไงก็ไม่มีผล
    public bool TrySnapToSlot(FoodInstance food, Vector2 checkPosition, out Vector3 snapPos, bool ignoreSnapRadius)
    {
        snapPos = Vector3.zero;
        if (food == null) return false;

        EnsureSlotArraySize();

        int bestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            // ช่องที่ยังไม่ได้ลากอะไรมาใส่ใน Inspector ต้องข้าม ไม่ใช่ปล่อยให้ NullReference
            // ระเบิดกลางจังหวะปล่อยเมาส์จนการวางทั้งครั้งนั้นตายไปทั้งยวง
            if (slots[i] == null) continue;
            if (foodsOnSlots[i] != null) continue;

            float dist = Vector2.Distance(checkPosition, slots[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                bestSlot = i;
            }
        }

        if (bestSlot == -1) return false;
        if (!ignoreSnapRadius && minDistance >= snapRadius) return false;

        foodsOnSlots[bestSlot] = food;

        snapPos = new Vector3(slots[bestSlot].position.x, slots[bestSlot].position.y, food.Position.z);

        food.currentSeasoning = this;
        food.SetShadowVisible(true);

        SoundManager.Instance.PlaySFX(SoundIds.SeasoningPlace, snapPos);
        return true;
    }

    // หาตำแหน่งช่องที่อาหารชิ้นนี้จองไว้อยู่ ใช้ตอนต้องดึงมันกลับเข้าช่องให้ตรงเป๊ะ
    // (เช่น ผู้เล่นแค่คลิกค้างไว้ แล้วเมาส์ขยับนิดหน่อยจนตัวอาหารเลื่อนออกจากช่อง)
    public bool TryGetSlotPosition(FoodInstance food, out Vector3 slotPos)
    {
        slotPos = Vector3.zero;
        EnsureSlotArraySize();

        for (int i = 0; i < slots.Length; i++)
        {
            if (foodsOnSlots[i] == food && slots[i] != null)
            {
                slotPos = new Vector3(slots[i].position.x, slots[i].position.y, food.Position.z);
                return true;
            }
        }

        return false;
    }

    public void RemoveFood(FoodInstance food)
    {
        EnsureSlotArraySize();

        for (int i = 0; i < slots.Length; i++)
        {
            if (foodsOnSlots[i] == food)
            {
                foodsOnSlots[i] = null;
                food.currentSeasoning = null;
                food.SetShadowVisible(false);
                break;
            }
        }
    }
}