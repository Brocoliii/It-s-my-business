using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    private Vector2 pressDownPos;

    private Camera mainCam;
    private IDraggable currentDraggable;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        HandleCameraPan();
        HandleDragAndDrop();
    }

    private void HandleCameraPan()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (cameraController != null) cameraController.PanCamera(mouseDelta);
        }
    }

    private void HandleDragAndDrop()
    {
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // ==========================================
        // จังหวะที่ 1: เริ่มกดคลิกซ้าย "ลง" (เสกของ หรือ หยิบของ)
        // ==========================================
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressDownPos = mouseWorldPos; // จำตำแหน่งที่เริ่มกดไว้ก่อน

            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            if (hit.collider != null)
            {
                // [คืนชีพโค้ดที่หายไป] 1. เช็คว่าคลิกโดน "ถาดเสกอาหาร" หรือไม่?
                FoodTray clickedTray = hit.collider.GetComponent<FoodTray>();
                if (clickedTray != null)
                {
                    FoodInstance spawnedFood = clickedTray.SpawnFood(); // สั่งถาดเสกหมู
                    if (spawnedFood != null)
                    {
                        currentDraggable = spawnedFood;
                        currentDraggable.OnBeginDrag(); // สั่งให้เริ่มจับลากหมูอันใหม่ทันที
                    }
                    return; // จบการทำงานรอบนี้เลย ไม่ต้องไปเช็คอย่างอื่นต่อ
                }

                // 2. ถ้าไม่ได้คลิกถาด ให้เช็คว่าคลิกโดนของที่ "ลากได้" บนเตาหรือไม่?
                currentDraggable = hit.collider.GetComponent<IDraggable>();
                if (currentDraggable != null)
                {
                    currentDraggable.OnBeginDrag(); // สั่งให้เริ่มจับลาก
                }
            }
        }

        // ==========================================
        // จังหวะที่ 2: กดคลิกซ้าย "ค้าง" (กำลังลาก)
        // ==========================================
        if (Mouse.current.leftButton.isPressed && currentDraggable != null)
        {
            currentDraggable.OnDrag(mouseWorldPos); // ของขยับตามเมาส์
        }

        // ==========================================
        // จังหวะที่ 3: ปล่อยคลิกซ้าย (วางของ หรือ สั่งทำงานแบบคลิก)
        // ==========================================
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            // ✨ ตรวจสอบว่านี่คือการ "คลิก" ใช่ไหม? (โดยดูว่าตอนปล่อย เมาส์ขยับจากตอนกดไม่เกิน 0.1f)
            if (Vector2.Distance(pressDownPos, mouseWorldPos) < 0.1f)
            {
                RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);
                if (hit.collider != null)
                {
                    IClickable clickableItem = hit.collider.GetComponent<IClickable>();
                    if (clickableItem != null)
                    {
                        clickableItem.OnClick(); // สั่งให้ พลิกด้าน / เด้งขวดซอส
                    }
                }
            }

            // ไม่ว่าจะคลิกหรือลาก ตอนปล่อยเมาส์ต้องล้างค่าการลากทิ้งเสมอ
            if (currentDraggable != null)
            {
                currentDraggable.OnEndDrag();
                currentDraggable = null;
            }
        }
    }
}