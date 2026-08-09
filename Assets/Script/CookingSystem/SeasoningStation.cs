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

    private void Awake()
    {
        if (dropArea == null) dropArea = GetComponent<Collider2D>();
        if (dropArea == null) dropArea = GetComponentInChildren<Collider2D>();
    }

    public bool IsWithinDropArea(Vector2 worldPos)
    {
        if (dropArea == null)
        {
            return Vector2.Distance(worldPos, transform.position) <= 1f;
        }

        Bounds bounds = dropArea.bounds;
        bounds.Expand(dropPadding * 2f);
        return bounds.Contains(worldPos);
    }

    public bool TrySnapToSlot(FoodInstance food, out Vector3 snapPos)
    {
        snapPos = Vector3.zero;
        int bestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (foodsOnSlots[i] == null)
            {
                float dist = Vector2.Distance(food.Position, slots[i].position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSlot = i;
                }
            }
        }

        if (bestSlot != -1 && minDistance < snapRadius)
        {
            foodsOnSlots[bestSlot] = food;

            snapPos = new Vector3(slots[bestSlot].position.x, slots[bestSlot].position.y, food.Position.z);

            food.currentSeasoning = this;
            return true;
        }
        return false;
    }

    public void RemoveFood(FoodInstance food)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (foodsOnSlots[i] == food)
            {
                foodsOnSlots[i] = null;
                food.currentSeasoning = null;
                break;
            }
        }
    }
}