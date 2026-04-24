using UnityEngine;
using UnityEngine.InputSystem;

public class SauceBrush : MonoBehaviour, IDraggable
{
    [Header("ตั้งค่าการทาซอส")]
    [Tooltip("ระยะกระชากเมาส์ที่ต้องการเพื่อทาซอสสำเร็จ (ยิ่งเยอะยิ่งต้องถูนาน)")]
    [SerializeField] private float requiredDrag = 200f;

    private Vector3 startPos;
    private float dragDistance = 0f;
    private SpriteRenderer sr;

    private void Start()
    {
        startPos = transform.position; 
        sr = GetComponent<SpriteRenderer>();
    }

    public void OnBeginDrag()
    {
        dragDistance = 0f; 

        if (sr != null) sr.sortingOrder = 15; 
        transform.rotation = Quaternion.Euler(0, 0, -15f); 
    }

    public void OnDrag(Vector2 mousePos)
    {
        transform.position = mousePos; 

        float deltaY = Mathf.Abs(Mouse.current.delta.ReadValue().y);
        dragDistance += deltaY; 

        if (dragDistance >= requiredDrag)
        {
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePos, Vector2.zero);
            foreach (var hit in hits)
            {
                FoodInstance food = hit.collider.GetComponent<FoodInstance>();

                if (food != null && food.currentSeasoning != null && !food.hasSauce)
                {
                    food.ApplySauce(); 
                    dragDistance = 0f; 
                    break; 
                }
            }
        }
    }

    public void OnEndDrag()
    {
        transform.position = startPos; // ปล่อยเมาส์ปุ๊บ เด้งกลับไปวางที่เดิม
        transform.rotation = Quaternion.identity; // ตั้งแปรงให้กลับมาตรงเหมือนเดิม

        if (sr != null) sr.sortingOrder = 5; // กลับไปอยู่ชั้นปกติ
    }
}