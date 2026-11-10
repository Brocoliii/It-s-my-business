using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

// สถานะของตัวชี้เมาส์ เรียงตามลำดับความสำคัญจากน้อยไปมาก (ตัวหลังชนะตัวหน้าเวลาเข้าเงื่อนไขพร้อมกัน)
public enum GameCursorState
{
    Default,        // ลูกศรปกติ
    Interactable,   // มือชี้ - ชี้อยู่บนของที่กด/ลากได้ หรือปุ่มบน UI
    Investigate,    // แว่นขยาย - ชี้อยู่บนคนที่แอบฟังได้
    Drag            // มือกำ - มีของอยู่ในมือจริง ๆ
}

// ตัวจัดการรูปเมาส์ของทั้งเกม
//
// วิธีใช้: ลากสคริปต์นี้ใส่ GameObject เปล่า ๆ สัก 1 ตัวในฉากแรก แล้วใส่รูปทั้ง 4 ช่องใน Inspector
// ตัวนี้ข้ามฉากไปด้วย (DontDestroyOnLoad) วางไว้ทุกฉากก็ได้ ตัวที่เกิดทีหลังจะลบตัวเองทิ้ง
//
// สำคัญ: รูปเมาส์ทุกใบต้องตั้ง Texture Type เป็น "Cursor" ใน Inspector ของไฟล์รูป
// ไม่งั้น Unity จะบีบอัดรูปจนใช้เป็นเมาส์ไม่ได้ (จะขึ้นเตือนใน Console ให้ตอนเริ่มเกม)
//
// รูปมาจากไหน: มีชุดรูปสำรองแบบพิกเซลอาร์ตให้แล้วที่ UI/Cursors ใช้ทดสอบไปก่อนได้
// พอมีรูปจริงจากฝ่ายอาร์ตแล้วค่อยสลับในช่อง Inspector ได้เลย ไม่ต้องแก้โค้ด
//
// ตัวนี้อ่านสถานะจาก InputManager ตัวเดียวกับที่คุมการลาก/การแอบฟังจริง
// จึงไม่มีทางที่รูปเมาส์จะบอกว่า "กดได้" ทั้งที่กดไปแล้วไม่มีอะไรเกิดขึ้น
//
// ขนาดเมาส์ตามความละเอียดหน้าจอ: เมาส์ฮาร์ดแวร์ของระบบปฏิบัติการวาดตามพิกเซลจริง ไม่ขยายตามจอ
// (เมาส์ 32x32 บน Full HD ดูพอดี แต่บน 4K จะเล็กลงครึ่งนึง) ตัวนี้จึงขยายรูปเป็นเท่าตัวเต็มตาม Screen.height
// ให้เอง (1080p = 1x, 4K = 2x) โดยขยายแบบ Point เพื่อให้พิกเซลอาร์ตยังคมอยู่
public class CursorManager : MonoBehaviour
{
    // จุดที่ถือว่าเป็น "ปลายเมาส์" จริง ๆ นับเป็นพิกเซลจากมุมซ้ายบนของรูป
    // ถ้าไม่ตั้งให้ตรงกับปลายลูกศร/ปลายนิ้ว ผู้เล่นจะกดแล้วรู้สึกว่าเมาส์เพี้ยนไปจากที่เห็น
    [System.Serializable]
    public class CursorArt
    {
        [Tooltip("รูปเมาส์ (Texture Type ต้องเป็น Cursor)")]
        public Texture2D texture;

        [Tooltip("จุดชี้จริงของรูป นับเป็นพิกเซลจากมุมซ้ายบน (นับที่ขนาดรูปต้นฉบับ ระบบขยายให้เองตามสเกล)\nลูกศร = (0,0) / ปลายนิ้ว = ตรงปลายนิ้วชี้ / แว่นขยาย = กลางเลนส์ / มือกำ = กลางฝ่ามือ")]
        public Vector2 hotspot;
    }

    public static CursorManager Instance { get; private set; }

    // ระบบปฏิบัติการวาดเมาส์ฮาร์ดแวร์ได้ไม่เกินขนาดนี้ ใหญ่กว่านี้ต้องให้เกมวาดเอง
    private const int MaxHardwareCursorSize = 32;

