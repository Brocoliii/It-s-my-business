using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private Camera mainCam;
    private CameraController cameraController;

    private InputAction investigateUpAction;
    private InputAction investigateDownAction;
    private InputAction investigateLeftAction;
    private InputAction investigateRightAction;

    private readonly Queue<StratagemDirection> bufferedInvestigationInputs = new Queue<StratagemDirection>();

    private IDraggable currentDraggable;
    private bool isDraggingFreshFromTray;
    private bool foodRemovedFromStation;
    private TrashBin hoveredTrashBin;

    private const float ClickMoveThreshold = 0.1f;

    private IInvestigatable currentInvestigation;
    private Transform currentInvestigationTarget;

    private Vector2 pressDownPos;
    private Vector2 dragOffset;

    private void Awake()
    {
        CreateInvestigationInputActions();
    }

    private void Start()
    {
        mainCam = Camera.main;
        cameraController = FindObjectOfType<CameraController>();
    }

    private void OnEnable()
    {
        SetInvestigationInputActionsEnabled(true);
    }

    private void OnDisable()
    {
        SetInvestigationInputActionsEnabled(false);
        ClearBufferedInvestigationInputs();
    }

    private void OnDestroy()
    {
        DisposeInvestigationInputActions();
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return;
        }

        if (currentInvestigation != null && !HasActiveInvestigation())
        {
            ResetInvestigationState();
        }

        HandleDragAndDrop();
        HandleInvestigationInput();
    }

    private void UpdateCameraBlockState()
    {
        if (cameraController == null || Mouse.current == null) return;

        bool shouldBlock = HasActiveInvestigation() && Mouse.current.leftButton.isPressed;
        cameraController.SetInvestigationInputBlocked(shouldBlock);
    }

    private void CreateInvestigationInputActions()
    {
        if (investigateUpAction != null)
        {
            return;
        }

        investigateUpAction = new InputAction("InvestigateUp", InputActionType.Button, "<Keyboard>/w");
        investigateDownAction = new InputAction("InvestigateDown", InputActionType.Button, "<Keyboard>/s");
        investigateLeftAction = new InputAction("InvestigateLeft", InputActionType.Button, "<Keyboard>/a");
        investigateRightAction = new InputAction("InvestigateRight", InputActionType.Button, "<Keyboard>/d");

        investigateUpAction.performed += OnInvestigateUpPerformed;
        investigateDownAction.performed += OnInvestigateDownPerformed;
        investigateLeftAction.performed += OnInvestigateLeftPerformed;
        investigateRightAction.performed += OnInvestigateRightPerformed;
    }

    private void SetInvestigationInputActionsEnabled(bool isEnabled)
    {
        if (investigateUpAction == null)
        {
            return;
        }

        if (isEnabled)
        {
            investigateUpAction.Enable();
            investigateDownAction.Enable();
            investigateLeftAction.Enable();
            investigateRightAction.Enable();
        }
        else
        {
            investigateUpAction.Disable();
            investigateDownAction.Disable();
            investigateLeftAction.Disable();
            investigateRightAction.Disable();
        }
    }

    private void DisposeInvestigationInputActions()
    {
        if (investigateUpAction != null)
        {
            investigateUpAction.performed -= OnInvestigateUpPerformed;
            investigateUpAction.Dispose();
            investigateUpAction = null;
        }

        if (investigateDownAction != null)
        {
            investigateDownAction.performed -= OnInvestigateDownPerformed;
            investigateDownAction.Dispose();
            investigateDownAction = null;
        }

        if (investigateLeftAction != null)
        {
            investigateLeftAction.performed -= OnInvestigateLeftPerformed;
            investigateLeftAction.Dispose();
            investigateLeftAction = null;
        }

        if (investigateRightAction != null)
        {
            investigateRightAction.performed -= OnInvestigateRightPerformed;
            investigateRightAction.Dispose();
            investigateRightAction = null;
        }
    }

    private void OnInvestigateUpPerformed(InputAction.CallbackContext context)
    {
        QueueInvestigationInput(StratagemDirection.Up);
    }

    private void OnInvestigateDownPerformed(InputAction.CallbackContext context)
    {
        QueueInvestigationInput(StratagemDirection.Down);
    }

    private void OnInvestigateLeftPerformed(InputAction.CallbackContext context)
    {
        QueueInvestigationInput(StratagemDirection.Left);
    }

    private void OnInvestigateRightPerformed(InputAction.CallbackContext context)
    {
        QueueInvestigationInput(StratagemDirection.Right);
    }

    private void QueueInvestigationInput(StratagemDirection direction)
    {
        if (!HasActiveInvestigation())
        {
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            return;
        }

        bufferedInvestigationInputs.Enqueue(direction);
    }

    private bool HasActiveInvestigation()
    {
        if (currentInvestigation == null)
        {
            return false;
        }

        if (currentInvestigation is Component component && component == null)
        {
            return false;
        }

        return true;
    }

    private void ResetInvestigationState()
    {
        currentInvestigation = null;
        currentInvestigationTarget = null;
        ClearBufferedInvestigationInputs();
        cameraController?.EndInvestigationFocus();
        UpdateCameraBlockState();
    }

    private void ClearBufferedInvestigationInputs()
    {
        bufferedInvestigationInputs.Clear();
    }

    private void HandleDragAndDrop()
    {
        if (Mouse.current == null) return;

        UpdateCameraBlockState();

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
                if (currentDraggable == null) currentDraggable = hit.collider.GetComponentInParent<IDraggable>();

                if (currentDraggable != null)
                {
                    FoodInstance food = currentDraggable as FoodInstance;
                    // ยังไม่ถอดออกจากเตา/โต๊ะปรุงรสตอนนี้ เผื่อแค่ "คลิก" เฉยๆ (เช่น พลิกอาหาร)
                    // ไม่งั้น GrillSlotUI จะมองว่าช่องว่างแล้วหายไปแวบนึงทั้งที่ไม่ได้ลากจริง
                    // ค่อยถอดตอนขยับเมาส์เกิน threshold จริงๆ (ดูจังหวะที่ 2 ด้านล่าง)
                    foodRemovedFromStation = false;

                    // อ้างอิงตำแหน่งจากตัวที่จะถูกลากจริงๆ ไม่ใช่ collider ที่โดน
                    // (ถ้า collider อยู่ที่ลูก สองอันนี้คนละตำแหน่ง แล้ววัตถุจะกระโดดตอนหยิบ)
                    Component draggableComponent = currentDraggable as Component;
                    Transform draggedTransform = draggableComponent != null ? draggableComponent.transform : hit.transform;
                    if (food != null) draggedTransform = food.FoodRoot;
                    dragOffset = (Vector2)draggedTransform.position - mouseWorldPos;
                    isDraggingFreshFromTray = false;

                    // เครื่องมือปรุงรส (แปรงซอส/ขวดพริก) ให้เกาะกึ่งกลางเมาส์เสมอ ไม่สนใจจุดที่คลิกโดนตัว sprite
                    if (currentDraggable is SauceBrush || currentDraggable is ChiliBottle)
                    {
                        dragOffset = Vector2.zero;
                    }

                    currentDraggable.OnBeginDrag();
                    return;
                }
            }

            foreach (RaycastHit2D hit in hits)
            {
                FoodTray clickedTray = hit.collider.GetComponent<FoodTray>();
                if (clickedTray == null) clickedTray = hit.collider.GetComponentInParent<FoodTray>();

                if (clickedTray != null)
                {
                    // หยิบวัตถุดิบออกมาทันทีที่กด แล้วลากต่อได้เลยในทีเดียว
                    GrabFoodFromTray(clickedTray, mouseWorldPos);
                    return;
                }
            }

            if (currentDraggable == null)
            {
                Debug.Log("<color=orange>--- เริ่มเช็คคลิกแอบฟัง ---</color>");

                if (hits.Length == 0)
                {
                    Debug.Log("เมาส์ชี้ทะลุ... ไม่โดนอะไรเลย (เช็ค BoxCollider หรือ Z-Axis ด่วน!)");
                }

                foreach (RaycastHit2D hit in hits)
                {
                    Debug.Log($"เมาส์ชี้โดน: <color=yellow>{hit.collider.gameObject.name}</color>");

                    currentInvestigation = hit.collider.GetComponent<IInvestigatable>();
                    if (currentInvestigation != null)
                    {
                        Debug.Log("<color=magenta>เจอตัวคนให้สืบแล้ว! สั่งเริ่มฟัง!</color>");
                        currentInvestigationTarget = hit.collider.transform;
                        cameraController?.BeginInvestigationFocus(currentInvestigationTarget);
                        currentInvestigation.OnListenStart();
                        UpdateCameraBlockState();
                        return;
                    }
                }
                Debug.Log("<color=orange>--------------------------</color>");
            }
        }

        // ==========================================
        // จังหวะที่ 2: กดคลิกซ้าย "ค้าง" 
        // ==========================================
        if (Mouse.current.leftButton.isPressed)
        {
            if (currentDraggable != null)
            {
                // วัตถุดิบที่เพิ่งหยิบจากถาด คำนวณระยะเยื้องใหม่ทุกเฟรม
                // เผื่อ sprite เปลี่ยนขนาดระหว่างลาก จะได้เกาะกลางเมาส์ตลอด
                if (isDraggingFreshFromTray && currentDraggable is FoodInstance freshFood)
                {
                    dragOffset = freshFood.VisualCenterOffset;
                }

                // เพิ่งรู้ว่านี่คือการ "ลาก" จริงๆ (ขยับเกิน threshold) ไม่ใช่แค่คลิก
                // ค่อยถอดออกจากเตา/โต๊ะปรุงรสตอนนี้ ไม่ใช่ตั้งแต่ตอนกดลง
                if (!foodRemovedFromStation && currentDraggable is FoodInstance draggedFood
                    && Vector2.Distance(pressDownPos, mouseWorldPos) >= ClickMoveThreshold)
                {
                    if (draggedFood.currentGrill != null) draggedFood.currentGrill.RemoveFood(draggedFood);
                    if (draggedFood.currentSeasoning != null) draggedFood.currentSeasoning.RemoveFood(draggedFood);
                    foodRemovedFromStation = true;
                }

                currentDraggable.OnDrag(mouseWorldPos + dragOffset);
                UpdateTrashHighlight(mouseWorldPos);
            }
            else if (HasActiveInvestigation())
            {
                currentInvestigation.OnListening();
            }
        }

        // ==========================================
        // จังหวะที่ 3: ปล่อยคลิกซ้าย
        // ==========================================
        if (Mouse.current.leftButton.wasReleasedThisFrame)
        {
            bool isClick = Vector2.Distance(pressDownPos, mouseWorldPos) < ClickMoveThreshold;

            if (currentDraggable != null)
            {
                FoodInstance food = currentDraggable as FoodInstance;
                Cup cup = currentDraggable as Cup;

                RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
                bool placed = false;

                // แค่คลิกเฉยๆ ไม่ได้ลากออกจากช่องจริง (ดูจังหวะที่ 2) จึงไม่ต้องหาที่วางใหม่
                // ปล่อยให้ระบบคลิกด้านล่างสุดจัดการแทน (เช่น สั่งพลิกอาหาร)
                if (food != null && !foodRemovedFromStation && !isDraggingFreshFromTray)
                {
                    // ไม่ต้องทำอะไร อาหารยังอยู่ที่ช่องเดิมของมันครบถ้วน
                }
                else if (food != null)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        TrashBin trashBin = hit.collider.GetComponent<TrashBin>();
                        if (trashBin != null)
                        {
                            trashBin.TrashFood(food);
                            placed = true;
                            break;
                        }

                        Cup targetCup = hit.collider.GetComponent<Cup>();
                        if (targetCup != null) { targetCup.AddFood(food); placed = true; break; }

                        GrillStation grill = hit.collider.GetComponent<GrillStation>();
                        if (grill != null && grill.TrySnapToSlot(food, out Vector3 snapPosG)) { food.SnapToPosition(snapPosG); placed = true; break; }

                        SeasoningStation seasoning = hit.collider.GetComponentInParent<SeasoningStation>();
                        if (seasoning != null && seasoning.TrySnapToSlot(food, out Vector3 snapPosS)) { food.SnapToPosition(snapPosS); placed = true; break; }
                    }

                    if (!placed)
                    {
                        GrillStation[] grills = FindObjectsOfType<GrillStation>();
                        foreach (GrillStation grillStation in grills)
                        {
                            if (grillStation != null && grillStation.IsWithinDropArea(mouseWorldPos) && grillStation.TrySnapToSlot(food, out Vector3 snapPosG))
                            {
                                food.SnapToPosition(snapPosG);
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (!placed)
                    {
                        SeasoningStation[] seasonings = FindObjectsOfType<SeasoningStation>();
                        foreach (SeasoningStation seasoning in seasonings)
                        {
                            if (seasoning != null && seasoning.IsWithinDropArea(mouseWorldPos) && seasoning.TrySnapToSlot(food, out Vector3 snapPosS))
                            {
                                food.SnapToPosition(snapPosS);
                                placed = true;
                                break;
                            }
                        }
                    }

                    if (!placed && isDraggingFreshFromTray)
                    {
                        currentDraggable.OnEndDrag();
                        currentDraggable = null;
                        isDraggingFreshFromTray = false;
                        ClearTrashHighlight();
                        Destroy(food.RootObject);
                        return;
                    }

                    if (!placed)
                    {
                        // เช็คระยะโดยใช้ startDragPos ตรงๆ (ไม่ใช่ food.Position ปัจจุบัน)
                        // เพราะ SnapToPosition ด้านล่างเป็นแค่แอนิเมชันค่อยๆ เลื่อน ตำแหน่งจริงยังไม่ขยับตอนเช็คระยะ
                        Vector3 fallbackTargetPos = food.startDragPos;

                        RaycastHit2D[] fallBackHits = Physics2D.RaycastAll(food.startDragPos, Vector2.zero);
                        foreach (var fbHit in fallBackHits)
                        {
                            GrillStation g = fbHit.collider.GetComponent<GrillStation>();
                            if (g != null && g.TrySnapToSlot(food, food.startDragPos, out Vector3 snapPosFbG))
                            {
                                fallbackTargetPos = snapPosFbG;
                                placed = true;
                                break;
                            }

                            SeasoningStation s = fbHit.collider.GetComponentInParent<SeasoningStation>();
                            if (s != null && s.TrySnapToSlot(food, food.startDragPos, out Vector3 snapPosFbS))
                            {
                                fallbackTargetPos = snapPosFbS;
                                placed = true;
                                break;
                            }
                        }

                        if (!placed)
                        {
                            GrillStation[] grills = FindObjectsOfType<GrillStation>();
                            foreach (GrillStation grillStation in grills)
                            {
                                if (grillStation != null && grillStation.IsWithinDropArea(food.startDragPos) && grillStation.TrySnapToSlot(food, food.startDragPos, out Vector3 snapPosG2))
                                {
                                    fallbackTargetPos = snapPosG2;
                                    placed = true;
                                    break;
                                }
                            }
                        }

                        if (!placed)
                        {
                            SeasoningStation[] seasonings = FindObjectsOfType<SeasoningStation>();
                            foreach (SeasoningStation seasoning in seasonings)
                            {
                                if (seasoning != null && seasoning.IsWithinDropArea(food.startDragPos) && seasoning.TrySnapToSlot(food, food.startDragPos, out Vector3 snapPosS2))
                                {
                                    fallbackTargetPos = snapPosS2;
                                    placed = true;
                                    break;
                                }
                            }
                        }

                        food.SnapToPosition(fallbackTargetPos);
                    }
                }
                else if (cup != null)
                {
                    foreach (RaycastHit2D hit in hits)
                    {
                        TrashBin trashBin = hit.collider.GetComponent<TrashBin>();
                        if (trashBin != null) { trashBin.TrashCup(cup); placed = true; break; }

                        Customer customer = hit.collider.GetComponent<Customer>();
                        if (customer != null) { customer.ReceiveCup(cup); placed = true; break; }
                    }
                }

                currentDraggable.OnEndDrag();
                currentDraggable = null;
                isDraggingFreshFromTray = false;
                foodRemovedFromStation = false;
                ClearTrashHighlight();
            }
            else if (HasActiveInvestigation())
            {
                Debug.Log("<color=red>ปล่อยเมาส์ เลิกแอบฟัง!</color>");
                currentInvestigation.OnListenEnd();
                ResetInvestigationState();
            }

            if (isClick && !HasActiveInvestigation() && currentDraggable == null)
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

    // เช็คทุกเฟรมตอนลากของว่าเมาส์อยู่เหนือถังขยะไหม เพื่อสลับสีเตือนเฉพาะถังที่ชี้อยู่
    private void UpdateTrashHighlight(Vector2 mouseWorldPos)
    {
        TrashBin trashBin = null;
        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
        foreach (RaycastHit2D hit in hits)
        {
            trashBin = hit.collider.GetComponent<TrashBin>();
            if (trashBin != null) break;
        }

        if (trashBin == hoveredTrashBin) return;

        if (hoveredTrashBin != null) hoveredTrashBin.SetHighlighted(false);
        if (trashBin != null) trashBin.SetHighlighted(true);
        hoveredTrashBin = trashBin;
    }

    private void ClearTrashHighlight()
    {
        if (hoveredTrashBin != null)
        {
            hoveredTrashBin.SetHighlighted(false);
            hoveredTrashBin = null;
        }
    }

    // กดถาดปุ๊บ วัตถุดิบเด้งออกมาอยู่กลางเมาส์ทันที แล้วลากไปไหนต่อก็ได้ในการกดครั้งเดียว
    private void GrabFoodFromTray(FoodTray tray, Vector2 mouseWorldPos)
    {
        FoodInstance spawnedFood = tray.SpawnFoodAt(mouseWorldPos);
        if (spawnedFood == null) return;

        currentDraggable = spawnedFood;
        // ให้ภาพวัตถุดิบมาอยู่กลางเมาส์พอดี (เผื่อ pivot ของ prefab ไม่ได้อยู่กลางภาพ)
        dragOffset = spawnedFood.VisualCenterOffset;
        // วัตถุดิบที่เพิ่งหยิบจากถาด จะอยู่แค่ตอนกดค้างไว้ ถ้าปล่อยมือแล้วไม่ได้วางที่ไหน = หายไป
        isDraggingFreshFromTray = true;
        currentDraggable.OnBeginDrag();
        currentDraggable.OnDrag(mouseWorldPos + dragOffset);
    }

    private void HandleInvestigationInput()
    {
        if (bufferedInvestigationInputs.Count == 0)
            return;

        if (!HasActiveInvestigation() || Mouse.current == null || !Mouse.current.leftButton.isPressed)
        {
            if (currentInvestigation != null)
            {
                ResetInvestigationState();
            }

            ClearBufferedInvestigationInputs();
            return;
        }

        while (bufferedInvestigationInputs.Count > 0)
        {
            if (!HasActiveInvestigation() || Mouse.current == null || !Mouse.current.leftButton.isPressed)
            {
                ClearBufferedInvestigationInputs();
                return;
            }

            StratagemDirection bufferedDirection = bufferedInvestigationInputs.Dequeue();
            currentInvestigation.OnStratagemInput(bufferedDirection);

            if (!HasActiveInvestigation())
            {
                if (currentInvestigation != null)
                {
                    ResetInvestigationState();
                }

                ClearBufferedInvestigationInputs();
                return;
            }
        }
    }
}