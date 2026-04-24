using UnityEngine;
using UnityEngine.InputSystem;

public class ChiliBottle : MonoBehaviour, IDraggable
{
    [Header("ตั้งค่าการโรยผง")]
    [Tooltip("ระยะกระชากเมาส์ต่อ 1 ระดับความเผ็ด")]
    [SerializeField] private float requiredShake = 150f;

    private Vector3 startPos;
    private float shakeDistance = 0f;
    private SpriteRenderer sr;

    private void Start()
    {
        startPos = transform.position;
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnBeginDrag()
    {
        shakeDistance = 0f;
        if (sr != null) sr.sortingOrder = 15;
        transform.rotation = Quaternion.Euler(0, 0, -45f);
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos;

        float deltaY = Mathf.Abs(Mouse.current.delta.ReadValue().y);
        shakeDistance += deltaY;

        if (shakeDistance >= requiredShake)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
            foreach (var hit in hits)
            {
                FoodInstance food = hit.collider.GetComponent<FoodInstance>();

                if (food != null && food.currentSeasoning != null && food.spicyLevel < 3)
                {
                    food.AddSpicy(); 
                    shakeDistance = 0f;

                    transform.position += new Vector3(0, 0.2f, 0);
                    break; 
                }
            }
        }
    }

    public void OnEndDrag()
    {
        transform.position = startPos; 
        transform.rotation = Quaternion.identity; 
        if (sr != null) sr.sortingOrder = 5;
    }
}