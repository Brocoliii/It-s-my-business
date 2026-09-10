using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// แผงเลือกช่องเซฟ ใช้ได้สองโหมดด้วยแผงใบเดียวกัน
//
//   โหมดโหลด (Load)  = กดปุ่ม LOAD ในเมนูหลัก เลือกช่องแล้วเล่นต่อจากตรงนั้น
//   โหมดเซฟ (Save)   = กดปุ่ม SAVE บนแผงสรุปผลตอนผ่านด่าน เลือกช่องผู้เล่นที่จะบันทึกลง
//
//     LOAD GAME
//     Choose a slot to continue.
//     [ AUTOSAVE   ·  2026-09-05  14:32                   ]
//     [   Day 1 cleared · Orders 12 · Clues 4/4 ...       ]
//     [ SLOT 1                                            ]
//     [   Empty                                           ]
//     ...
//     [ BACK ]
//
// วิธีใช้: ไม่ต้องทำอะไรเลย ฝั่งที่เรียกใช้ (MainMenuController / StageResultPanel)
// สร้างแผงนี้ให้เองตอนถูกกดปุ่ม ถ้าอยากจัดหน้าตาเองก็ลากสคริปต์นี้ใส่ GameObject เปล่าในฉาก
// แล้วผูกช่องต่าง ๆ ใน Inspector แทนได้ ช่องไหนผูกไว้แล้วจะไม่ถูกสร้างทับ
//
// อยากได้ช่องเซฟหน้าตาของตัวเอง: ทำ Prefab แถวช่องเซฟแล้วใส่ในช่อง Slot Row Prefab
// (ต้องวางสคริปต์นี้ในฉากก่อนถึงจะมีที่ให้ใส่ Prefab — วิธีทำอ่านที่หัวไฟล์ SaveSlotRowView)
//
// ข้อความบนแผงเป็นภาษาอังกฤษทั้งหมด เพราะฟอนต์ TextMeshPro ที่โปรเจกต์นี้ใช้ไม่มีตัวอักษรไทย
[DisallowMultipleComponent]
public class SaveSlotPanel : MonoBehaviour
{
    public static SaveSlotPanel Instance { get; private set; }

    public enum PanelMode { Load, Save }

    [Header("สร้าง UI อัตโนมัติ")]
    [Tooltip("ติ๊กถูก = ช่องไหนไม่ได้ผูกไว้ จะสร้างให้เองตอนเริ่มฉาก")]
    public bool buildUiIfMissing = true;
    [Tooltip("ฟอนต์ที่ใช้ตอนสร้างข้อความให้เอง ถ้าไม่ใส่จะใช้ฟอนต์ปริยายของ TextMeshPro")]
    public TMP_FontAsset panelFont;
    [Tooltip("ลำดับการวางทับของ Canvas ที่สร้างให้เอง ต้องมากกว่าของแผงสรุปผล (300) ไม่งั้นแผงนี้จะโดนบัง")]
    public int canvasSortingOrder = 350;

    [Header("ชิ้นส่วนของแผง")]
    [Tooltip("แผงทั้งใบ (ตัวที่จะถูกเปิด/ปิด) ปล่อยว่าง = สร้างให้เอง")]
    public GameObject panelRoot;
    public TextMeshProUGUI titleText;
    [Tooltip("บรรทัดเล็กใต้หัวข้อ ใช้บอกว่าต้องทำอะไร และบอกผลตอนเซฟเสร็จ")]
    public TextMeshProUGUI statusText;
    [Tooltip("ตัวแม่ที่แถวช่องเซฟจะไปเรียงต่อกันข้างใน ปล่อยว่าง = สร้างให้เอง")]
    public RectTransform slotContainer;
    public Button backButton;

