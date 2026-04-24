using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CameraController cameraController;
    private Vector2 pressDownPos;
    private Vector2 dragOffset;

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


        if (Mouse.current.leftButton.isPressed && currentDraggable != null)
        {
            currentDraggable.OnDrag(mouseWorldPos + dragOffset);
        }

       
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            bool isClick = Vector2.Distance(pressDownPos, mouseWorldPos) < 0.1f;

            if (currentDraggable != null)
            {
                FoodInstance food = currentDraggable as FoodInstance;
                if (food != null)
                {
                    RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
                    bool placed = false;

                    foreach (RaycastHit2D hit in hits)
                    {
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