    [Header("รูปเมาส์แต่ละสถานะ")]
    [Tooltip("ลูกศรปกติ (ค่า hotspot ที่เข้ากับรูปสำรอง: 0,0)")]
    [SerializeField] private CursorArt defaultCursor = new CursorArt();

    [Tooltip("มือชี้ ตอนชี้ของที่กด/ลากได้ (ค่า hotspot ที่เข้ากับรูปสำรอง: 13,1)")]
    [SerializeField] private CursorArt interactableCursor = new CursorArt();

    [Tooltip("แว่นขยาย ตอนชี้คนที่แอบฟังได้ (ค่า hotspot ที่เข้ากับรูปสำรอง: 9,8)")]
    [SerializeField] private CursorArt investigateCursor = new CursorArt();

    [Tooltip("มือกำ ตอนมีของอยู่ในมือ (ค่า hotspot ที่เข้ากับรูปสำรอง: 14,14)")]
    [SerializeField] private CursorArt dragCursor = new CursorArt();

    [Header("ตั้งค่า")]
    [Tooltip("Auto = ให้ระบบปฏิบัติการวาดให้ถ้าทำได้ (ลื่นที่สุด ไม่มีอาการเมาส์ตามไม่ทัน)\nForceSoftware = ให้เกมวาดเอง ใช้ตอนอยากได้รูปใหญ่เกิน 32x32\nถ้าเลือก Auto แล้วรูป (หลังขยาย) ใหญ่เกิน 32x32 ระบบจะสลับเป็น ForceSoftware ให้เองเฉพาะรูปนั้น")]
    [SerializeField] private CursorMode cursorMode = CursorMode.Auto;

    [Tooltip("ปิดได้ถ้าอยากคุมรูปเมาส์เองทั้งหมดผ่าน ForceState()")]
    [SerializeField] private bool autoDetectState = true;

    [Header("ขนาดตามความละเอียดจอ")]
    [Tooltip("เปิด = ขยายรูปเมาส์เป็นเท่าตัวเต็มตามความสูงของจอ ให้ขนาดที่ตาเห็นใกล้เคียงกันทุกความละเอียด")]
    [SerializeField] private bool scaleWithResolution = true;

    [Tooltip("ความสูงจอที่ถือว่าเมาส์เป็นขนาด 1 เท่า (1080 = Full HD)\nสเกล = Screen.height / ค่านี้ ปัดเป็นจำนวนเต็ม เช่น 1080p = 1x, 4K = 2x\nอยากให้ 1440p ใหญ่ขึ้นด้วยให้ลดค่านี้ เช่น 720 จะได้ 1440p = 2x, 4K = 3x")]
    [SerializeField] private float referenceHeight = 1080f;

    [Tooltip("สเกลสูงสุดที่อนุญาต กันรูปใหญ่เกินเวลาเจอจอความละเอียดสูงมาก ๆ")]
    [SerializeField, Range(1, 8)] private int maxScale = 4;

    // จำค่าที่ "สั่งไปแล้วจริง ๆ" ไว้ เพราะ Cursor.SetCursor ต้องยิงเฉพาะตอนเปลี่ยนเท่านั้น
    // ยิงทุกเฟรมจะทำให้เมาส์กะพริบบนบางเครื่อง (โดยเฉพาะโหมด ForceSoftware)
    private GameCursorState appliedState = GameCursorState.Default;
    private Texture2D appliedTexture;
    private Vector2 appliedHotspot;
    private bool hasAppliedOnce;

    private GameCursorState forcedState;
    private bool hasForcedState;

    // ของบน UI ที่กำลังถูกลากอยู่ (เช่นตราปั๊มในสมุด) การลากบน UI วิ่งผ่าน EventSystem
    // คนละทางกับ InputManager ที่คุมของในฉาก จึงให้ตัวที่ถูกลากมาบอกเองตอนเริ่ม/จบ
    // แทนที่จะให้ตรงนี้ไล่หาทุกเฟรม (ของพวกนี้มักอยู่ในแผงที่ปิดไว้ หาไม่เจอด้วยซ้ำ)
    private MonoBehaviour activeUiDragOwner;

    private Camera mainCam;
    private InputManager inputManager;