    [Header("ทำแถวช่องเซฟเอง")]
    [Tooltip("Prefab ของแถวช่องเซฟหนึ่งแถว ต้องมีสคริปต์ SaveSlotRowView อยู่ที่ตัวนอกสุด\nใส่ไว้ = ใช้หน้าตาของ Prefab นี้แทนแถวเรียบ ๆ ที่โค้ดสร้างให้\nปล่อยว่าง = ใช้แถวที่โค้ดสร้างให้เหมือนเดิม\n(วิธีทำ Prefab อ่านได้ที่หัวไฟล์ SaveSlotRowView)")]
    public SaveSlotRowView slotRowPrefab;

    [Header("ข้อความ")]
    public string loadTitle = "LOAD GAME";
    public string saveTitle = "SAVE GAME";
    public string loadHint = "Choose a slot to continue.";
    public string saveHint = "Choose a slot to save your progress.";
    public string backLabel = "BACK";

    [Header("การแสดงผล")]
    [Tooltip("เวลาที่แผงใช้เด้งขึ้นมา")]
    public float popDuration = 0.28f;
    [Tooltip("กดทับช่องที่มีเซฟอยู่แล้วต้องกดยืนยันซ้ำ ตัวเลขนี้คือเวลาที่รอการกดครั้งที่สอง (วินาที)")]
    public float overwriteConfirmTimeout = 4f;

    // สีเริ่มต้นของแถวที่โค้ดสร้างให้เอง
    // แถวที่มาจาก Prefab ไม่เกี่ยวกับสองค่านี้ ใช้สีที่ตั้งไว้บนตัว Prefab เอง (SaveSlotRowView)
    private static readonly Color RowNormalColor = new Color(0.16f, 0.18f, 0.23f, 1f);
    private static readonly Color DetailTextColor = new Color(1f, 1f, 1f, 0.62f);

    private readonly List<SaveSlotRowView> rows = new List<SaveSlotRowView>();

    private PanelMode mode = PanelMode.Load;

    // ข้อมูลที่จะเขียนลงช่องตอนอยู่โหมดเซฟ ฝั่งที่เปิดแผงเป็นคนเตรียมมาให้
    private SaveData pendingSaveData;

    private RectTransform builtCanvasRoot;
    private CanvasGroup panelCanvasGroup;
    private Vector3 panelBaseScale = Vector3.one;
    private bool hasPanelBase = false;
    private Coroutine popRoutine;
    private bool isOpen = false;

    // แถวที่กำลังรอการกดยืนยันเขียนทับ -1 = ไม่มี
    private int confirmRowIndex = -1;
    private float confirmTimer = 0f;

    // ---------- ทางเข้าที่ฝั่งอื่นเรียกใช้ ----------

    // หาแผงในฉาก (รวมตัวที่ปิดอยู่) ไม่มีก็สร้างให้ ฝั่งที่เรียกจะได้ไม่ต้องผูกอะไรใน Inspector เลย
    public static SaveSlotPanel GetOrCreate()
    {
        if (Instance != null) return Instance;

        SaveSlotPanel found = FindObjectOfType<SaveSlotPanel>(true);

        if (found != null)
        {
            // เผื่อคนวางสคริปต์ไว้บน GameObject ที่ปิดอยู่ ต้องเปิดก่อนไม่งั้น coroutine ของแผงจะไม่เดิน
            if (!found.gameObject.activeSelf) found.gameObject.SetActive(true);
            return found;
        }

        GameObject host = new GameObject("SaveSlotPanel");
        return host.AddComponent<SaveSlotPanel>();
    }

    // เปิดแผงในโหมดโหลด (เมนูหลัก)
    public static bool OpenLoadPanel()
    {
        SaveSlotPanel panel = GetOrCreate();
        if (panel == null) return false;

        return panel.Open(PanelMode.Load, null);
    }

    // เปิดแผงในโหมดเซฟ (แผงสรุปผลตอนผ่านด่าน)
    // data = จุดที่จะเล่นต่อ เตรียมมาจาก SaveManager.CaptureStageCleared
    public static bool OpenSavePanel(SaveData data)
    {
        if (data == null) return false;

        SaveSlotPanel panel = GetOrCreate();
        if (panel == null) return false;

        return panel.Open(PanelMode.Save, data);
    }

