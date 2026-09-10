using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    [Header("Grab Feel")]
    [Tooltip("ระยะเวลาที่ของค่อย ๆ ไถลเข้ามาอยู่กลางเมาส์ตอนเพิ่งคว้าขึ้นมา (0 = เกาะกลางเมาส์ทันทีแบบเดิม)")]
    [SerializeField] private float grabCenterEaseDuration = 0.15f;

    [Tooltip("เฉพาะอาหารที่ยกออกจากเตา/โต๊ะปรุงรส แยกค่าไว้เพราะชิ้นเนื้อใหญ่กว่าเครื่องมือ ไถลเร็วเท่ากันแล้วดูวาร์ป")]
    [SerializeField] private float foodGrabCenterEaseDuration = 0.35f;

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

    // วัดเป็น "พิกเซลบนจอ" ไม่ใช่หน่วยโลก เพราะระยะในโลกจะเปลี่ยนตามการซูม/การเลื่อนกล้อง
    // (กล้องยังไถลต่อหลังแพน และมีดันขอบจอตอนกดค้าง) ทำให้คลิกนิ่ง ๆ ถูกนับเป็นการลากจนคลิกหาย
    private const float ClickMoveThresholdPixels = 10f;

    private IInvestigatable currentInvestigation;
    private Transform currentInvestigationTarget;

    private Vector2 pressDownScreenPos;
    private Vector2 dragOffset;

    // เครื่องมือ/ถ้วยมีปลายทางเป็น "เกาะกลางเมาส์" แต่ถ้าตั้งระยะเยื้องเป็นค่าปลายทางตั้งแต่เฟรมแรกที่กด
    // ภาพจะวาร์ปจากจุดที่คลิกโดนไปกลางเมาส์ในเฟรมเดียว จึงเริ่มจากระยะเยื้องตอนคว้าจริง แล้วค่อยไล่ไปหาปลายทาง
    private Vector2 dragOffsetFrom;
    private Vector2 dragOffsetTo;
    private float grabEaseElapsed;
    // ของแต่ละชนิดไถลเข้ากลางเมาส์คนละความเร็ว จึงต้องจำค่าของ "รอบที่กำลังไถลอยู่" ไว้
    private float grabEaseDuration;
    private bool isEasingGrabOffset;

    // ของที่คลิกได้ซึ่ง "โดนตอนกดเมาส์ลง" จำไว้ตั้งแต่จังหวะกด แล้วค่อยยิง OnClick ตอนปล่อย
    // ถ้าไปหาเอาตอนปล่อยอย่างเดียว เมาส์ที่ยังเคลื่อนอยู่จะเลื่อนพ้นตัวมันไปแล้ว คลิกเลยหายเฉย ๆ
    private IClickable pendingClickable;
    private Collider2D pendingClickableCollider;

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
        // หยุดเกมอยู่: ห้ามลากของ/แอบฟังต่อ ปุ่มในแผงหยุดเกมอยู่บน Canvas จึงไม่เกี่ยวกับตรงนี้
        // (ของที่ค้างอยู่ในมือถูกปล่อยไปแล้วตอน PauseMenuController สั่ง CancelActiveInteraction)
        if (PauseMenuController.IsPaused)
        {
            return;
        }

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
        // callback ของ InputAction ยังยิงอยู่ตอนหยุดเกม ถ้าไม่กันไว้ ปุ่มที่กดระหว่างหยุด
        // จะไปกองรอในคิว แล้วถูกกินรวดเดียวตอนกดเล่นต่อ
        if (PauseMenuController.IsPaused)
        {
            return;
        }

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

    // ให้ระบบอื่นเช็คได้ว่าตอนนี้กำลังแอบฟังอยู่ไหม (PauseMenuController ใช้กันไม่ให้กดหยุดเกมกลางคัน)
    public bool IsInvestigating
    {
        get { return HasActiveInvestigation(); }
    }

    // ให้ระบบอื่นเช็คได้ว่าตอนนี้มีของอยู่ในมือจริง ๆ ไหม (CursorManager ใช้สลับเป็นรูปมือกำของ)
    public bool IsDragging
    {
        get
        {
            if (currentDraggable == null) return false;

            // ของที่ถืออยู่อาจถูกทำลายไปแล้วระหว่างลาก (เช่นโดนทิ้งลงถัง) ตัวแปรจะยังไม่เป็น null ทันที
            if (currentDraggable is Component component && component == null) return false;

            return true;
        }
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
            pressDownScreenPos = mouseScreenPos;
            pendingClickable = null;
            pendingClickableCollider = null;
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
                    isEasingGrabOffset = false;

                    // เครื่องมือปรุงรส (แปรงซอส/ขวดพริก) ให้เกาะกึ่งกลางเมาส์เสมอ ไม่สนใจจุดที่คลิกโดนตัว sprite
                    if (currentDraggable is SauceBrush || currentDraggable is ChiliBottle)
                    {
                        BeginGrabOffsetEase(dragOffset, Vector2.zero, grabCenterEaseDuration);
                    }

                    currentDraggable.OnBeginDrag();

                    // ถ้วยให้เกาะกึ่งกลางภาพไว้กับเมาส์เสมอ ไม่สนใจจุดที่คลิกโดน
                    // ต้องคำนวณหลัง OnBeginDrag เพราะถ้วยสลับเป็น dragSprite ตอนนั้น
                    // จุดกึ่งกลางของสไปรท์ตอนลากจึงไม่เท่ากับสไปรท์ปกติ
                    if (currentDraggable is Cup draggedCup)
                    {
                        BeginGrabOffsetEase(dragOffset, draggedCup.VisualCenterOffset, grabCenterEaseDuration);
                    }

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

                // ไม่ใช่ของที่ลากได้และไม่ใช่คนให้สืบ: จำไว้ก่อนว่ากดโดนอะไรที่คลิกได้ (เช่น ที่กดถ้วย)
                // แล้วรอไปยิงตอนปล่อยเมาส์ในจังหวะที่ 3
                foreach (RaycastHit2D hit in hits)
                {
                    IClickable pressedClickable = hit.collider.GetComponent<IClickable>();
                    if (pressedClickable != null)
                    {
                        pendingClickable = pressedClickable;
                        pendingClickableCollider = hit.collider;
                        break;
                    }
                }
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

                UpdateGrabOffsetEase();

                // เพิ่งรู้ว่านี่คือการ "ลาก" จริงๆ (ขยับเกิน threshold) ไม่ใช่แค่คลิก
                // ค่อยถอดออกจากเตา/โต๊ะปรุงรสตอนนี้ ไม่ใช่ตั้งแต่ตอนกดลง
                if (!foodRemovedFromStation && currentDraggable is FoodInstance draggedFood
                    && Vector2.Distance(pressDownScreenPos, mouseScreenPos) >= ClickMoveThresholdPixels)
                {
                    // ต้องจำก่อนถอด เพราะ RemoveFood จะล้าง currentGrill/currentSeasoning ทิ้ง
                    bool wasOnStation = draggedFood.currentGrill != null || draggedFood.currentSeasoning != null;

                    if (draggedFood.currentGrill != null) draggedFood.currentGrill.RemoveFood(draggedFood);
                    if (draggedFood.currentSeasoning != null) draggedFood.currentSeasoning.RemoveFood(draggedFood);
                    foodRemovedFromStation = true;

                    // เพิ่งยกออกจากเตา/โต๊ะปรุงรสจริง ค่อยไล่ให้ภาพมาเกาะกลางเมาส์เหมือนตอนหยิบจากถาด
                    // ไม่เริ่มตั้งแต่จังหวะกดลง เพราะถ้าเป็นแค่คลิกพลิก ชิ้นนั้นจะไถลออกจากช่องทั้งที่ไม่ได้ลากจริง
                    if (wasOnStation) BeginGrabOffsetEase(dragOffset, draggedFood.VisualCenterOffset, foodGrabCenterEaseDuration);
                }

                // ส่งตำแหน่งเมาส์ดิบให้ตัวอาหารเอาไปคิดการส่าย ต้องเป็นค่าดิบเท่านั้น
                // เพราะ mouseWorldPos + dragOffset ขยับตามมุมที่มันเพิ่งเอียงไปแล้ว
                if (currentDraggable is FoodInstance swayingFood) swayingFood.UpdateDragSway(mouseWorldPos);

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
            bool isClick = Vector2.Distance(pressDownScreenPos, mouseScreenPos) < ClickMoveThresholdPixels;

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
                    // ช่องเดิมยังจองไว้ให้อยู่ แต่ตัวอาหารเลื่อนตามเมาส์ไปแล้วระหว่างกดค้าง
                    // (จังหวะที่ 2 สั่ง OnDrag ทุกเฟรม ไม่ได้รอให้เกิน threshold ก่อน)
                    // ต้องดึงกลับเข้าช่องให้ตรง ไม่งั้นมันจะค้างอยู่ตรงที่คลิก แล้วเยื้องเพิ่มทุกครั้งที่คลิกซ้ำ
                    food.ReturnToStationSlot();
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

                        // ไต่ขึ้นหาตัวแม่ด้วย เพราะ collider ของเตาอาจอยู่ที่ลูก (Awake ของ GrillStation รองรับเคสนั้นไว้แล้ว)
                        GrillStation grill = hit.collider.GetComponentInParent<GrillStation>();
                        if (grill != null && grill.TrySnapToSlot(food, out Vector3 snapPosG)) { food.SnapToPosition(snapPosG); placed = true; break; }

                        SeasoningStation seasoning = hit.collider.GetComponentInParent<SeasoningStation>();
                        if (seasoning != null && seasoning.TrySnapToSlot(food, out Vector3 snapPosS)) { food.SnapToPosition(snapPosS); placed = true; break; }
                    }

                    // ถ้วยมาก่อนเตา/โต๊ะปรุงรส ตามลำดับเดียวกับตอนไล่ผลเรย์แคสต์ด้านบน
                    // ปล่อยมือในเขต Drop Area ของถ้วย = ตั้งใจใส่ลงถ้วยแน่ๆ ไม่ต้องเล็งให้โดน collider ถ้วยพอดี
                    if (!placed)
                    {
                        Cup[] cups = FindObjectsOfType<Cup>();
                        foreach (Cup nearbyCup in cups)
                        {
                            if (nearbyCup != null && nearbyCup.IsWithinDropArea(mouseWorldPos))
                            {
                                nearbyCup.AddFood(food);
                                placed = true;
                                break;
                            }
                        }
                    }

                    // ปล่อยมือในเขต Drop Area = ตั้งใจวางลงเตาแน่ๆ จึงสั่งข้าม snapRadius (พารามิเตอร์ตัวท้าย)
                    // ถ้าไม่ข้าม เขตที่วางสำเร็จจริงจะเหลือแค่ส่วนที่ Drop Area ทับวงกลมรัศมี snapRadius รอบช่อง
                    // กลายเป็นว่า Drop Area ได้แต่ทำให้เขตวางแคบลง ขยาย collider เท่าไหร่ก็ไม่มีผล
                    if (!placed)
                    {
                        GrillStation[] grills = FindObjectsOfType<GrillStation>();
                        foreach (GrillStation grillStation in grills)
                        {
                            if (grillStation != null && grillStation.IsWithinDropArea(mouseWorldPos) && grillStation.TrySnapToSlot(food, food.Position, out Vector3 snapPosG, true))
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
                            if (seasoning != null && seasoning.IsWithinDropArea(mouseWorldPos) && seasoning.TrySnapToSlot(food, food.Position, out Vector3 snapPosS, true))
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
                        isEasingGrabOffset = false;
                        ClearTrashHighlight();
                        Destroy(food.RootObject);
                        return;
                    }

                    if (!placed)
                    {
                        ReturnFoodToStartPosition(food);
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
                isEasingGrabOffset = false;
                ClearTrashHighlight();
            }
            else if (HasActiveInvestigation())
            {
                Debug.Log("<color=red>ปล่อยเมาส์ เลิกแอบฟัง!</color>");
                currentInvestigation.OnListenEnd();
                ResetInvestigationState();
            }

            if (!HasActiveInvestigation() && currentDraggable == null)
            {
                if (pendingClickable != null)
                {
                    // ยิงจากตัวที่กดลงไปโดนตอนแรก ไม่สนว่าเมาส์เลื่อนไปกี่พิกเซลระหว่างกด
                    // ขอแค่ตอนปล่อยยังอยู่บนตัวเดิม (หรือแทบไม่ขยับเลย) ก็นับเป็นคลิก
                    if (IsPointerStillOnPendingClickable(mouseWorldPos) || isClick)
                    {
                        pendingClickable.OnClick();
                    }
                }
                else if (isClick)
                {
                    // ของที่ลากได้และคลิกได้ในตัวเดียวกัน (เช่น อาหารที่คลิกเพื่อพลิก)
                    // จังหวะกดถูกกินไปโดยระบบลากแล้ว จึงต้องมาหาเอาตอนปล่อยแทน
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

            pendingClickable = null;
            pendingClickableCollider = null;
        }
    }

    // เริ่มไล่ระยะเยื้องจาก "จุดที่คว้าโดนจริง" ไปหา "ปลายทางที่อยากให้เกาะ" แทนที่จะกระโดดไปทันที
    private void BeginGrabOffsetEase(Vector2 fromOffset, Vector2 toOffset, float easeDuration)
    {
        dragOffsetFrom = fromOffset;
        dragOffsetTo = toOffset;
        grabEaseElapsed = 0f;
        grabEaseDuration = easeDuration;
        isEasingGrabOffset = easeDuration > 0f;
        dragOffset = isEasingGrabOffset ? fromOffset : toOffset;
    }

    private void UpdateGrabOffsetEase()
    {
        if (!isEasingGrabOffset) return;

        // ถ้วยสลับเป็น dragSprite ตอนเริ่มลาก จุดกึ่งกลางภาพจึงเพิ่งอัปเดตหลังคว้าไปแล้ว ต้องถามค่าปลายทางใหม่ทุกเฟรม
        if (currentDraggable is Cup easingCup)
        {
            dragOffsetTo = easingCup.VisualCenterOffset;
        }
        // วัตถุดิบก็เปลี่ยนภาพได้ระหว่างทาง (สุกขึ้น/พลิกด้าน) จุดกึ่งกลางจึงต้องถามใหม่ทุกเฟรมเหมือนกัน
        else if (currentDraggable is FoodInstance easingFood)
        {
            dragOffsetTo = easingFood.VisualCenterOffset;
        }

        grabEaseElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(grabEaseElapsed / grabEaseDuration);
        t = t * t * (3f - 2f * t); // smoothstep เพื่อให้ออกตัว/เข้าที่นุ่มนวล ไม่กระตุกหัวท้าย

        dragOffset = Vector2.Lerp(dragOffsetFrom, dragOffsetTo, t);

        if (grabEaseElapsed >= grabEaseDuration)
        {
            dragOffset = dragOffsetTo;
            isEasingGrabOffset = false;
        }
    }

    // ตอนปล่อยเมาส์ยังชี้อยู่บน collider ตัวเดียวกับที่กดลงไปไหม
    private bool IsPointerStillOnPendingClickable(Vector2 mouseWorldPos)
    {
        if (pendingClickableCollider == null) return false;

        RaycastHit2D[] hits = Physics2D.RaycastAll(mouseWorldPos, Vector2.zero);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == pendingClickableCollider) return true;
        }

        return false;
    }

    // วางไม่โดนอะไรเลย: พาอาหารกลับไปที่เดิมตอนก่อนเริ่มลาก
    // ถ้าที่เดิมเป็นช่องบนเตา/โต๊ะปรุงรส ต้องจองช่องคืนด้วย ไม่ใช่แค่เลื่อนตำแหน่งกลับไปทับเฉย ๆ
    private void ReturnFoodToStartPosition(FoodInstance food)
    {
        if (food == null) return;

        // เช็คระยะโดยใช้ startDragPos ตรงๆ (ไม่ใช่ food.Position ปัจจุบัน)
        // เพราะ SnapToPosition ด้านล่างเป็นแค่แอนิเมชันค่อยๆ เลื่อน ตำแหน่งจริงยังไม่ขยับตอนเช็คระยะ
        Vector3 fallbackTargetPos = food.startDragPos;
        bool placed = false;

        RaycastHit2D[] fallBackHits = Physics2D.RaycastAll(food.startDragPos, Vector2.zero);
        foreach (var fbHit in fallBackHits)
        {
            GrillStation g = fbHit.collider.GetComponentInParent<GrillStation>();
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

    // ตัดจบทุกอย่างที่ค้างอยู่กลางคัน (ลากของอยู่ / แอบฟังอยู่) แล้วคืนของกลับที่เดิม
    // เรียกตอนกดหยุดเกม เพราะระหว่างหยุด Update ตัวนี้ไม่ทำงาน จังหวะ "ปล่อยคลิก" เลยหายไปทั้งจังหวะ
    // ถ้าไม่เก็บกวาดตรงนี้ อาหารจะค้างลอยกลางอากาศ และเตาจะยังจองช่องให้ชิ้นที่ไม่ได้อยู่บนเตาแล้ว
    public void CancelActiveInteraction()
    {
        // กดค้างอยู่แล้วโดนตัดจบ (เช่นกดหยุดเกม) ถือว่าคลิกนั้นเป็นโมฆะ
        // ไม่งั้นพอกลับมาเล่นต่อแล้วปล่อยเมาส์ ของที่กดค้างไว้ตั้งแต่ก่อนหยุดจะเด้งทำงานย้อนหลัง
        pendingClickable = null;
        pendingClickableCollider = null;

        if (currentDraggable != null)
        {
            FoodInstance food = currentDraggable as FoodInstance;

            currentDraggable.OnEndDrag();

            if (food != null)
            {
                if (isDraggingFreshFromTray)
                {
                    // วัตถุดิบที่เพิ่งหยิบจากถาดยังไม่มีที่เดิมให้กลับ ปล่อยมือแล้วก็หายไปตามกติกาเดิม
                    Destroy(food.RootObject);
                }
                else if (foodRemovedFromStation)
                {
                    ReturnFoodToStartPosition(food);
                }
                else
                {
                    // ยังไม่ได้ลากออกจากช่องจริง (ขยับไม่ถึง threshold) ช่องเดิมยังจองไว้ให้อยู่
                    food.ReturnToStationSlot();
                }
            }

            currentDraggable = null;
            isDraggingFreshFromTray = false;
            foodRemovedFromStation = false;
            isEasingGrabOffset = false;
            ClearTrashHighlight();
        }

        if (HasActiveInvestigation())
        {
            currentInvestigation.OnListenEnd();
        }

        if (currentInvestigation != null)
        {
            ResetInvestigationState();
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
        // อันนี้ไม่ต้องค่อย ๆ ไล่ระยะเยื้อง เพราะมันเพิ่งเกิดขึ้นมาที่เมาส์อยู่แล้ว ไม่มีจุดเดิมให้กระโดดจาก
        dragOffset = spawnedFood.VisualCenterOffset;
        isEasingGrabOffset = false;
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