    // ของที่ใช้ยิงเรย์หา UI ใต้เมาส์ สร้างครั้งเดียวแล้วใช้ซ้ำ เพราะงานนี้วิ่งทุกเฟรม
    private PointerEventData uiPointerData;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    // รูปไหนตรวจแล้วว่าใช้เป็นเมาส์ได้/ไม่ได้ จำไว้เลย จะได้ไม่ต้องตรวจซ้ำทุกเฟรม
    // และรูปที่ใช้ไม่ได้จะเตือนแค่ครั้งเดียวต่อรูป ไม่ถล่ม Console
    private readonly Dictionary<Texture2D, bool> cursorTextureUsable = new Dictionary<Texture2D, bool>();

    // รูปที่ขยายแล้ว เก็บไว้ใช้ซ้ำ (key = รูปต้นฉบับ + สเกล) ไม่งั้นต้องสร้างใหม่ทุกครั้งที่เปลี่ยนสถานะ
    // รูปพวกนี้เราสร้างเอง จึงต้อง Destroy เองตอนปิดเกม
    private readonly Dictionary<(Texture2D, int), Texture2D> scaledCache = new Dictionary<(Texture2D, int), Texture2D>();

    // ความสูงจอรอบก่อน ใช้จับว่าผู้เล่นเปลี่ยนความละเอียด/สลับเต็มจอ เพื่อสั่งเมาส์ใหม่ด้วยสเกลที่ถูกต้อง
    private int lastScreenHeight;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // วางสคริปต์ไว้ทุกฉากได้ ตัวที่ข้ามฉากมาก่อนคือตัวจริง ตัวใหม่ลบตัวเองทิ้ง
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // ฉากไหนเผลอซ่อนเมาส์ไว้ ระบบนี้จะไม่มีความหมายเลย จึงเปิดให้เห็นไว้ตั้งแต่ต้น
        Cursor.visible = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        lastScreenHeight = Screen.height;
        WarnAboutUnusableTextures();
        ApplyState(GameCursorState.Default);
    }

    private void OnDestroy()
    {
        if (Instance != this) return;

        Instance = null;
        // คืนเมาส์ปกติของเครื่องตอนปิดเกม ไม่งั้นรูปที่ตั้งไว้อาจค้างอยู่บน Editor หลังกดหยุดเล่น
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        // ทำลายรูปที่ขยายไว้ทิ้ง (ต้องทำหลังคืนเมาส์ปกติ จะได้ไม่มีเมาส์ที่ชี้รูปที่ถูกลบไปแล้ว)
        foreach (Texture2D scaled in scaledCache.Values)
        {
            if (scaled != null) Destroy(scaled);
        }
        scaledCache.Clear();
    }

    // ตัวนี้ข้ามฉากไปด้วย แต่ InputManager กับกล้องเป็นของประจำฉาก ต้องหาใหม่ทุกครั้งที่เปลี่ยนฉาก
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        mainCam = null;
        inputManager = null;

        // เปลี่ยนฉากทั้งทีที่ยังลากของบน UI ค้างอยู่ ถือว่าจบการลากไปด้วย
        activeUiDragOwner = null;
    }

    // สลับกลับไปเป็นเมาส์ปกติของเครื่องตอนสลับหน้าต่างออกไปแล้วกลับเข้ามา
    // Windows คืนรูปเมาส์เป็นค่าเริ่มต้นให้เองตอนเปลี่ยนโฟกัส ถ้าไม่สั่งซ้ำ รูปที่ตั้งไว้จะหายไปเฉย ๆ
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;

        hasAppliedOnce = false;
        ApplyState(appliedState);
    }

    // ใช้ตอนอยากบังคับรูปเมาส์เอง เช่นระหว่างฉากคัตซีนที่ไม่อยากให้เมาส์เปลี่ยนไปมา
    public void ForceState(GameCursorState state)
    {
        forcedState = state;
        hasForcedState = true;
    }

    // เลิกบังคับ กลับไปให้ระบบเลือกรูปตามของที่ชี้อยู่ตามปกติ
    public void ClearForcedState()
    {
        hasForcedState = false;
    }

    // ของบน UI เรียกตอนเริ่มถูกลาก เพื่อให้เมาส์เปลี่ยนเป็นรูปมือกำ (ดู StampConfirmUI เป็นตัวอย่าง)
    public void BeginUiDrag(MonoBehaviour owner)
    {
        if (owner == null) return;

        activeUiDragOwner = owner;
    }

    // เรียกตอนปล่อยมือ ต้องเรียกตอนถูกปิด/ถูกรีเซ็ตด้วย ไม่งั้นรูปมือกำจะค้างอยู่ทั้งที่ไม่ได้ถืออะไรแล้ว
    public void EndUiDrag(MonoBehaviour owner)
    {
        // เช็คว่าเป็นตัวเดิมที่จองไว้จริง กันของอีกชิ้นที่จบทีหลังมาล้างการลากที่กำลังทำอยู่
        if (activeUiDragOwner != owner) return;

        activeUiDragOwner = null;
    }

    // ใช้ LateUpdate เพราะ InputManager คว้า/ปล่อยของใน Update
    // ถ้าเช็คใน Update ด้วยกัน ลำดับการวิ่งของสองสคริปต์จะไม่แน่นอน รูปมือกำจะช้าไป 1 เฟรมบ้างไม่ช้าบ้าง
    private void LateUpdate()
    {
        // ความละเอียดเปลี่ยน (ปรับในตั้งค่าเกม / สลับเต็มจอ / ลากย่อขยายหน้าต่าง)
        // ต้องสั่งเมาส์ใหม่ด้วยสเกลใหม่ ไม่งั้นเมาส์จะค้างขนาดเดิมไปเรื่อย ๆ
        if (Screen.height != lastScreenHeight)
        {
            lastScreenHeight = Screen.height;
            hasAppliedOnce = false;
        }

        if (!autoDetectState && !hasForcedState)
        {
            // โหมดคุมเองแต่ยังไม่ได้บังคับอะไร ถ้าความละเอียดเปลี่ยนก็ยังต้องสั่งรูปเดิมใหม่ให้ขนาดถูก
            if (!hasAppliedOnce) ApplyState(appliedState);
            return;
        }

        ApplyState(ResolveState());
    }

    private GameCursorState ResolveState()
    {
        if (hasForcedState) return forcedState;

        // ของบน UI ที่ถูกลากอยู่ ต้องมาก่อนทุกอย่าง เพราะระหว่างลาก ตัวมันปิดการรับเรย์ไว้
        // (blocksRaycasts = false) หาจากสิ่งที่อยู่ใต้เมาส์ยังไงก็ไม่เจอ
        // เทียบ null ทุกครั้งเผื่อเจ้าของถูกทำลายไปกลางคัน เช่นเปลี่ยนฉากขณะยังลากค้างอยู่
        if (activeUiDragOwner != null) return GameCursorState.Drag;

        InputManager input = GetInputManager();

        // สองอันนี้ถามจากตัวที่คุมของจริง ไม่ใช่เดาจากสิ่งที่อยู่ใต้เมาส์
        // เพราะระหว่างลาก เมาส์วิ่งเร็ว ๆ อาจหลุดออกนอกตัวของที่ถืออยู่ได้ แต่มือยังกำของอยู่
        if (input != null)
        {
            if (input.IsDragging) return GameCursorState.Drag;
            if (input.IsInvestigating) return GameCursorState.Investigate;
        }

        // ปุ่มบน UI ยังกดได้ตลอด แม้ตอนหยุดเกมหรือจบเกม จึงเช็คก่อนเงื่อนไขสถานะเกมด้านล่าง
        if (IsPointerOverLiveUI()) return GameCursorState.Interactable;

        if (!IsWorldInteractionAllowed()) return GameCursorState.Default;

        return ResolveWorldState();
    }

    // เงื่อนไขเดียวกับที่ InputManager.Update ใช้เด้งออกตั้งแต่ต้นฟังก์ชัน
    // ถ้าตรงนี้ไม่ตรงกับตรงนั้น เมาส์จะโชว์ว่ากดได้ทั้งที่กดไปก็ไม่มีอะไรเกิดขึ้น
    private bool IsWorldInteractionAllowed()
    {
        if (PauseMenuController.IsPaused) return false;

        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
        {
            return false;
        }

        return true;
    }

    // ไล่ลำดับเดียวกับตอน InputManager กดเมาส์ลง (ลากได้ > ถาด > แอบฟังได้ > กดได้)
    // ของชิ้นเดียวที่เป็นได้หลายอย่างพร้อมกันจะได้โชว์รูปตรงกับสิ่งที่กดแล้วเกิดขึ้นจริง
    private GameCursorState ResolveWorldState()
    {
        Camera cam = GetMainCamera();
        if (cam == null || Mouse.current == null) return GameCursorState.Default;

        Vector2 mouseWorldPos = cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorldPos);

        if (hits.Length == 0) return GameCursorState.Default;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            IDraggable draggable = hit.GetComponent<IDraggable>();
            if (draggable == null) draggable = hit.GetComponentInParent<IDraggable>();
            if (draggable != null) return GameCursorState.Interactable;
        }

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            FoodTray tray = hit.GetComponent<FoodTray>();
            if (tray == null) tray = hit.GetComponentInParent<FoodTray>();
            if (tray != null) return GameCursorState.Interactable;

            CatTreatTray catTreatTray = hit.GetComponent<CatTreatTray>();
            if (catTreatTray == null) catTreatTray = hit.GetComponentInParent<CatTreatTray>();
            if (catTreatTray != null) return GameCursorState.Interactable;
        }

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            // ไม่ไต่ขึ้นหาตัวแม่ ให้ตรงกับ InputManager ที่มองแค่ตัว collider ตรง ๆ
            if (hit.GetComponent<IInvestigatable>() != null) return GameCursorState.Investigate;
        }

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;

            if (hit.GetComponent<IClickable>() != null) return GameCursorState.Interactable;
        }

        return GameCursorState.Default;
    }

    // ชี้อยู่บนปุ่ม UI ที่กดได้จริงไหม (ปุ่มที่ถูกปิดไว้ไม่นับ ผู้เล่นจะได้ไม่หลงกดซ้ำ ๆ)
    private bool IsPointerOverLiveUI()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null) return false;

        if (uiPointerData == null) uiPointerData = new PointerEventData(eventSystem);
        uiPointerData.position = Mouse.current.position.ReadValue();

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(uiPointerData, uiRaycastResults);

        foreach (RaycastResult result in uiRaycastResults)
        {
            if (result.gameObject == null) continue;

            // ไต่ขึ้นหาตัวแม่ด้วย เพราะข้อความ/ไอคอนในปุ่มเป็นตัวที่โดนเรย์ ไม่ใช่ตัวปุ่มเอง
            Selectable selectable = result.gameObject.GetComponentInParent<Selectable>();
            if (selectable != null && selectable.IsInteractable()) return true;

            // ตราปั๊มไม่ใช่ปุ่ม (ไม่มี Selectable ติดอยู่) แต่ผู้เล่นคว้ามันได้จริง จึงต้องเช็คแยก
            // ตัวที่ปั๊มลงไปแล้ว (isLocked) คว้าไม่ได้อีก จะได้ไม่หลอกให้ผู้เล่นไปลากซ้ำ
            StampConfirmUI stamp = result.gameObject.GetComponentInParent<StampConfirmUI>();
            if (stamp != null && !stamp.IsLocked) return true;
        }

        return false;
    }

    private void ApplyState(GameCursorState state)
    {
        CursorArt art = GetArt(state);

        // ช่องไหนยังไม่ได้ใส่รูป ให้ถอยไปใช้ลูกศรปกติ ดีกว่าเมาส์หายไปเฉย ๆ ตอนชี้ของบางอย่าง
        if (art == null || art.texture == null) art = defaultCursor;

        Texture2D texture = art != null ? art.texture : null;
        Vector2 hotspot = art != null ? art.hotspot : Vector2.zero;

        // รูปที่ Unity รับไม่ได้ ต้องไม่ส่งเข้า SetCursor เลย ไม่งั้นมันจะด่าออก Console ทุกครั้งที่สั่ง
        // และเพราะทุกสถานะที่รูปเสียถูกยุบเหลือ (null, 0,0) เหมือนกันหมด ตัวกันสั่งซ้ำด้านล่างจึงกันได้จริง
        // แม้ผู้เล่นจะกวาดเมาส์เข้าออกจนสถานะสลับไปมาทุกเฟรม
        if (texture != null && !CanUseAsCursor(texture))
        {
            texture = null;
            hotspot = Vector2.zero;
        }

        // ขยายรูปตามความละเอียดจอ (ทำหลังตรวจรูปแล้ว เพราะต้องอ่านพิกเซลจากรูปต้นฉบับที่ใช้ได้เท่านั้น)
        // hotspot ต้องคูณสเกลตามด้วย ไม่งั้นปลายเมาส์จะเลื่อนไปจากที่ตาเห็น
        if (texture != null && scaleWithResolution)
        {
            int scale = GetScale();
            if (scale > 1)
            {
                Texture2D scaled = GetScaledTexture(texture, scale);
                if (scaled != null)
                {
                    texture = scaled;
                    hotspot *= scale;
                }
            }
        }

        // รูปใหญ่เกิน 32x32 ระบบปฏิบัติการวาดให้ไม่ได้ ถ้าตั้ง Auto ไว้ให้สลับเป็นเกมวาดเองเฉพาะรูปนี้
        CursorMode mode = cursorMode;
        if (texture != null && mode == CursorMode.Auto &&
            (texture.width > MaxHardwareCursorSize || texture.height > MaxHardwareCursorSize))
        {
            mode = CursorMode.ForceSoftware;
        }

        if (hasAppliedOnce && appliedTexture == texture && appliedHotspot == hotspot)
        {
            appliedState = state;
            return;
        }

        Cursor.SetCursor(texture, hotspot, mode);

        appliedState = state;
        appliedTexture = texture;
        appliedHotspot = hotspot;
        hasAppliedOnce = true;
    }

    // สเกลเป็นจำนวนเต็มเสมอ เพื่อให้พิกเซลอาร์ตคม ไม่เบลอ (1080p = 1x, 4K = 2x)
    private int GetScale()
    {
        float safeReference = Mathf.Max(1f, referenceHeight);
        int scale = Mathf.RoundToInt(Screen.height / safeReference);
        return Mathf.Clamp(scale, 1, maxScale);
    }

    // ขยายรูปแบบ Point (ก๊อปพิกเซลซ้ำเป็นบล็อก) แล้วจำไว้ใช้ซ้ำ
    // ต้องอ่านพิกเซลจากรูปต้นฉบับ ซึ่งผ่าน CanUseAsCursor มาแล้วว่า Read/Write เปิดอยู่
    private Texture2D GetScaledTexture(Texture2D source, int scale)
    {
        var key = (source, scale);
        if (scaledCache.TryGetValue(key, out Texture2D cached) && cached != null) return cached;

        int srcW = source.width;
        int srcH = source.height;
        int dstW = srcW * scale;
        int dstH = srcH * scale;

        Color32[] srcPixels = source.GetPixels32();
        Color32[] dstPixels = new Color32[dstW * dstH];

        for (int y = 0; y < dstH; y++)
        {
            int srcRow = (y / scale) * srcW;
            int dstRow = y * dstW;

            for (int x = 0; x < dstW; x++)
            {
                dstPixels[dstRow + x] = srcPixels[srcRow + (x / scale)];
            }
        }

        // ต้องเป็น RGBA32 ไม่มี mipmap ตรงตามที่ Cursor.SetCursor ต้องการ
        Texture2D result = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false);
        result.name = source.name + "_x" + scale;
        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;
        result.SetPixels32(dstPixels);
        result.Apply(false, false); // ต้องคงไว้ให้อ่านได้ (makeNoLongerReadable = false) ไม่งั้นใช้เป็นเมาส์ไม่ได้

        scaledCache[key] = result;
        return result;
    }

    private CursorArt GetArt(GameCursorState state)
    {
        switch (state)
        {
            case GameCursorState.Interactable: return interactableCursor;
            case GameCursorState.Investigate: return investigateCursor;
            case GameCursorState.Drag: return dragCursor;
            default: return defaultCursor;
        }
    }

    private InputManager GetInputManager()
    {
        if (inputManager == null) inputManager = FindObjectOfType<InputManager>();
        return inputManager;
    }

    private Camera GetMainCamera()
    {
        if (mainCam == null) mainCam = Camera.main;
        return mainCam;
    }

    // อาการ "ใส่รูปครบแล้วแต่เมาส์ยังเป็นลูกศรของ Windows" เกือบทั้งหมดมาจากตั้ง Texture Type ผิด
    // เตือนตั้งแต่เริ่มเกมจะได้ไม่ต้องไปนั่งไล่หาสาเหตุในโค้ด
    private void WarnAboutUnusableTextures()
    {
        WarnIfUnusable(defaultCursor, "ลูกศรปกติ");
        WarnIfUnusable(interactableCursor, "มือชี้");
        WarnIfUnusable(investigateCursor, "แว่นขยาย");
        WarnIfUnusable(dragCursor, "มือกำ");
    }

    private void WarnIfUnusable(CursorArt art, string label)
    {
        if (art == null || art.texture == null)
        {
            Debug.LogWarning($"[CursorManager] ยังไม่ได้ใส่รูปเมาส์ช่อง '{label}' จะใช้รูปลูกศรปกติแทนไปก่อน", this);
            return;
        }

        Texture2D texture = art.texture;

        // ตรวจด้วยเส้นเดียวกับตอนใช้งานจริง จะได้ไม่มีกรณี "เตือนว่าผ่าน แต่พอเล่นจริงกลับใช้ไม่ได้"
        // ตัวนี้เตือนรายละเอียดให้เองแล้ว ตรงนี้แค่บอกเพิ่มว่าเป็นรูปของช่องไหน
        if (!CanUseAsCursor(texture))
        {
            Debug.LogWarning($"[CursorManager] รูปที่ใช้ไม่ได้ข้างบนคือช่อง '{label}' ระหว่างนี้ช่องนี้จะใช้เมาส์ปกติของเครื่องแทน", this);
            return;
        }

        // รูปต้นฉบับเกิน 32x32 จะถูกวาดโดยเกมเองเสมอ (ForceSoftware) ซึ่งเมาส์จะตามช้ากว่าของจริงนิดหน่อย
        // เตือนไว้ให้รู้ แต่ไม่ได้ทำให้ใช้ไม่ได้ เพราะ ApplyState สลับโหมดให้เองแล้ว
        if (texture.width > MaxHardwareCursorSize || texture.height > MaxHardwareCursorSize)
        {
            Debug.LogWarning($"[CursorManager] รูป '{texture.name}' (ช่อง {label}) ใหญ่เกิน 32x32 จะใช้โหมดให้เกมวาดเอง (ForceSoftware) เมาส์อาจตามช้ากว่าปกตินิดหน่อย", this);
        }
    }

    // เงื่อนไขเดียวกับที่ Unity เช็คข้างใน Cursor.SetCursor ถ้าไม่ผ่านมันจะโยน
    // "Invalid texture used for cursor" ออกมาทุกครั้งที่สั่ง จึงต้องกรองไว้ก่อนถึงจะไม่ถล่ม Console
    // (alphaIsTransparency เป็นค่าของตัวนำเข้าไฟล์ อ่านตอนเล่นไม่ได้ จึงเช็คไม่ได้ตรงนี้
    //  แต่ตั้ง Texture Type = Cursor ทีเดียวก็ได้ครบทุกข้ออยู่แล้ว)
    private bool CanUseAsCursor(Texture2D texture)
    {
        if (texture == null) return false;

        if (cursorTextureUsable.TryGetValue(texture, out bool cached)) return cached;

        string problem = null;

        if (texture.format != TextureFormat.RGBA32)
        {
            problem = $"รูปแบบเป็น {texture.format} ต้องเป็น RGBA32";
        }
        else if (!texture.isReadable)
        {
            problem = "รูปไม่ได้เปิด Read/Write";
        }
        else if (texture.mipmapCount > 1)
        {
            problem = "รูปมี mipmap ติดมาด้วย ต้องปิด Generate Mip Maps";
        }

        bool usable = problem == null;
        cursorTextureUsable[texture] = usable;

        if (!usable)
        {
            Debug.LogError($"[CursorManager] รูป '{texture.name}' ใช้เป็นเมาส์ไม่ได้ ({problem})\nแก้โดยเลือกไฟล์รูปใน Project แล้วตั้ง Texture Type = Cursor แล้วกด Apply", this);
        }

        return usable;
    }
}