    // ---------- วงจรชีวิต ----------

    private void Awake()
    {
        // ฉากหนึ่งควรมีแผงเลือกช่องเซฟใบเดียว ตัวที่เกินมาทิ้งไปตั้งแต่ยังไม่ทันสร้าง UI ของตัวเอง
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (buildUiIfMissing && panelRoot == null) BuildDefaultPanel();

        // เผื่อคนจัดหน้าตาเองแล้วผูก Panel Root กับ Slot Container ไว้ใน Inspector
        // แถวช่องเซฟยังต้องสร้างจากโค้ดอยู่ดี เพราะจำนวนช่องมาจาก SaveManager
        if (rows.Count == 0 && slotContainer != null) BuildSlotRows();

        // ผูกปุ่มจากโค้ดแทนการผูกใน Inspector จะได้ไม่มีเคสลืมผูกแล้วปุ่มกดไม่ติด
        // ผูกตั้งแต่ Awake เพราะแผงเริ่มมาแบบปิดอยู่ ปุ่มข้างในเลยยังไม่ทำงานถ้าไปรอผูกทีหลัง
        if (backButton != null) backButton.onClick.AddListener(Close);

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (backButton != null) backButton.onClick.RemoveListener(Close);

        // Canvas ที่สร้างเองอาจถูกวางไว้ที่ระดับฉาก (ไม่ได้เป็นลูกของเรา) ต้องเก็บทิ้งเองไม่ให้ค้างในฉาก
        if (builtCanvasRoot != null && builtCanvasRoot.parent == null) Destroy(builtCanvasRoot.gameObject);

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (!isOpen) return;

        if (WasClosePressed())
        {
            Close();
            return;
        }

        if (confirmRowIndex < 0) return;

        // unscaledDeltaTime เพราะแผงสรุปผลหยุดเวลาเกมไว้ตอนเปิดอยู่
        confirmTimer -= Time.unscaledDeltaTime;

        if (confirmTimer <= 0f)
        {
            // รอนานแล้วไม่กดยืนยัน ให้ลืมไปเลย ผู้เล่นจะได้ไม่เผลอกดทับของเก่าทีหลัง
            CancelOverwriteConfirm();
            RefreshRows();
        }
    }

