using UnityEngine;

public class SeasoningStation : MonoBehaviour
{
    [Header("จุดทาซอส (ลาก Empty Object 3 อันมาใส่)")]
    public Transform[] slots = new Transform[3];
    private FoodInstance[] foodsOnSlots = new FoodInstance[3];

    public bool TrySnapToSlot(FoodInstance food, out Vector3 snapPos)
    {
        snapPos = Vector3.zero;
        int bestSlot = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (foodsOnSlots[i] == null)
            {
                float dist = Vector2.Distance(food.transform.position, slots[i].position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    bestSlot = i;
                }
            }
        }

        if (bestSlot != -1 && minDistance < 3f)
        {
            foodsOnSlots[bestSlot] = food;
            snapPos = slots[bestSlot].position;

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