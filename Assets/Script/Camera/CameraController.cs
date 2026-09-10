using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [Header("Camera Settings")]
    [Tooltip("ความเร็วพื้นฐานตอนคลิกขวาลากจอ\nค่านี้จะถูกคูณด้วยค่าที่ผู้เล่นตั้งในแผง Settings อีกที (100% = ใช้ค่านี้ตรง ๆ)")]
    [SerializeField] private float panSpeed = 0.5f;
    [Tooltip("ให้ค่าความเร็วแพนที่ผู้เล่นตั้งเองในแผง Settings มามีผลกับกล้องตัวนี้ไหม")]
    [SerializeField] private bool usePlayerPanSpeedSetting = true;
    [SerializeField] private float investigationZoomSize = 3.5f;
    [SerializeField] private float investigationTransitionSpeed = 10f;

    [Header("Pan Smoothing (ความหนืดตอนแพนกล้อง)")]
    [Tooltip("เวลาที่กล้องใช้ไล่ตามตำแหน่งเป้าหมาย (วินาที)\nยิ่งมากยิ่งนุ่มแต่หน่วง, 0 = ตามทันทีแบบเดิม")]
    [SerializeField] private float panSmoothTime = 0.08f;
    [Tooltip("ปล่อยเมาส์แล้วให้กล้องไถลต่อตามแรงลาก")]
    [SerializeField] private bool usePanInertia = true;
    [Tooltip("ความหน่วงของการไถล ยิ่งมากยิ่งหยุดเร็ว")]
    [SerializeField] private float panInertiaDamping = 6f;
    [Tooltip("ให้สวิตช์ความหนืด/แรงไถลในแผง Settings มามีผลกับกล้องตัวนี้ไหม\nติ๊กออกถ้าอยากล็อกความรู้สึกของกล้องตัวนี้ไว้ตามค่าข้างบน")]
    [SerializeField] private bool usePlayerSmoothingSetting = true;

    [Header("Keyboard Pan (เลื่อนกล้องด้วย WASD / ลูกศร)")]
    [SerializeField] private bool useKeyboardPan = true;
    [Tooltip("ความเร็วเลื่อนกล้องด้วยคีย์บอร์ด (หน่วยต่อวินาที)\nคูณด้วยค่าที่ผู้เล่นตั้งในแผง Settings เหมือนการแพนด้วยเมาส์")]
    [SerializeField] private float keyboardPanSpeed = 12f;
    [Tooltip("ให้ปุ่มลูกศรใช้เลื่อนกล้องได้เหมือน WASD")]
    [SerializeField] private bool useArrowKeys = true;

    [Header("Edge Scrolling (ดันขอบจอตอนถือของ)")]
    [SerializeField] private bool useEdgeScrolling = true;
    [SerializeField] private float edgePanSpeed = 15f;
    [SerializeField] private float edgeSize = 40f;

    [Header("Camera Bounds")]
    [SerializeField] private Transform minBoundPoint;
    [SerializeField] private Transform maxBoundPoint;

    private bool isLocked = false;
    private bool isInvestigationBlocked = false;
    private bool isInvestigationFocused = false;
    private bool isReturningFromInvestigation = false;
    private Transform investigationTarget;
    private Vector3 restorePosition;
    private float restoreZoom;
    private Camera cam;

    // ตำแหน่งเป้าหมายของกล้อง อินพุตทุกทางจะไปบวกที่ตัวนี้ แล้วค่อยให้กล้องไล่ตามแบบนุ่ม ๆ
    private Vector3 panTarget;
    private Vector3 panSmoothVelocity;
    private Vector3 panInertia;

    private void Start()
    {
        cam = GetComponent<Camera>();
        restorePosition = transform.position;
        restoreZoom = cam.orthographicSize;
        panTarget = transform.position;
    }

    private void Update()
    {
        // หยุดเกมอยู่: ห้ามแพนกล้อง ไม่งั้นดันเมาส์ไปโดนขอบจอตอนเล็งปุ่มในแผงหยุดเกม
        // แล้วกล้องจะไถลออกไปเอง (การซูมเข้า/ออกตอนสืบสวนก็ต้องหยุดค้างไว้เหมือนกัน)
        if (PauseMenuController.IsPaused)
        {
            StopPanMotion();
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            StopPanMotion();
            return;
        }

        if (isInvestigationFocused || isReturningFromInvestigation)
        {
            UpdateInvestigationFocus();
            // ระหว่างสืบสวนกล้องถูกสั่งตำแหน่งตรง ๆ ต้องลากเป้าหมายแพนตามไปด้วย
            // ไม่งั้นพอกลับมาแพนเองกล้องจะกระตุกกลับไปจุดเดิมก่อนซูม
            StopPanMotion();
            return;
        }

        if (isLocked || isInvestigationBlocked)
        {
            StopPanMotion();
            return;
        }

        // คีย์บอร์ดทำงานแยกจากเมาส์ กดค้างไว้แล้วลากเมาส์พร้อมกันก็ยังบวกกันได้
        bool keyboardMoving = HandleKeyboardPan();

        if (Mouse.current == null)
        {
            if (!keyboardMoving) UpdatePanInertia();
            ApplyPanSmoothing();
            return;
        }

        if (Mouse.current.rightButton.isPressed)
        {
            // อ่านค่าที่ผู้เล่นตั้งทุกเฟรม ลากสไลเดอร์ในแผงตั้งค่าแล้วกล้องจะเปลี่ยนความเร็วทันที
            float speed = panSpeed * PanSpeedScale;
            Vector2 dragDelta = Mouse.current.delta.ReadValue();
            Vector3 moveDelta = new Vector3(-dragDelta.x * speed, -dragDelta.y * speed, 0) * Time.deltaTime;
            TrackPanInertia(moveDelta);
            MovePanTarget(moveDelta);
        }
        else if (useEdgeScrolling && Mouse.current.leftButton.isPressed)
        {
            HandleEdgeScrolling();
            panInertia = Vector3.zero;
        }
        else if (!keyboardMoving)
        {
            UpdatePanInertia();
        }

        ApplyPanSmoothing();
    }

    // เลื่อนกล้องด้วย WASD / ลูกศร คืนค่า true ถ้าเฟรมนี้มีการกดปุ่มอยู่
    private bool HandleKeyboardPan()
    {
        if (!useKeyboardPan || Keyboard.current == null) return false;

        Vector3 moveDirection = Vector3.zero;

        if (Keyboard.current.aKey.isPressed) moveDirection.x -= 1f;
        if (Keyboard.current.dKey.isPressed) moveDirection.x += 1f;
        if (Keyboard.current.sKey.isPressed) moveDirection.y -= 1f;
        if (Keyboard.current.wKey.isPressed) moveDirection.y += 1f;

        if (useArrowKeys)
        {
            if (Keyboard.current.leftArrowKey.isPressed) moveDirection.x -= 1f;
            if (Keyboard.current.rightArrowKey.isPressed) moveDirection.x += 1f;
            if (Keyboard.current.downArrowKey.isPressed) moveDirection.y -= 1f;
            if (Keyboard.current.upArrowKey.isPressed) moveDirection.y += 1f;
        }

        if (moveDirection == Vector3.zero) return false;

        // กดสองปุ่มพร้อมกัน (เช่น W+D) ต้องไม่เร็วกว่ากดปุ่มเดียว เลยต้องนอร์มัลไลซ์ก่อน
        // กดปุ่มตรงข้ามพร้อมกันจะหักล้างกันเป็นศูนย์ ซึ่งถูกดักไปแล้วข้างบน
        moveDirection.Normalize();
        MovePanTarget(moveDirection * (keyboardPanSpeed * PanSpeedScale) * Time.deltaTime);

        // กดปุ่มอยู่แล้วยังปล่อยให้แรงไถลจากเมาส์ดันกล้องต่อ ทิศทางจะตีกัน
        panInertia = Vector3.zero;
        return true;
    }

    // เก็บความเร็วลากล่าสุด (หน่วยต่อวินาที) ไว้ใช้ไถลต่อหลังปล่อยเมาส์
    private void TrackPanInertia(Vector3 moveDelta)
    {
        if (!PanInertiaOn || Time.deltaTime <= 0f)
        {
            panInertia = Vector3.zero;
            return;
        }

        Vector3 dragVelocity = moveDelta / Time.deltaTime;
        // ถัวเฉลี่ยแบบไม่ขึ้นกับเฟรมเรต กันเมาส์สะบัดเฟรมเดียวแล้วกล้องพุ่ง
        float blend = 1f - Mathf.Exp(-20f * Time.deltaTime);
        panInertia = Vector3.Lerp(panInertia, dragVelocity, blend);
    }

    private void UpdatePanInertia()
    {
        // ผู้เล่นปิดแรงไถลตอนกล้องกำลังไถลอยู่ ต้องล้างค่าที่ค้างทิ้ง ไม่งั้นมันจะรอปะทุตอนเปิดกลับ
        if (!PanInertiaOn)
        {
            panInertia = Vector3.zero;
            return;
        }

        if (panInertia == Vector3.zero) return;

        MovePanTarget(panInertia * Time.deltaTime);
        panInertia *= Mathf.Exp(-panInertiaDamping * Time.deltaTime);

        if (panInertia.sqrMagnitude < 0.0001f) panInertia = Vector3.zero;
    }

    private void ApplyPanSmoothing()
    {
        float smoothTime = PanSmoothOn ? panSmoothTime : 0f;

        if (smoothTime <= 0f)
        {
            transform.position = panTarget;
            panSmoothVelocity = Vector3.zero;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, panTarget, ref panSmoothVelocity, smoothTime);
    }

    // ยกเลิกแรงไถลและดึงเป้าหมายมาที่กล้องปัจจุบัน ใช้ตอนกล้องถูกสั่งจากที่อื่นหรือหยุดรับอินพุต
    private void StopPanMotion()
    {
        panTarget = transform.position;
        panSmoothVelocity = Vector3.zero;
        panInertia = Vector3.zero;
    }

    private void HandleEdgeScrolling()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 moveDirection = Vector3.zero;

        if (mousePos.x <= edgeSize) moveDirection.x = -1;
        else if (mousePos.x >= Screen.width - edgeSize) moveDirection.x = 1;

        if (mousePos.y <= edgeSize) moveDirection.y = -1;
        else if (mousePos.y >= Screen.height - edgeSize) moveDirection.y = 1;

        if (moveDirection != Vector3.zero)
        {
            MovePanTarget(moveDirection * (edgePanSpeed * PanSpeedScale) * Time.deltaTime);
        }
    }

    // ตัวคูณความเร็วแพนที่ผู้เล่นตั้งไว้ในแผง Settings (1 = ปกติ)
    private float PanSpeedScale
    {
        get { return usePlayerPanSpeedSetting ? CameraSettings.PanSpeedScale : 1f; }
    }

    // ช่องใน Inspector เป็นสวิตช์ตัวใหญ่ ถ้าปิดตรงนั้นผู้เล่นจะเปิดจากแผง Settings ไม่ได้เลย
    // อ่านทุกเฟรม กดสวิตช์ในแผงตั้งค่าแล้วกล้องเปลี่ยนความรู้สึกทันที
    private bool PanSmoothOn
    {
        get { return usePlayerSmoothingSetting ? CameraSettings.PanSmoothEnabled : true; }
    }

    private bool PanInertiaOn
    {
        get
        {
            if (!usePanInertia) return false;
            return usePlayerSmoothingSetting ? CameraSettings.PanInertiaEnabled : true;
        }
    }

    public void SetCameraLock(bool state)
    {
        isLocked = state;
    }

    public void SetInvestigationInputBlocked(bool state)
    {
        isInvestigationBlocked = state;
    }

    public void BeginInvestigationFocus(Transform target)
    {
        if (target == null || cam == null) return;

        if (!isInvestigationFocused && !isReturningFromInvestigation)
        {
            restorePosition = transform.position;
            restoreZoom = cam.orthographicSize;
        }

        investigationTarget = target;
        isInvestigationFocused = true;
        isReturningFromInvestigation = false;
    }

    public void EndInvestigationFocus()
    {
        if (cam == null) return;

        if (!isInvestigationFocused && !isReturningFromInvestigation) return;

        isInvestigationFocused = false;
        isReturningFromInvestigation = true;
        investigationTarget = null;
    }

    private void UpdateInvestigationFocus()
    {
        if (cam == null) return;

        if (isInvestigationFocused && investigationTarget == null)
        {
            EndInvestigationFocus();
            return;
        }

        Vector3 targetPosition = isInvestigationFocused
            ? new Vector3(investigationTarget.position.x, investigationTarget.position.y, transform.position.z)
            : restorePosition;

        float targetZoom = isInvestigationFocused ? investigationZoomSize : restoreZoom;

        transform.position = Vector3.Lerp(transform.position, targetPosition, investigationTransitionSpeed * Time.deltaTime);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, investigationTransitionSpeed * Time.deltaTime);

        bool positionReached = Vector3.Distance(transform.position, targetPosition) < 0.01f;
        bool zoomReached = Mathf.Abs(cam.orthographicSize - targetZoom) < 0.01f;

        if (!isInvestigationFocused && positionReached && zoomReached)
        {
            transform.position = restorePosition;
            cam.orthographicSize = restoreZoom;
            isReturningFromInvestigation = false;
        }
    }

    private void MovePanTarget(Vector3 moveAmount)
    {
        if (minBoundPoint == null || maxBoundPoint == null) return;

        // ถ้ามีสคริปต์อื่นย้ายกล้องไปไกล (เช่นเปลี่ยนฉาก) ให้รีเซ็ตเป้าหมายก่อน
        // ไม่งั้นกล้องจะถูกดูดกลับไปหาเป้าหมายเก่า
        if ((panTarget - transform.position).sqrMagnitude > 100f)
        {
            panTarget = transform.position;
            panSmoothVelocity = Vector3.zero;
        }

        Vector3 pos = panTarget;
        pos += moveAmount;

        float camHalfHeight = cam.orthographicSize;
        float camHalfWidth = cam.aspect * camHalfHeight;

        float clampMinX = minBoundPoint.position.x + camHalfWidth;
        float clampMaxX = maxBoundPoint.position.x - camHalfWidth;
        float clampMinY = minBoundPoint.position.y + camHalfHeight;
        float clampMaxY = maxBoundPoint.position.y - camHalfHeight;

        if (clampMinX > clampMaxX) clampMinX = clampMaxX = (minBoundPoint.position.x + maxBoundPoint.position.x) / 2f;
        if (clampMinY > clampMaxY) clampMinY = clampMaxY = (minBoundPoint.position.y + maxBoundPoint.position.y) / 2f;

        pos.x = Mathf.Clamp(pos.x, clampMinX, clampMaxX);
        pos.y = Mathf.Clamp(pos.y, clampMinY, clampMaxY);

        // ชนขอบแล้วให้แรงไถลด้านนั้นดับ กล้องจะได้ไม่ค้างดันขอบอยู่
        if (!Mathf.Approximately(pos.x, panTarget.x + moveAmount.x)) panInertia.x = 0f;
        if (!Mathf.Approximately(pos.y, panTarget.y + moveAmount.y)) panInertia.y = 0f;

        panTarget = pos;
    }

    private void OnDrawGizmos()
    {
        if (minBoundPoint != null && maxBoundPoint != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 minPos = minBoundPoint.position;
            Vector3 maxPos = maxBoundPoint.position;

            Gizmos.DrawLine(new Vector3(minPos.x, minPos.y, 0), new Vector3(maxPos.x, minPos.y, 0));
            Gizmos.DrawLine(new Vector3(maxPos.x, minPos.y, 0), new Vector3(maxPos.x, maxPos.y, 0));
            Gizmos.DrawLine(new Vector3(maxPos.x, maxPos.y, 0), new Vector3(minPos.x, maxPos.y, 0));
            Gizmos.DrawLine(new Vector3(minPos.x, maxPos.y, 0), new Vector3(minPos.x, minPos.y, 0));
        }
    }
}