    // กด Esc เพื่อปิดแผง
    //
    // โปรเจกต์นี้ใช้ Input System ตัวใหม่ (ดู InputManager) การเรียก Input.GetKeyDown ตรง ๆ
    // จะพังตอนรันถ้า Active Input Handling ตั้งเป็น "Input System Package (New)" อย่างเดียว
    // เลยต้องแยกทางตามตัวที่โปรเจกต์เปิดใช้อยู่จริง
    private static bool WasClosePressed()
    {
#if ENABLE_INPUT_SYSTEM
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    public bool Open(PanelMode panelMode, SaveData data)
    {
        if (panelRoot == null)
        {
            Debug.LogWarning("[SaveSlotPanel] ไม่มีแผงให้โชว์ (ปิด Build Ui If Missing ไว้ และไม่ได้ผูก Panel Root)");
            return false;
        }

        mode = panelMode;
        pendingSaveData = data;
        isOpen = true;
        CancelOverwriteConfirm();

        if (titleText != null) titleText.text = mode == PanelMode.Load ? loadTitle : saveTitle;
        SetStatus(mode == PanelMode.Load ? loadHint : saveHint);

        RefreshRows();

        panelRoot.SetActive(true);

        if (popRoutine != null) StopCoroutine(popRoutine);
        popRoutine = StartCoroutine(PopRoutine());

        return true;
    }

    public void Close()
    {
        isOpen = false;
        CancelOverwriteConfirm();
        pendingSaveData = null;

        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
            popRoutine = null;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    // ---------- แถวช่องเซฟ ----------

    private void RefreshRows()
    {
        for (int i = 0; i < rows.Count; i++)
        {
            RefreshRow(i);
        }
    }

    private void RefreshRow(int rowIndex)
    {
        SaveSlotRowView row = rows[rowIndex];
        if (row == null) return;

        SaveData data = SaveManager.GetSlot(row.isAuto, row.slotIndex);

        // ช่องอัตโนมัติเป็นของเกม ผู้เล่นเขียนทับเองไม่ได้ เลยกดได้เฉพาะตอนโหลด
        // ช่องผู้เล่นตอนโหลดก็กดได้เฉพาะช่องที่มีเซฟอยู่ ช่องว่างไม่มีอะไรให้โหลด
        bool canClick = mode == PanelMode.Load ? data != null : !row.isAuto;
        bool isConfirming = confirmRowIndex == rowIndex;

        // มีช่องอัตโนมัติช่องเดียวก็ไม่ต้องมีเลขต่อท้าย ("AUTO 1" อ่านแล้วเหมือนมีช่องที่ 2 อยู่)
        string slotName = row.isAuto
            ? (SaveManager.AutoSlotCount > 1 ? "AUTO " + (row.slotIndex + 1) : "AUTOSAVE")
            : "SLOT " + (row.slotIndex + 1);

        string time = data != null ? data.DescribeTime() : "";
        string label = string.IsNullOrEmpty(time) ? slotName : slotName + "     " + time;

        row.Apply(label, BuildDetailText(row, data, isConfirming), canClick, isConfirming, data != null);
    }

    private string BuildDetailText(SaveSlotRowView row, SaveData data, bool isConfirming)
    {
        if (isConfirming) return "Click again to overwrite this slot";

        if (data != null) return data.DescribeProgress();

        // ช่องว่าง: บอกให้ตรงกับสิ่งที่กดได้จริงในโหมดนั้น
        if (mode == PanelMode.Save && !row.isAuto) return "Empty  ·  click to save here";

        return row.isAuto ? "Empty  ·  filled by the game when you clear a day" : "Empty";
    }

    private void OnRowClicked(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= rows.Count) return;

        SaveSlotRowView row = rows[rowIndex];
        if (row == null) return;

        if (mode == PanelMode.Load)
        {
            LoadFromRow(row);
            return;
        }

        SaveToRow(rowIndex, row);
    }

    private void LoadFromRow(SaveSlotRowView row)
    {
        if (SaveManager.GetSlot(row.isAuto, row.slotIndex) == null) return;

        if (!SaveManager.LoadSlot(row.isAuto, row.slotIndex))
        {
            // ส่วนใหญ่คือฉากปลายทางถูกเปลี่ยนชื่อ/ยังไม่ได้ใส่ใน Build Settings
            // รายละเอียดจริงอยู่ใน Console แล้ว ตรงนี้แค่บอกผู้เล่นว่ากดไปแล้วไม่ได้อะไร
            SetStatus("Cannot load this slot. Check the Console for details.");
            return;
        }

        // โหลดสำเร็จ = กำลังจะเปลี่ยนฉาก ปิดแผงไว้ก่อนเผื่อฉากปลายทางคือฉากเดิม
        Close();
    }

    private void SaveToRow(int rowIndex, SaveSlotRowView row)
    {
        // ช่องอัตโนมัติเป็นของเกม ปุ่มถูกปิดไว้อยู่แล้ว กันไว้อีกชั้นเผื่อมีใครเรียกตรง ๆ
        if (row.isAuto) return;

        if (pendingSaveData == null)
        {
            SetStatus("Nothing to save right now.");
            return;
        }

        bool isOccupied = SaveManager.GetSlot(false, row.slotIndex) != null;

        // กดทับของเก่าต้องยืนยันสองครั้ง เซฟที่หายไปแล้วเอากลับมาไม่ได้
        if (isOccupied && confirmRowIndex != rowIndex)
        {
            confirmRowIndex = rowIndex;
            confirmTimer = Mathf.Max(0.5f, overwriteConfirmTimeout);
            SetStatus("Slot " + (row.slotIndex + 1) + " already has a save. Click it again to overwrite.");
            RefreshRows();
            return;
        }

        CancelOverwriteConfirm();

        if (!SaveManager.WriteSlot(false, row.slotIndex, pendingSaveData))
        {
            SetStatus("Could not write the save file. Check the Console for details.");
            RefreshRows();
            return;
        }

        SetStatus("Saved to SLOT " + (row.slotIndex + 1) + ".");
        RefreshRows();
    }

    private void CancelOverwriteConfirm()
    {
        confirmRowIndex = -1;
        confirmTimer = 0f;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }

    // ---------- แอนิเมชันตอนเปิด ----------

    private IEnumerator PopRoutine()
    {
        Transform root = panelRoot.transform;

        if (!hasPanelBase)
        {
            // จำสเกลจริงไว้ครั้งเดียวก่อนจะไปเขียนทับเป็น 0 ไม่งั้นรอบถัดไปจะได้ค่า 0 มาเป็นค่าฐาน
            hasPanelBase = true;
            panelBaseScale = root.localScale == Vector3.zero ? Vector3.one : root.localScale;

            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        float duration = Mathf.Max(0.01f, popDuration);
        float time = 0f;

        root.localScale = Vector3.zero;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

        while (time < duration)
        {
            // unscaledDeltaTime เพราะแผงนี้ถูกเปิดตอนที่เวลาเกมมีสิทธิ์ถูกหยุดไว้อยู่แล้ว
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            root.localScale = panelBaseScale * EaseOutBack(t);
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Clamp01(t * 3f);

            yield return null;
        }

        root.localScale = panelBaseScale;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;
        popRoutine = null;
    }

    private static float EaseOutBack(float t)
    {
        float overshoot = 1.70158f;
        float shifted = t - 1f;
        return 1f + (overshoot + 1f) * Mathf.Pow(shifted, 3f) + overshoot * Mathf.Pow(shifted, 2f);
    }

    // ---------- สร้าง UI ให้เอง ----------

    private void BuildDefaultPanel()
    {
        RectTransform canvasRoot = EnsureCanvasRoot();
        if (canvasRoot == null) return;

        GameObject rootObject = new GameObject("SaveSlotPanel Root", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.SetParent(canvasRoot, false);
        Stretch(rootRect);
        panelRoot = rootObject;

        Image dimmer = CreateImage(rootRect, "Dimmer", new Color(0f, 0f, 0f, 0.78f), null);
        Stretch(dimmer.rectTransform);
        // กินคลิกไว้ ไม่ให้ผู้เล่นกดของที่อยู่ใต้แผงทะลุขึ้นมาได้
        dimmer.raycastTarget = true;

        RectTransform card = CreateCard(rootRect);

        if (titleText == null) titleText = CreateText(card, "Title", loadTitle, Color.white, 50f, TextAlignmentOptions.Center, FontStyles.Bold);
        if (statusText == null) statusText = CreateText(card, "Status", loadHint, new Color(1f, 1f, 1f, 0.7f), 24f, TextAlignmentOptions.Center, FontStyles.Normal);

        CreateSpacer(card, 8f);

        if (slotContainer == null) slotContainer = CreateSlotContainer(card);

        BuildSlotRows();

        CreateSpacer(card, 10f);

        if (backButton == null) backButton = CreateButton(card, "BackButton", backLabel, new Color(0.28f, 0.32f, 0.42f, 1f), 64f);

        // ใส่เสียงกด/เลื่อนผ่านให้ปุ่มทั้งแผงในทีเดียว (ตัวมันไล่ผูกให้เองตอน Start)
        if (rootObject.GetComponent<UIButtonSound>() == null) rootObject.AddComponent<UIButtonSound>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogWarning("[SaveSlotPanel] ฉากนี้ไม่มี EventSystem ปุ่มบนแผงเลือกช่องเซฟจะกดไม่ได้ (เพิ่มได้จาก GameObject > UI > Event System)");
        }
    }

    private void BuildSlotRows()
    {
        if (slotContainer == null) return;

        rows.Clear();

        for (int i = 0; i < SaveManager.AutoSlotCount; i++) rows.Add(CreateSlotRow(true, i));
        for (int i = 0; i < SaveManager.ManualSlotCount; i++) rows.Add(CreateSlotRow(false, i));
    }

    private SaveSlotRowView CreateSlotRow(bool isAuto, int index)
    {
        // มี Prefab ก็ใช้ของคนทำเกม ไม่มีค่อยสร้างแถวเรียบ ๆ ให้
        // (false = ให้ตำแหน่ง/สเกลอิงตัวแม่ ไม่ใช่พิกัดโลก ไม่งั้นแถวจะขนาดเพี้ยนตอน Canvas ถูกย่อ/ขยาย)
        SaveSlotRowView row = slotRowPrefab != null
            ? Instantiate(slotRowPrefab, slotContainer, false)
            : BuildDefaultSlotRow();

        row.gameObject.name = (isAuto ? "AutoSlot" : "ManualSlot") + index;
        row.isAuto = isAuto;
        row.slotIndex = index;
        row.ResolveMissingReferences();

        // แถวจาก Prefab ที่ไม่มี LayoutElement จะโดน Vertical Layout Group ของตัวแม่บีบจนเตี้ยผิดรูป
        // ใส่ให้เองโดยยึดความสูงที่ตั้งไว้ใน Prefab คนทำ Prefab จะได้ไม่ต้องมารู้เรื่องนี้
        if (slotRowPrefab != null && row.GetComponent<LayoutElement>() == null)
        {
            RectTransform rowRect = row.transform as RectTransform;

            if (rowRect != null && rowRect.rect.height > 0f)
            {
                LayoutElement fitted = row.gameObject.AddComponent<LayoutElement>();
                fitted.preferredHeight = rowRect.rect.height;
            }
        }

        if (row.button != null)
        {
            // ปิดสีตอนกดไม่ได้ของ Unity ทิ้ง (มันไปคูณอัลฟ่าจนแถวจาง) เราคุมสีแถวเองใน Apply แล้ว
            // Prefab ที่ตั้ง Tint Background = false ไว้ก็ยังไม่โดนแตะ เพราะเราไม่ได้ไปเขียนสีเอง
            if (row.tintBackground)
            {
                ColorBlock colors = row.button.colors;
                colors.disabledColor = Color.white;
                row.button.colors = colors;
            }

            // เก็บลำดับแถวไว้ในตัวแปรของตัวเอง ไม่ใช้ rows.Count ตอนกด
            // เพราะ listener จะจำค่าตอนที่ผูก ไม่ใช่ค่าตอนที่กด
            int rowIndex = rows.Count;
            row.button.onClick.AddListener(delegate { OnRowClicked(rowIndex); });
        }
        else
        {
            Debug.LogWarning("[SaveSlotPanel] แถวช่องเซฟ '" + row.gameObject.name + "' ไม่มีปุ่ม (Button) เลย กดเลือกช่องนี้ไม่ได้", row);
        }

        return row;
    }

    // แถวมาตรฐานที่โค้ดสร้างให้ ใช้ตอนไม่ได้ใส่ Slot Row Prefab ไว้
    private SaveSlotRowView BuildDefaultSlotRow()
    {
        Image background = CreateImage(slotContainer, "SlotRow", RowNormalColor, ResolveBoxSprite());

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        LayoutElement element = background.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 84f;

        VerticalLayoutGroup layout = background.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 12, 12);
        layout.spacing = 2f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TextMeshProUGUI labelText = CreateText(background.rectTransform, "Label", "", Color.white, 26f, TextAlignmentOptions.Left, FontStyles.Bold);
        TextMeshProUGUI detailText = CreateText(background.rectTransform, "Detail", "", DetailTextColor, 19f, TextAlignmentOptions.Left, FontStyles.Normal);

        // ข้อความบนแถวต้องไม่กินคลิก ไม่งั้นบางจังหวะคลิกจะไปโดนตัวอักษรแทนตัวปุ่ม
        labelText.raycastTarget = false;
        detailText.raycastTarget = false;

        SaveSlotRowView row = background.gameObject.AddComponent<SaveSlotRowView>();
        row.button = button;
        row.background = background;
        row.labelText = labelText;
        row.detailText = detailText;

        return row;
    }

    private RectTransform EnsureCanvasRoot()
    {
        if (builtCanvasRoot != null) return builtCanvasRoot;

        // สร้าง Canvas ของตัวเองเสมอ ไม่ไปอาศัย Canvas ที่มีอยู่ในฉาก
        // เพราะแผงนี้ต้องอยู่บนสุด (เหนือแผงสรุปผลและเมนูหลัก) ซึ่งคุมด้วย Sorting Order ตรงนี้ที่เดียวจบ
        GameObject canvasObject = new GameObject("SaveSlotPanel Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = canvasSortingOrder;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // เก็บไว้เป็นลูกของตัวเองเพื่อให้ถูกทำลายไปพร้อมกัน แต่ห้ามทำแบบนั้นถ้าตัวเองอยู่ใต้ Canvas อื่นอยู่แล้ว
        // เพราะ Canvas ซ้อน Canvas จะไม่สนใจ Sorting Order ของตัวเอง (ไปตามตัวแม่แทน) แผงจะโดนบังได้
        if (GetComponentInParent<Canvas>() == null)
        {
            canvasObject.transform.SetParent(transform, false);
        }

        builtCanvasRoot = canvasObject.GetComponent<RectTransform>();
        return builtCanvasRoot;
    }

    private RectTransform CreateCard(RectTransform parent)
    {
        Image background = CreateImage(parent, "Card", new Color(0.09f, 0.1f, 0.13f, 0.97f), ResolveBoxSprite());
        RectTransform rect = background.rectTransform;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(900f, 0f);

        VerticalLayoutGroup layout = background.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(48, 48, 36, 36);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = background.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private RectTransform CreateSlotContainer(RectTransform parent)
    {
        GameObject containerObject = new GameObject("Slots", typeof(RectTransform));
        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        VerticalLayoutGroup layout = containerObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = containerObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }

    private Button CreateButton(RectTransform parent, string name, string label, Color color, float height)
    {
        Image background = CreateImage(parent, name, color, ResolveBoxSprite());

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        LayoutElement element = background.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = height;

        TextMeshProUGUI text = CreateText(background.rectTransform, "Label", label, Color.white, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform);
        text.raycastTarget = false;

        // ป้ายอยู่ใต้ปุ่มที่ไม่ใช่ Layout Group เลยต้องกางเต็มเอง ไม่ต้องมี LayoutElement
        LayoutElement labelElement = text.GetComponent<LayoutElement>();
        if (labelElement != null) Destroy(labelElement);

        return button;
    }

    private Image CreateImage(RectTransform parent, string name, Color color, Sprite sprite)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;

        if (sprite != null)
        {
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
        }

        return image;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string content, Color color, float fontSize, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (panelFont != null) text.font = panelFont;
        text.text = content;
        text.color = color;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.fontStyle = style;
        text.enableWordWrapping = false;
        // รายละเอียดของช่องเซฟยาวไม่เท่ากัน ปล่อยให้ย่อขนาดลงเองดีกว่าปล่อยให้ล้นออกนอกการ์ด
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.7f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize * 1.5f;

        return text;
    }

    private void CreateSpacer(RectTransform parent, float height)
    {
        GameObject spacerObject = new GameObject("Spacer", typeof(RectTransform));
        spacerObject.transform.SetParent(parent, false);

        LayoutElement layout = spacerObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    // สไปรต์กล่องมุมมนที่ติดมากับ Unity ใช้เป็นพื้นหลังแผง/ปุ่มได้เลย ไม่ต้องเตรียมอาร์ต
    // หาไม่เจอก็ไม่เป็นไร Image จะกลายเป็นสี่เหลี่ยมทึบแทน
    private Sprite ResolveBoxSprite()
    {
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    }
}
