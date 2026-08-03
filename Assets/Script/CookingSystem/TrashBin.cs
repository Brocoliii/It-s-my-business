using UnityEngine;

public class TrashBin : MonoBehaviour
{
    public void TrashFood(FoodInstance food)
    {
        if (food == null) return;

        if (food.currentGrill != null)
        {
            food.currentGrill.RemoveFood(food);
        }

        if (food.currentSeasoning != null)
        {
            food.currentSeasoning.RemoveFood(food);
        }

        Destroy(food.gameObject);
    }
}