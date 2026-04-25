using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Camera mainCam;
    private IDraggable currentDraggable;
    private Vector2 pressDownPos;
    private Vector2 dragOffset;

    private void Start()
    {
        mainCam = Camera.main;
    }

    private void Update()
    {
        HandleDragAndDrop();
    }

    private void HandleDragAndDrop()
    {
        if (Mouse.current == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 mouseWorldPos = mainCam.ScreenToWorldPoint(mouseScreenPos);

        // ==========================================
        // จังหวะที่ 1: เริ่มกดคลิกซ้าย "ลง"
        // ==========================================
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            pressDownPos = mouseWorldPos;
            RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);

            foreach (RaycastHit2D hit in hits)
            {
                currentDraggable = hit.collider.GetComponent<IDraggable>();
                if (currentDraggable != null)
                {
                    FoodInstance food = currentDraggable as FoodInstance;
                    if (food != null)
                    {
                        if (food.currentGrill != null) food.currentGrill.RemoveFood(food);
                        if (food.currentSeasoning != null) food.currentSeasoning.RemoveFood(food);
                    }

                    dragOffset = (Vector2)hit.transform.position - mouseWorldPos;
                    currentDraggable.OnBeginDrag();
                    return;
                }
            }

            foreach (RaycastHit2D hit in hits)
            {
                FoodTray clickedTray = hit.collider.GetComponent<FoodTray>();
                if (clickedTray != null)
                {
                    FoodInstance spawnedFood = clickedTray.SpawnFood();
                    if (spawnedFood != null)
                    {
                        currentDraggable = spawnedFood;
                        dragOffset = Vector2.zero;
                        currentDraggable.OnBeginDrag();
                    }
                    return;
                }
            }
        }

        // ==========================================
        // จังหวะที่ 2: กดคลิกซ้าย "ค้าง" (กำลังลาก)
        // ==========================================
        if (Mouse.current.leftButton.isPressed && currentDraggable != null)
        {
            currentDraggable.OnDrag(mouseWorldPos + dragOffset);
        }

        // ==========================================
        // จังหวะที่ 3: ปล่อยคลิกซ้าย
        // ==========================================
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            bool isClick = Vector2.Distance(pressDownPos, mouseWorldPos) < 0.1f;

            if (currentDraggable != null)
            {
                FoodInstance food = currentDraggable as FoodInstance;
                Cup cup = currentDraggable as Cup;

                RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
                bool placed = false;

                if (food != null)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        Cup targetCup = hit.collider.GetComponent<Cup>();
                        if (targetCup != null)
                        {
                            targetCup.AddFood(food);
                            placed = true;
                            break;
                        }

                        GrillStation grill = hit.collider.GetComponent<GrillStation>();
                        if (grill != null && grill.TrySnapToSlot(food, out Vector3 snapPosG))
                        {
                            food.transform.position = snapPosG;
                            placed = true;
                            break;
                        }

                        SeasoningStation seasoning = hit.collider.GetComponent<SeasoningStation>();
                        if (seasoning != null && seasoning.TrySnapToSlot(food, out Vector3 snapPosS))
                        {
                            food.transform.position = snapPosS;
                            placed = true;
                            break;
                        }
                    }

                    if (!placed)
                    {
                        food.transform.position = food.startDragPos;
                        RaycastHit2D[] fallBackHits = Physics2D.RaycastAll(food.startDragPos, Vector2.zero);
                        foreach (var fbHit in fallBackHits)
                        {
                            GrillStation g = fbHit.collider.GetComponent<GrillStation>();
                            if (g != null && g.TrySnapToSlot(food, out _)) break;

                            SeasoningStation s = fbHit.collider.GetComponent<SeasoningStation>();
                            if (s != null && s.TrySnapToSlot(food, out _)) break;
                        }
                    }
                }
                else if (cup != null)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        Customer customer = hit.collider.GetComponent<Customer>();
                        if (customer != null)
                        {
                            customer.ReceiveCup(cup);
                            placed = true;
                            break;
                        }
                    }
                }

                currentDraggable.OnEndDrag();
                currentDraggable = null;
            }

            if (isClick)
            {
                RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
                foreach (RaycastHit2D hit in hits)
                {
                    IClickable clickableItem = hit.collider.GetComponent<IClickable>();
                    if (clickableItem != null)
                    {
                        clickableItem.OnClick();
                        break;
                    }
                }
            }
        }
    }
}