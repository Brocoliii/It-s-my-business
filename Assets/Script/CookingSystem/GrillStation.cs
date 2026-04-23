using UnityEngine;
using System.Collections.Generic;

public class GrillStation : MonoBehaviour
{
    [SerializeField] private List<FoodInstance> foodsOnGrill = new List<FoodInstance>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        FoodInstance food = collision.GetComponent<FoodInstance>();

        if (food != null && !foodsOnGrill.Contains(food))
        {
            foodsOnGrill.Add(food);
            food.SetGrilling(true); 
            Debug.Log($"{food.name} เริ่มย่างบนเตา");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        FoodInstance food = collision.GetComponent<FoodInstance>();

        if (food != null && foodsOnGrill.Contains(food))
        {
            food.SetGrilling(false); 
            foodsOnGrill.Remove(food);
            Debug.Log($"{food.name} ถูกหยิบออกจากเตา");
        }
    }
}