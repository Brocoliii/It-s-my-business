using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[System.Serializable]
public class SuspectData
{
    public string suspectName;
    public Sprite suspectImage;
    public string shortNote;
    public string suspectDetail;
}

public class NotebookManager : MonoBehaviour
{
    [Header("��觢��: ��¡�������")]
    public Transform clueContentContainer;
    public GameObject clueTextPrefab;

    [Header("��觫���: ����ͧʧ���")]
    public Transform suspectGridContainer;
    public GameObject suspectCardPrefab;

    [Header("��ͧʧ���: ���͡���")]
    public GameObject clueTabRoot;
    public GameObject suspectListTabRoot;
    public GameObject suspectDetailTabRoot;
    public Image selectedSuspectImage;
    public TMP_Text selectedSuspectNameText;
    public TMP_Text selectedSuspectDetailText;
    [Tooltip("ตราปั๊มที่ต้องลากไปปั๊มเพื่อยืนยัน (ใช้แทนปุ่มยืนยันแบบเดิม)")]
    public StampConfirmUI confirmStamp;

    [Header("�ҹ�����ż���ͧʧ���")]
    public List<SuspectData> allSuspects;

    [Header("แอนิเมชัน: เปิด/ปิดสมุด")]
    [Tooltip("ตัวสมุดที่จะโดนย่อ/ขยาย/เอียง ปล่อยว่าง = ใช้ Transform ของตัวเอง")]
    public Transform notebookRoot;
    public float openDuration = 0.45f;
    public float closeDuration = 0.24f;
    [Tooltip("ขนาดตอนเริ่มเด้งขึ้นมา เทียบกับขนาดจริง (1 = ไม่ย่อเลย)")]
    [Range(0.1f, 1f)] public float openStartScale = 0.55f;
    [Tooltip("องศาที่สมุดเอียงตอนโผล่มา แล้วค่อยๆ คืนตรงเหมือนโดนวางลงบนโต๊ะ")]
    public float openTiltAngle = 7f;

    [Header("แอนิเมชัน: ฉากหลังหรี่จอตอนสมุดเปิด")]
    [Tooltip("ภาพเต็มจอที่อยู่หลังสมุด จะค่อยๆ จางเข้ามาตอนสมุดเปิด และจางออกตอนสมุดปิด ปล่อยว่าง = ไม่มีฉากหลัง")]
    public Image backdropImage;
    [Tooltip("ความจางสูงสุดของฉากหลังตอนสมุดเปิดเต็มที่ (1 = ทึบสุด)")]
    [Range(0f, 1f)] public float backdropMaxAlpha = 0.65f;

    [Header("แอนิเมชัน: หน้าประวัติผู้ต้องสงสัย")]
    [Tooltip("เวลาที่หน้าประวัติเลื่อนเข้ามาจากทางขวา ตอนกดเลือกผู้ต้องสงสัย")]
    public float detailSlideInDuration = 0.32f;
    [Tooltip("เวลาที่หน้าประวัติคนเก่าเลื่อนออกไปทางซ้าย ตอนกดเลือกผู้ต้องสงสัยคนใหม่")]
    public float detailSlideOutDuration = 0.2f;
    [Tooltip("ระยะที่หน้าประวัติเริ่มเลื่อนเข้ามา หน่วยเดียวกับ anchoredPosition ของ Canvas (ค่าบวก = โผล่มาจากทางขวา)")]
    public float detailSlideDistance = 260f;

    [Header("แอนิเมชัน: โชว์หน้าประวัติหลังปั๊มเสร็จ")]
    [Tooltip("ปั๊มเสร็จแล้ว ให้หน้าประวัติของคนที่ถูกปั๊มลอยไปอยู่กลางจอและขยายใหญ่ขึ้น ก่อนจะตัดสินแพ้/ชนะ")]
    public bool showcaseDetailAfterStamp = true;
    [Tooltip("ที่ที่หน้าประวัติจะย้ายไปอยู่ตอนโชว์ ต้องเป็นของนอกตัวสมุด ปล่อยว่าง = Canvas ชั้นบนสุด")]
    public RectTransform showcaseLayer;
    [Tooltip("เวลาที่หน้าประวัติใช้ลอยไปกลางจอ")]
    public float showcaseMoveDuration = 0.45f;
    [Tooltip("ขนาดตอนไปอยู่กลางจอ เทียบกับขนาดปกติของหน้าประวัติ (1 = เท่าเดิม)")]
    public float showcaseScale = 1.2f;
    [Tooltip("องศาที่หน้าประวัติค่อยๆ เอียงตอนลอยไปกลางจอ เหมือนวางแฟ้มเอียงไว้บนโต๊ะ (0 = ไม่เอียง, ค่าบวก = เอียงทวนเข็ม)")]
    public float showcaseTiltAngle = 0f;
    [Tooltip("เวลาที่ค้างให้ผู้เล่นดูหน้าประวัติกลางจอ ก่อนจอแพ้/ชนะจะเด้งขึ้นมา")]
    public float showcaseHoldDuration = 0.6f;
    [Tooltip("ขยับออกจากจุดกึ่งกลางจอ หน่วยเป็นพิกเซลบนจอ เผื่ออยากให้เยื้องขึ้น/ลง")]
    public Vector2 showcaseCenterOffset = Vector2.zero;

    [Header("แอนิเมชัน: เก็บหน้าเบาะแสตอนปั๊มเสร็จ")]
    [Tooltip("ปั๊มเสร็จแล้ว ให้หน้าเบาะแสไถลออกไปทางซ้ายแล้วจางหายก่อน แล้วสมุดค่อยหุบปิดตามไป")]
    public bool hideClueTabAfterStamp = true;
    [Tooltip("เวลาที่หน้าเบาะแสใช้ไถลออกไปจนหายลับ")]
    public float clueTabHideDuration = 0.22f;
    [Tooltip("ระยะที่หน้าเบาะแสไถลออกไป หน่วยเดียวกับ anchoredPosition ของ Canvas (ค่าบวก = ไถลออกไปทางซ้าย)")]
    public float clueTabHideDistance = 240f;
    [Tooltip("ขนาดตอนไถลออกไปสุด เทียบกับขนาดจริง (1 = ไม่ย่อเลย)")]
    [Range(0.1f, 1f)] public float clueTabHideScale = 0.92f;

    [Header("แอนิเมชัน: หน้าเบาะแสไถลเข้าตอนเปิดสมุด")]
    [Tooltip("เวลาที่หน้าเบาะแสใช้ไถลเข้ามาตอนสมุดเปิดเสร็จ ระยะและขนาดเริ่มต้นใช้ค่าเดียวกับตอนไถลออก (clueTabHideDistance/clueTabHideScale) แค่วิ่งกลับทาง")]
    public float clueTabShowDuration = 0.28f;

    [Header("แอนิเมชัน: รายการในสมุด")]
    [Tooltip("เวลาที่เบาะแส 1 บรรทัด หรือการ์ด 1 ใบ ใช้เด้งขึ้นมา")]
    public float entryPopDuration = 0.28f;
    [Tooltip("เวลาหน่างระหว่างชิ้นที่ i กับ i+1 ทำให้ทยอยโผล่ทีละอันแทนที่จะโผล่พร้อมกันหมด")]
    public float entryStagger = 0.05f;

    private SuspectData selectedSuspect;
    private readonly List<SuspectCardUI> spawnedCards = new List<SuspectCardUI>();

    // ของหนึ่งชิ้นที่ทยอยเด้งเข้ามาในสมุด (บรรทัดเบาะแส 1 บรรทัด หรือการ์ดผู้ต้องสงสัย 1 ใบ)
    private class NotebookEntry
    {
        public Transform target;
        public CanvasGroup group;
        public Vector3 baseScale;
    }

    private readonly List<NotebookEntry> clueEntries = new List<NotebookEntry>();
    private readonly List<NotebookEntry> cardEntries = new List<NotebookEntry>();

    private Coroutine openCloseRoutine;
    private Coroutine tabRoutine;
    private Coroutine clueEntryRoutine;
    private Coroutine cardEntryRoutine;
    private Coroutine clueTabShowRoutine;

    private CanvasGroup notebookCanvasGroup;
    private Vector3 notebookBaseScale = Vector3.one;
    private Quaternion notebookBaseRotation = Quaternion.identity;
    private bool hasCachedNotebookBase;
    private bool isClosing;
    private bool isOpen;

    private RectTransform detailTabRect;
    private CanvasGroup detailTabCanvasGroup;
    private Vector2 detailTabBasePos;
    private bool hasCachedDetailTabBase;

    private RectTransform clueTabRect;
    private CanvasGroup clueTabCanvasGroup;
    private Vector2 clueTabBasePos;
    private Vector3 clueTabBaseScale = Vector3.one;
    private bool hasCachedClueTabBase;

    private Transform detailTabHomeParent;
    private int detailTabHomeSiblingIndex;
    private Vector3 detailTabBaseScale = Vector3.one;
    private Quaternion detailTabBaseRotation = Quaternion.identity;
    private Canvas detailTabCanvas;

    public void OpenNotebook()
    {
        // ตอนจบวันสมุดโดนสั่งเปิดสองที (หน้าจอจบวันเรียกที GameManager เรียกอีกที ห่างกันเฟรมเดียว)
        // ถ้าไม่กันไว้ สมุดจะย่อกลับไปเด้งใหม่ทั้งใบ แล้วการ์ดที่กำลังทยอยโผล่ก็จะโดนตัดกลางคันไปเริ่มนับหนึ่งใหม่
        if (isOpen) return;
        isOpen = true;

        gameObject.SetActive(true);
        selectedSuspect = null;

        CacheNotebookBase();
        StopAllNotebookAnimations();
        RestoreNotebookPose();

        // เล่นแอนิเมชันเด้งเข้ามาแทนการสแนปโผล่ทันที ให้ตราปั๊มโผล่มาพร้อมจังหวะเดียวกับที่สมุดกำลังเปิด
        // ตราปั๊มถูกปิด GameObject ไว้ตอนพัก ต้องเปิดก่อนถึงจะสั่ง Coroutine ของมันได้
        if (confirmStamp != null)
        {
            confirmStamp.gameObject.SetActive(true);
            confirmStamp.PlayAppearAnimation();
        }

        PopulateClueList();
        PopulateSuspectCards();

        // หน้าเบาะแสไม่เปิดทันทีตรงนี้ ปล่อยให้ ShowClueTabAnimation ใน OpenRoutine เป็นคนเปิดพร้อมไถลเข้า
        // จะได้ไม่โผล่มาแบบไม่มีแอนิเมชันขณะที่ตัวสมุดกำลังเด้งเปิดอยู่
        ShowSuspectListTab(true);
        ShowSuspectDetailTab(false);

        openCloseRoutine = StartCoroutine(OpenRoutine());
    }

    // เคลียร์ธงตรงนี้ที่เดียว จะได้ครอบทุกทางที่สมุดถูกปิด ไม่ใช่แค่ทางที่ปั๊มเลือกคนร้ายเสร็จ
    private void OnDisable()
    {
        isOpen = false;
    }

    // หน้าจอจบวันเรียกตัวนี้ ตอนนี้เปิดเหมือน OpenNotebook ทุกอย่าง
    // เพราะการ์ดผู้ต้องสงสัยต้องโผล่มาพร้อมสมุดเสมอ ไม่ว่าจะเปิดสมุดมาจากทางไหน
    public void OpenClueWindow()
    {
        OpenNotebook();
    }

    // เด้งตัวสมุดขึ้นมาให้เสร็จก่อน แล้วค่อยทยอยเขียนของข้างในลงไป
    // ถ้าปล่อยให้เล่นพร้อมกันจะดูรกและอ่านไม่ทัน
    private IEnumerator OpenRoutine()
    {
        yield return OpenAnimation();

        clueTabShowRoutine = StartCoroutine(ShowClueTabAnimation());
        clueEntryRoutine = StartCoroutine(PlayEntryAnimation(clueEntries));
        cardEntryRoutine = StartCoroutine(PlayEntryAnimation(cardEntries));

        openCloseRoutine = null;
    }

    private void PopulateClueList()
    {
        clueEntries.Clear();

        if (clueContentContainer == null || clueTextPrefab == null)
        {
            return;
        }

        // for + GetChild แทน foreach (Transform child in ...) เพราะ foreach ของ Transform ทำให้เกิด boxing allocation ทุกครั้งที่ลูป
        for (int i = clueContentContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(clueContentContainer.GetChild(i).gameObject);
        }

        InvestigationManager invMgr = FindObjectOfType<InvestigationManager>();
        if (invMgr == null)
        {
            return;
        }

        for (int i = 0; i < invMgr.collectedClues.Count; i++)
        {
            GameObject newClue = Instantiate(clueTextPrefab, clueContentContainer);
            TMP_Text clueText = newClue.GetComponent<TMP_Text>();
            if (clueText != null)
            {
                clueText.text = $"- {invMgr.collectedClues[i]}";
            }

            clueEntries.Add(BuildEntry(newClue));
        }

        // ซ่อนทันทีตั้งแต่ตอนสร้าง ไม่งั้นจะเห็นทุกบรรทัดโผล่เต็มจอหนึ่งเฟรมก่อนจะยุบไปเริ่มแอนิเมชัน
        HideEntries(clueEntries);
    }

    private void PopulateSuspectCards()
    {
        cardEntries.Clear();

        if (suspectGridContainer == null || suspectCardPrefab == null)
        {
            return;
        }

        // เหตุผลเดียวกับ PopulateClueList: เลี่ยง boxing allocation ของ foreach บน Transform
        for (int i = suspectGridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(suspectGridContainer.GetChild(i).gameObject);
        }
        spawnedCards.Clear();

        foreach (SuspectData suspect in allSuspects)
        {
            GameObject newCard = Instantiate(suspectCardPrefab, suspectGridContainer);
            SuspectCardUI cardUI = newCard.GetComponent<SuspectCardUI>();
            if (cardUI != null)
            {
                cardUI.SetupCard(suspect, this);
                spawnedCards.Add(cardUI);
            }

            cardEntries.Add(BuildEntry(newCard));
        }

        HideEntries(cardEntries);
    }

    public void ShowSuspectDetails(string suspectName)
    {
        // สมุดกำลังหุบปิดอยู่ (ปั๊มไปแล้ว) ห้ามเปิดประวัติคนใหม่ซ้อนขึ้นมา
        if (isClosing) return;

        SuspectData suspect = FindSuspect(suspectName);
        if (suspect == null)
        {
            return;
        }

        bool isDetailOnScreen = suspectDetailTabRoot != null && suspectDetailTabRoot.activeSelf;

        // กดคนเดิมซ้ำไม่ต้องเล่นสไลด์ใหม่ ไม่งั้นหน้าจะไถลออกซ้ายแล้ววิ่งกลับเข้ามาเปล่าๆ
        // แถมตราปั๊มจะโดน ResetStamp ทิ้งทั้งที่ผู้เล่นยังไม่ได้เปลี่ยนใจ
        if (isDetailOnScreen && selectedSuspect == suspect) return;

        selectedSuspect = suspect;
        HighlightSuspectCard(suspect.suspectName);

        if (tabRoutine != null) StopCoroutine(tabRoutine);
        tabRoutine = StartCoroutine(ShowDetailRoutine(isDetailOnScreen));
    }

    // หน้ารายชื่อค้างอยู่ตลอด ผู้เล่นเลยกดสลับคนได้เลยโดยไม่ต้องมีปุ่มย้อนกลับ
    // ถ้ามีประวัติคนเก่าค้างอยู่ ให้ดันออกไปทางซ้ายก่อน แล้วคนใหม่ค่อยไถลเข้ามาจากทางขวา
    // เหมือนเลื่อนแฟ้มทีละใบ ผู้เล่นจะอ่านทิศทางได้ว่าของเก่าถูกแทนที่ ไม่ใช่แค่เนื้อหากระพริบเปลี่ยน
    // ต้องเปลี่ยนเนื้อหาตอนหน้าอยู่นอกจอเท่านั้น ไม่งั้นจะเห็นรูปสลับกลางคันตอนหน้ายังไถลอยู่
    private IEnumerator ShowDetailRoutine(bool replacePrevious)
    {
        if (replacePrevious)
        {
            yield return SlideDetailTabOut();
        }

        UpdateSelectedSuspectView();

        yield return SlideDetailTabIn();

        tabRoutine = null;
    }

    public void ConfirmSelectedSuspect()
    {
        if (selectedSuspect == null)
        {
            return;
        }

        SelectCulprit(selectedSuspect.suspectName);
    }

    public void SelectCulprit(string chosenName)
    {
        // กันสั่งซ้ำ: ตราปั๊มยิง event ได้อีกรอบระหว่างที่สมุดยังเล่นแอนิเมชันปิดค้างอยู่
        if (isClosing) return;
        isClosing = true;

        Debug.Log($"<color=orange>�����蹪����: {chosenName}!</color>");

        if (openCloseRoutine != null) StopCoroutine(openCloseRoutine);
        openCloseRoutine = StartCoroutine(CloseAndSubmitRoutine(chosenName));
    }

    // ให้สมุดหุบปิดจนจบก่อน ผู้เล่นจะได้เห็นผลของการปั๊มก่อนจอแพ้/ชนะจะเด้งขึ้นมาทับ
    // แล้วค่อยดันหน้าประวัติของคนที่โดนปั๊มไปไว้กลางจอ เป็นการสรุปว่าตกลงชี้ใครไป
    private IEnumerator CloseAndSubmitRoutine(string chosenName)
    {
        // ต้องดึงหน้าประวัติออกจากสมุด "ก่อน" สมุดจะเริ่มหุบ
        // ไม่งั้นมันจะโดนสเกลกับ CanvasGroup ของสมุดลากให้ย่อและจางหายไปด้วยกัน
        bool willShowcase = PrepareDetailShowcase();

        // เก็บหน้าเบาะแสก่อนเป็นอย่างแรก ผู้เล่นจะได้เห็นสมุดถูกเก็บทีละส่วน ไม่ใช่ทั้งใบยุบหายพร้อมกันทีเดียว
        yield return HideClueTabAnimation();

        yield return CloseAnimation();

        if (willShowcase)
        {
            yield return MoveDetailTabToCenter();

            if (showcaseHoldDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(showcaseHoldDuration);
            }
        }

        openCloseRoutine = null;

        GameManager.Instance.SubmitVerdict(chosenName);

        // SetActive(false) ต้องเป็นบรรทัดสุดท้ายเสมอ เพราะมันฆ่า coroutine ตัวนี้ทิ้งทันที
        // อะไรที่เขียนต่อจากนี้จะไม่ถูกรันเลย
        gameObject.SetActive(false);
    }

    private void HighlightSuspectCard(string suspectName)
    {
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] == null)
            {
                continue;
            }

            spawnedCards[i].SetSelected(spawnedCards[i].SuspectName == suspectName);
        }
    }

    private SuspectData FindSuspect(string suspectName)
    {
        for (int i = 0; i < allSuspects.Count; i++)
        {
            if (allSuspects[i] != null && allSuspects[i].suspectName == suspectName)
            {
                return allSuspects[i];
            }
        }

        return null;
    }

    private void UpdateSelectedSuspectView()
    {
        if (selectedSuspect == null)
        {
            return;
        }

        if (selectedSuspectImage != null)
        {
            selectedSuspectImage.sprite = selectedSuspect.suspectImage;
            selectedSuspectImage.enabled = selectedSuspect.suspectImage != null;
        }

        if (selectedSuspectNameText != null)
        {
            selectedSuspectNameText.text = selectedSuspect.suspectName;
        }

        if (selectedSuspectDetailText != null)
        {
            selectedSuspectDetailText.text = string.IsNullOrWhiteSpace(selectedSuspect.suspectDetail)
                ? selectedSuspect.shortNote
                : selectedSuspect.suspectDetail;
        }

        if (confirmStamp != null)
        {
            confirmStamp.onStampConfirmed.RemoveListener(ConfirmSelectedSuspect);
            confirmStamp.onStampConfirmed.AddListener(ConfirmSelectedSuspect);

            // ผู้ต้องสงสัยคนใหม่ = เอาตราปั๊มกลับไปวางที่เดิม รอปั๊มใหม่
            confirmStamp.ResetStamp();
        }
    }

    private void ShowClueTab(bool isVisible)
    {
        SetTabVisible(clueTabRoot, isVisible);
    }

    private void ShowSuspectListTab(bool isVisible)
    {
        SetTabVisible(suspectListTabRoot, isVisible);
    }

    private void ShowSuspectDetailTab(bool isVisible)
    {
        SetTabVisible(suspectDetailTabRoot, isVisible);
    }

    // เปิด/ปิดหน้าแบบทันที
    private void SetTabVisible(GameObject tabRoot, bool isVisible)
    {
        if (tabRoot == null)
        {
            return;
        }

        // หน้าประวัติมีทั้งตำแหน่งและความจางที่แอนิเมชันเลื่อนหน้าเขียนทับไว้ ต้องคืนให้ครบก่อนเสมอ
        // ไม่งั้นถ้าแอนิเมชันถูกหยุดกลางคัน หน้าจะค้างอยู่นอกจอแบบจางๆ แล้วกดอะไรไม่ได้เลย
        if (tabRoot == suspectDetailTabRoot)
        {
            RestoreDetailTabPose();
        }

        // หน้าเบาะแสก็โดนแอนิเมชันตอนปั๊มเสร็จเขียนทับตำแหน่ง/สเกล/ความจางเหมือนกัน ต้องคืนให้ครบด้วยเหตุผลเดียวกัน
        if (tabRoot == clueTabRoot)
        {
            RestoreClueTabPose();
        }

        tabRoot.SetActive(isVisible);
    }

    // ===== แอนิเมชันสมุด =====

    // อ่านสเกล/มุมจริงของสมุดเก็บไว้ครั้งเดียวตอนเปิดครั้งแรก
    // ทำใน Awake ไม่ได้ เพราะสมุดมักเริ่มต้นด้วยการถูกปิดอยู่ Awake เลยยังไม่ถูกเรียก
    private void CacheNotebookBase()
    {
        isClosing = false;

        Transform root = ResolveNotebookRoot();

        if (notebookCanvasGroup == null)
        {
            notebookCanvasGroup = EnsureCanvasGroup(root.gameObject);
        }

        if (hasCachedNotebookBase)
        {
            return;
        }

        hasCachedNotebookBase = true;
        notebookBaseScale = root.localScale;
        notebookBaseRotation = root.localRotation;
    }

    private Transform ResolveNotebookRoot()
    {
        return notebookRoot != null ? notebookRoot : transform;
    }

    private void StopAllNotebookAnimations()
    {
        if (openCloseRoutine != null) StopCoroutine(openCloseRoutine);
        if (tabRoutine != null) StopCoroutine(tabRoutine);
        if (clueEntryRoutine != null) StopCoroutine(clueEntryRoutine);
        if (cardEntryRoutine != null) StopCoroutine(cardEntryRoutine);
        if (clueTabShowRoutine != null) StopCoroutine(clueTabShowRoutine);

        openCloseRoutine = null;
        tabRoutine = null;
        clueEntryRoutine = null;
        cardEntryRoutine = null;
        clueTabShowRoutine = null;
    }

    // สมุดเด้งขึ้นมาจากขนาดเล็กแบบ ease-out back พร้อมเอียงอยู่นิดหนึ่งแล้วค่อยคืนตรง
    // เหมือนมีคนโยนสมุดลงมาวางบนโต๊ะแล้วมันกระดอนเข้าที่
    private IEnumerator OpenAnimation()
    {
        Transform root = ResolveNotebookRoot();
        float duration = Mathf.Max(0.01f, openDuration);

        // ระหว่างสมุดยังเด้งไม่เข้าที่ ห้ามให้กดการ์ดหรือลากตราปั๊มได้
        SetNotebookInteractable(false);

        // เปิดบล็อกคลิกของฉากหลังตั้งแต่เริ่มเปิด ไม่ต้องรอให้จางเข้าเสร็จก่อน กันผู้เล่นคลิกทะลุไปโดนของหลังสมุดระหว่างที่ฉากหลังยังจางอยู่
        if (backdropImage != null) backdropImage.raycastTarget = true;

        root.localScale = notebookBaseScale * openStartScale;
        root.localRotation = notebookBaseRotation * Quaternion.Euler(0f, 0f, openTiltAngle);

        float time = 0f;
        while (time < duration)
        {
            // unscaledDeltaTime เผื่อวันไหนหน้าเลือกคนร้ายไปหยุดเวลาเกม สมุดจะได้ยังเล่นแอนิเมชันได้อยู่
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            root.localScale = notebookBaseScale * Mathf.Lerp(openStartScale, 1f, EaseOutBack(t));

            float settle = t * t * (3f - 2f * t);
            root.localRotation = notebookBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(openTiltAngle, 0f, settle));

            // ให้จางเข้ามาไวกว่าการเด้ง ไม่งั้นจะรู้สึกว่าสมุดมาช้าเกิน
            float fadeIn = Mathf.Clamp01(t / 0.35f);
            if (notebookCanvasGroup != null) notebookCanvasGroup.alpha = fadeIn;
            SetBackdropAlpha(fadeIn * backdropMaxAlpha);

            yield return null;
        }

        root.localScale = notebookBaseScale;
        root.localRotation = notebookBaseRotation;
        if (notebookCanvasGroup != null) notebookCanvasGroup.alpha = 1f;
        SetBackdropAlpha(backdropMaxAlpha);

        SetNotebookInteractable(true);
    }

    // ปิดสมุด: ยืดโตขึ้นแวบหนึ่งก่อน (anticipation) แล้วค่อยหุบลงเร็วๆ พร้อมเอียงกลับทางเดิม
    private IEnumerator CloseAnimation()
    {
        SetNotebookInteractable(false);

        // ปิดบล็อกคลิกของฉากหลังทันที ไม่ต้องรอให้จางออกเสร็จ เผื่อจอแพ้/ชนะที่กำลังจะขึ้นมาต้องใช้พื้นที่ตรงนี้
        if (backdropImage != null) backdropImage.raycastTarget = false;

        Transform root = ResolveNotebookRoot();
        float duration = Mathf.Max(0.01f, closeDuration);

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            // t*t = ease-in ทำให้ช่วงแรกแทบไม่ขยับแล้วค่อยหุบพรวดตอนท้าย
            float shrink = Mathf.Lerp(1f, 0.7f, t * t);
            float anticipation = Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI) * 0.08f;

            root.localScale = notebookBaseScale * (shrink + anticipation);
            root.localRotation = notebookBaseRotation * Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -openTiltAngle, t * t));

            float fadeOut = 1f - t * t;
            if (notebookCanvasGroup != null) notebookCanvasGroup.alpha = fadeOut;
            SetBackdropAlpha(fadeOut * backdropMaxAlpha);

            yield return null;
        }

        // ห้ามคืนสเกล/ความจางกลับเป็นเต็มตรงนี้
        // เพราะ GameObject ของสมุดยังไม่ถูกปิด จนกว่าหน้าประวัติจะลอยไปโชว์กลางจอเสร็จ (ดู CloseAndSubmitRoutine)
        // ถ้าคืนค่าตรงนี้ สมุดจะเด้งกลับมาโผล่ทั้งใบซ้อนอยู่ข้างหลังหน้าประวัติที่กำลังโชว์
        // ปล่อยให้ค้างเป็นสมุดจิ๋วจางๆ ไว้แบบนี้ แล้วไปคืนค่าตอนเปิดรอบหน้าด้วย RestoreNotebookPose()
        root.localScale = notebookBaseScale * 0.7f;
        root.localRotation = notebookBaseRotation * Quaternion.Euler(0f, 0f, -openTiltAngle);
        if (notebookCanvasGroup != null) notebookCanvasGroup.alpha = 0f;
        SetBackdropAlpha(0f);
    }

    // คืนสเกล/มุม/ความจางของสมุดให้เต็มก่อนเริ่มเปิดรอบใหม่
    // ต้องมีเพราะตอนปิดเราจงใจปล่อยให้สมุดค้างเป็นใบจิ๋วจางๆ ไว้ (ดูคอมเมนต์ท้าย CloseAnimation)
    // เรียกก่อน OpenAnimation เสมอ
    private void RestoreNotebookPose()
    {
        Transform root = ResolveNotebookRoot();

        root.localScale = notebookBaseScale;
        root.localRotation = notebookBaseRotation;
        if (notebookCanvasGroup != null) notebookCanvasGroup.alpha = 1f;
        SetBackdropAlpha(backdropMaxAlpha);
    }

    // เปลี่ยนความจางของภาพฉากหลังตรงๆ ผ่านสี ไม่ใช้ CanvasGroup เพราะฉากหลังเป็นแค่ Image ใบเดียว ไม่มีลูกที่ต้องจางตาม
    private void SetBackdropAlpha(float alpha)
    {
        if (backdropImage == null) return;

        Color color = backdropImage.color;
        color.a = alpha;
        backdropImage.color = color;
    }

    // ===== หน้าประวัติเลื่อนเข้า/เลื่อนออก =====

    // หน้าประวัติไถลเข้ามาจากทางขวาแล้วหยุดนิ่งที่ตำแหน่งจริงเลย ไม่เลยเป้าแล้วดีดกลับ
    // ห้ามใช้ EaseOutBack ตรงนี้ เพราะรูปผู้ต้องสงสัยเลื่อนไปกับหน้าด้วย พอหน้าดีดกลับรูปจะดูเด้งตาม
    private IEnumerator SlideDetailTabIn()
    {
        if (suspectDetailTabRoot == null)
        {
            yield break;
        }

        // SetTabVisible คืนตำแหน่ง/ความจางให้เต็มก่อน แล้วเราค่อยเขียนค่าตั้งต้นทับ ลำดับนี้สลับกันไม่ได้
        SetTabVisible(suspectDetailTabRoot, true);

        float duration = Mathf.Max(0.01f, detailSlideInDuration);

        // ระหว่างหน้ายังไถลไม่เข้าที่ ห้ามให้ลากตราปั๊มได้ ไม่งั้นปั๊มโดนก่อนที่ผู้เล่นจะเห็นว่ากดใคร
        if (detailTabCanvasGroup != null) detailTabCanvasGroup.blocksRaycasts = false;

        SetDetailTabOffset(detailSlideDistance);
        if (detailTabCanvasGroup != null) detailTabCanvasGroup.alpha = 0f;

        float time = 0f;
        while (time < duration)
        {
            // unscaledDeltaTime เหมือนแอนิเมชันอื่นในสมุด เผื่อหน้าเลือกคนร้ายไปหยุดเวลาเกมไว้
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            // smoothstep ค้างอยู่ในช่วง 0..1 ตลอด หน้าเลยไถลเข้ามาช้าลงเรื่อยๆ แล้วจอดสนิท ไม่แกว่งเลยเป้า
            SetDetailTabOffset(Mathf.Lerp(detailSlideDistance, 0f, t * t * (3f - 2f * t)));

            // ให้จางเข้ามาไวกว่าการเลื่อน ผู้เล่นจะได้เริ่มอ่านชื่อได้ก่อนหน้าจะหยุดสนิท
            if (detailTabCanvasGroup != null) detailTabCanvasGroup.alpha = Mathf.Clamp01(t / 0.4f);

            yield return null;
        }

        SetDetailTabOffset(0f);
        if (detailTabCanvasGroup != null)
        {
            detailTabCanvasGroup.alpha = 1f;
            detailTabCanvasGroup.blocksRaycasts = true;
        }
    }

    // หน้าคนเก่าไถลออกไปทางซ้าย สวนทางกับหน้าคนใหม่ที่กำลังจะเข้ามาจากทางขวา
    // ทิศตรงข้ามกันคือสิ่งที่บอกผู้เล่นว่าหน้านี้ถูกแทนที่ ไม่ใช่แค่ปิดไปเฉยๆ
    // ใช้ ease-in คือออกช้าๆ แล้วสะบัดหายตอนท้าย จะได้ไม่ดูเหมือนเด้งกลับออกไป
    private IEnumerator SlideDetailTabOut()
    {
        if (suspectDetailTabRoot == null || !suspectDetailTabRoot.activeSelf)
        {
            yield break;
        }

        CacheDetailTabBase();

        float duration = Mathf.Max(0.01f, detailSlideOutDuration);

        if (detailTabCanvasGroup != null) detailTabCanvasGroup.blocksRaycasts = false;

        // เริ่มจากตำแหน่ง/ความจางปัจจุบัน ไม่ใช่ตำแหน่งจริงของหน้า
        // เผื่อผู้เล่นรัวกดการ์ดใบใหม่ตอนหน้าคนก่อนยังไถลเข้ามาไม่สุด หน้าจะได้ไหลออกต่อจากจุดที่ค้างอยู่ ไม่กระตุกไปเริ่มใหม่
        float startOffset = GetDetailTabOffset();
        float startAlpha = detailTabCanvasGroup != null ? detailTabCanvasGroup.alpha : 1f;

        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            float ease = t * t;

            SetDetailTabOffset(Mathf.Lerp(startOffset, -detailSlideDistance, ease));
            if (detailTabCanvasGroup != null) detailTabCanvasGroup.alpha = startAlpha * (1f - ease);

            yield return null;
        }

        // SetTabVisible คืนตำแหน่ง/ความจางให้ครบตอนปิด รอบหน้าที่เปิดจะได้ไม่โผล่มาค้างนอกจอ
        SetTabVisible(suspectDetailTabRoot, false);
    }

    // อ่านตำแหน่งจริงของหน้าประวัติเก็บไว้ครั้งเดียว ก่อนที่แอนิเมชันจะไปเขียนทับ
    // ถ้าไปอ่านตอนหน้ากำลังไถลอยู่ จะได้ตำแหน่งกลางทางมาเป็นค่าตั้งต้น หน้าจะเพี้ยนค้างถาวร
    private void CacheDetailTabBase()
    {
        if (suspectDetailTabRoot == null || hasCachedDetailTabBase)
        {
            return;
        }

        hasCachedDetailTabBase = true;
        detailTabRect = suspectDetailTabRoot.GetComponent<RectTransform>();
        detailTabCanvasGroup = EnsureCanvasGroup(suspectDetailTabRoot);
        detailTabBasePos = detailTabRect != null
            ? detailTabRect.anchoredPosition
            : (Vector2)suspectDetailTabRoot.transform.localPosition;

        // ที่อยู่เดิมในสมุด เก็บไว้เอาไปคืนหลังจากหน้านี้ถูกดึงออกไปโชว์กลางจอ
        detailTabHomeParent = suspectDetailTabRoot.transform.parent;
        detailTabHomeSiblingIndex = suspectDetailTabRoot.transform.GetSiblingIndex();
        detailTabBaseScale = suspectDetailTabRoot.transform.localScale;
        detailTabBaseRotation = suspectDetailTabRoot.transform.localRotation;

        // true = หา Canvas เจอถึงแม้ตอนนี้หน้าประวัติยังถูกปิดอยู่
        detailTabCanvas = suspectDetailTabRoot.GetComponentInParent<Canvas>(true);
    }

    // เลื่อนหน้าออกจากตำแหน่งจริงไปทางขวาเท่ากับ offsetX
    // ใช้ anchoredPosition ถ้าเป็น RectTransform เพราะ RectTransform คำนวณ localPosition ใหม่เองจาก anchor
    private void SetDetailTabOffset(float offsetX)
    {
        CacheDetailTabBase();

        if (detailTabRect != null)
        {
            detailTabRect.anchoredPosition = detailTabBasePos + new Vector2(offsetX, 0f);
            return;
        }

        if (suspectDetailTabRoot != null)
        {
            Transform target = suspectDetailTabRoot.transform;
            target.localPosition = new Vector3(detailTabBasePos.x + offsetX, detailTabBasePos.y, target.localPosition.z);
        }
    }

    // ระยะที่หน้าประวัติเลื่อนออกจากตำแหน่งจริงอยู่ตอนนี้ ใช้ตอนไถลออกกลางคันจะได้ต่อจากจุดเดิม
    private float GetDetailTabOffset()
    {
        CacheDetailTabBase();

        if (detailTabRect != null)
        {
            return detailTabRect.anchoredPosition.x - detailTabBasePos.x;
        }

        if (suspectDetailTabRoot != null)
        {
            return suspectDetailTabRoot.transform.localPosition.x - detailTabBasePos.x;
        }

        return 0f;
    }

    private void RestoreDetailTabPose()
    {
        RestoreDetailTabHome();
        SetDetailTabOffset(0f);

        if (detailTabCanvasGroup != null)
        {
            detailTabCanvasGroup.alpha = 1f;
            detailTabCanvasGroup.blocksRaycasts = true;
        }
    }

    // เอาหน้าประวัติกลับเข้าไปอยู่ในสมุดที่เดิม หลังจากรอบก่อนมันถูกดึงออกไปโชว์กลางจอ
    // ต้องคืนทั้งพ่อ ลำดับ สเกล และมุมเอียง ไม่งั้นเปิดสมุดรอบหน้าหน้าประวัติจะค้างอยู่กลางจอเป็นใบใหญ่ๆ เอียงๆ นอกสมุด
    private void RestoreDetailTabHome()
    {
        CacheDetailTabBase();

        if (detailTabRect == null || detailTabHomeParent == null)
        {
            return;
        }

        if (detailTabRect.parent != detailTabHomeParent)
        {
            // false = ใช้ค่า anchor/ตำแหน่งในสมุดตามที่จัดไว้ ไม่ต้องไปคำนวณย้อนจากตำแหน่งกลางจอ
            detailTabRect.SetParent(detailTabHomeParent, false);
            detailTabRect.SetSiblingIndex(detailTabHomeSiblingIndex);
        }

        detailTabRect.localScale = detailTabBaseScale;
        detailTabRect.localRotation = detailTabBaseRotation;
    }

    // ===== หน้าเบาะแสไถลเข้า/ออก =====

    // หน้าเบาะแสไถลเข้ามาจากทางซ้ายพร้อมขยายจากขนาดย่อและจางเข้ามา ตอนสมุดเปิดเสร็จ
    // ทิศตรงข้ามกับตอนปั๊มเสร็จแล้วไถลออก (HideClueTabAnimation) ใช้ระยะ/ขนาดเริ่มต้นชุดเดียวกัน แค่วิ่งกลับทาง
    // ใช้ smoothstep เหมือนหน้าประวัติตอนไถลเข้า (SlideDetailTabIn) เพราะข้างในมีตัวหนังสือขยับไปด้วย ห้ามเด้งเลยเป้า
    private IEnumerator ShowClueTabAnimation()
    {
        if (clueTabRoot == null)
        {
            yield break;
        }

        CacheClueTabBase();

        // SetTabVisible คืนตำแหน่ง/สเกล/ความจางให้เต็มก่อน แล้วเราค่อยเขียนค่าตั้งต้นทับ ลำดับนี้สลับกันไม่ได้
        // (เหตุผลเดียวกับ SlideDetailTabIn)
        SetTabVisible(clueTabRoot, true);

        float duration = Mathf.Max(0.01f, clueTabShowDuration);

        // ระหว่างหน้ายังไถลไม่เข้าที่ ห้ามกดโดนของข้างในได้
        if (clueTabCanvasGroup != null) clueTabCanvasGroup.blocksRaycasts = false;

        float startOffset = -Mathf.Abs(clueTabHideDistance);
        Vector3 startScale = clueTabBaseScale * Mathf.Clamp(clueTabHideScale, 0.1f, 1f);

        SetClueTabOffset(startOffset);
        clueTabRoot.transform.localScale = startScale;
        if (clueTabCanvasGroup != null) clueTabCanvasGroup.alpha = 0f;

        float time = 0f;
        while (time < duration)
        {
            // unscaledDeltaTime เหมือนแอนิเมชันอื่นในสมุด เผื่อหน้าเลือกคนร้ายไปหยุดเวลาเกมไว้
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float ease = t * t * (3f - 2f * t);

            SetClueTabOffset(Mathf.Lerp(startOffset, 0f, ease));
            clueTabRoot.transform.localScale = Vector3.Lerp(startScale, clueTabBaseScale, ease);
            if (clueTabCanvasGroup != null) clueTabCanvasGroup.alpha = ease;

            yield return null;
        }

        SetClueTabOffset(0f);
        clueTabRoot.transform.localScale = clueTabBaseScale;
        if (clueTabCanvasGroup != null)
        {
            clueTabCanvasGroup.alpha = 1f;
            clueTabCanvasGroup.blocksRaycasts = true;
        }

        clueTabShowRoutine = null;
    }

    // อ่านตำแหน่ง/สเกลจริงของหน้าเบาะแสเก็บไว้ครั้งเดียว ก่อนที่แอนิเมชันตอนปั๊มเสร็จจะไปเขียนทับ
    // เหตุผลเดียวกับ CacheDetailTabBase คือถ้าไปอ่านตอนหน้ากำลังไถลอยู่ จะได้ค่ากลางทางมาเป็นค่าตั้งต้น แล้วหน้าจะเพี้ยนค้างถาวร
    private void CacheClueTabBase()
    {
        if (clueTabRoot == null || hasCachedClueTabBase)
        {
            return;
        }

        hasCachedClueTabBase = true;
        clueTabRect = clueTabRoot.GetComponent<RectTransform>();
        clueTabCanvasGroup = EnsureCanvasGroup(clueTabRoot);
        clueTabBasePos = clueTabRect != null
            ? clueTabRect.anchoredPosition
            : (Vector2)clueTabRoot.transform.localPosition;
        clueTabBaseScale = clueTabRoot.transform.localScale;
    }

    // เลื่อนหน้าเบาะแสออกจากตำแหน่งจริงไปทางขวาเท่ากับ offsetX (ค่าลบ = ไปทางซ้าย)
    // ใช้ anchoredPosition ถ้าเป็น RectTransform ด้วยเหตุผลเดียวกับ SetDetailTabOffset
    private void SetClueTabOffset(float offsetX)
    {
        CacheClueTabBase();

        if (clueTabRect != null)
        {
            clueTabRect.anchoredPosition = clueTabBasePos + new Vector2(offsetX, 0f);
            return;
        }

        if (clueTabRoot != null)
        {
            Transform target = clueTabRoot.transform;
            target.localPosition = new Vector3(clueTabBasePos.x + offsetX, clueTabBasePos.y, target.localPosition.z);
        }
    }

    private void RestoreClueTabPose()
    {
        CacheClueTabBase();

        if (clueTabRoot == null)
        {
            return;
        }

        SetClueTabOffset(0f);
        clueTabRoot.transform.localScale = clueTabBaseScale;

        if (clueTabCanvasGroup != null)
        {
            clueTabCanvasGroup.alpha = 1f;
            clueTabCanvasGroup.blocksRaycasts = true;
        }
    }

    // ปั๊มเสร็จแล้วหน้าเบาะแสไถลออกไปทางซ้ายพร้อมย่อและจางหาย
    // ทิศเดียวกับหน้าประวัติคนเก่าที่ถูกแทนที่ ผู้เล่นเลยอ่านออกว่า "ของถูกเก็บออกไป" ไม่ใช่แค่หายวับ
    // ใช้ ease-in คือออกช้าๆ แล้วสะบัดหายตอนท้าย จะได้ไม่ดูเหมือนเด้งกลับออกไป
    private IEnumerator HideClueTabAnimation()
    {
        if (!hideClueTabAfterStamp) yield break;
        if (clueTabRoot == null || !clueTabRoot.activeSelf) yield break;

        CacheClueTabBase();

        // ถ้าเบาะแสยังทยอยเด้งขึ้นมาไม่ครบ ต้องหยุดตรงนี้ ไม่งั้นมันจะแย่งเขียนสเกล/ความจางของแต่ละบรรทัด
        // สวนกับหน้าที่กำลังจางหายอยู่ แล้วจะเห็นบรรทัดกระพริบกลับมาชัดตอนหน้ากำลังจะหายลับ
        if (clueEntryRoutine != null)
        {
            StopCoroutine(clueEntryRoutine);
            clueEntryRoutine = null;
        }

        // เผื่อผู้เล่นปั๊มไวมากจนหน้ายังไถลเข้าไม่ทันสุด (ShowClueTabAnimation ยังไม่จบ)
        // ต้องหยุดตัวนั้นก่อน ไม่งั้นสองโครูทีนจะแย่งกันเขียนตำแหน่ง/สเกลของหน้าเดียวกันคนละทิศในเฟรมเดียวกัน
        if (clueTabShowRoutine != null)
        {
            StopCoroutine(clueTabShowRoutine);
            clueTabShowRoutine = null;
        }

        float duration = Mathf.Max(0.01f, clueTabHideDuration);

        // ปั๊มไปแล้ว ห้ามกดโดนอะไรบนหน้าเบาะแสได้อีกระหว่างที่มันกำลังไถลออก
        if (clueTabCanvasGroup != null) clueTabCanvasGroup.blocksRaycasts = false;

        float startAlpha = clueTabCanvasGroup != null ? clueTabCanvasGroup.alpha : 1f;
        Vector3 startScale = clueTabRoot.transform.localScale;
        Vector3 endScale = clueTabBaseScale * Mathf.Clamp(clueTabHideScale, 0.1f, 1f);

        float time = 0f;
        while (time < duration)
        {
            // unscaledDeltaTime เหมือนแอนิเมชันอื่นในสมุด เผื่อหน้าเลือกคนร้ายไปหยุดเวลาเกมไว้
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            float ease = t * t;

            SetClueTabOffset(Mathf.Lerp(0f, -clueTabHideDistance, ease));
            clueTabRoot.transform.localScale = Vector3.Lerp(startScale, endScale, ease);
            if (clueTabCanvasGroup != null) clueTabCanvasGroup.alpha = startAlpha * (1f - ease);

            yield return null;
        }

        // SetTabVisible คืนตำแหน่ง/สเกล/ความจางให้ครบตอนปิด รอบหน้าที่เปิดสมุดหน้าเบาะแสจะได้ไม่โผล่มาค้างนอกจอแบบจางๆ
        SetTabVisible(clueTabRoot, false);
    }

    // ===== หน้าประวัติลอยไปกลางจอตอนปั๊มเสร็จ =====

    // ย้ายหน้าประวัติไปแขวนไว้บน Canvas ชั้นบนสุด โดยให้ยังอยู่ตรงตำแหน่งเดิมบนจอ
    // ทำก่อนสมุดเริ่มหุบ หน้านี้จะได้ค้างอยู่ตอนสมุดย่อหายไปข้างใต้ เหมือนหยิบแฟ้มออกมาถือไว้
    private bool PrepareDetailShowcase()
    {
        if (!showcaseDetailAfterStamp) return false;
        if (suspectDetailTabRoot == null || !suspectDetailTabRoot.activeSelf) return false;

        CacheDetailTabBase();

        RectTransform layer = ResolveShowcaseLayer();
        if (layer == null || detailTabRect == null) return false;

        // true = คงตำแหน่ง/ขนาดบนจอไว้เหมือนเดิม หน้าประวัติจะได้ไม่กระโดดตอนหลุดออกจากสมุด
        detailTabRect.SetParent(layer, true);
        detailTabRect.SetAsLastSibling();

        if (detailTabCanvasGroup != null)
        {
            detailTabCanvasGroup.alpha = 1f;

            // ปั๊มไปแล้ว ห้ามกดอะไรบนหน้านี้ได้อีก และห้ามให้มันไปบังปุ่มบนจอแพ้/ชนะที่กำลังจะขึ้นมา
            detailTabCanvasGroup.blocksRaycasts = false;
        }

        return true;
    }

    // ลอยไปหยุดกลางจอพร้อมขยายขึ้นนิดหนึ่ง ใช้ smoothstep ให้ไปจอดสนิท ไม่เลยเป้าแล้วดีดกลับ
    // (เหตุผลเดียวกับหน้าประวัติตอนไถลเข้ามา คือมีรูปคนอยู่ในนั้น พอดีดกลับแล้วรูปจะดูสั่น)
    private IEnumerator MoveDetailTabToCenter()
    {
        if (detailTabRect == null)
        {
            yield break;
        }

        Vector3 centerWorld;
        if (!TryGetShowcaseCenterWorld(out centerWorld))
        {
            yield break;
        }

        float duration = Mathf.Max(0.01f, showcaseMoveDuration);

        Vector3 startPos = detailTabRect.position;
        Vector3 startScale = detailTabRect.localScale;
        Vector3 endScale = startScale * Mathf.Max(0.01f, showcaseScale);

        // เอียงต่อจากมุมที่หน้ากำลังวางอยู่ตอนนี้ ไม่ใช่จากมุมศูนย์
        // เพราะตอน PrepareDetailShowcase ย้ายหน้าไปแขวนใต้ Canvas ชั้นบนสุดแบบคงท่าเดิมบนจอ
        // มุม local ตอนนี้เลยอาจไม่เท่ากับมุมที่เก็บไว้ตอนมันยังอยู่ในสมุด
        Quaternion startRot = detailTabRect.localRotation;
        Quaternion endRot = startRot * Quaternion.Euler(0f, 0f, showcaseTiltAngle);

        float time = 0f;
        while (time < duration)
        {
            // unscaledDeltaTime เหมือนแอนิเมชันอื่นในสมุด เผื่อหน้าเลือกคนร้ายไปหยุดเวลาเกมไว้
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float ease = t * t * (3f - 2f * t);

            detailTabRect.localScale = Vector3.Lerp(startScale, endScale, ease);

            // ต้องหมุนก่อนคิดตำแหน่ง เพราะ GetPivotToCenterOffset อ่านค่าจาก TransformPoint
            // ซึ่งขึ้นกับมุมของใบด้วย ถ้าหมุนทีหลังใบที่ pivot ไม่ได้อยู่กลางจะเยื้องออกจากกลางจอ
            detailTabRect.localRotation = Quaternion.Slerp(startRot, endRot, ease);

            // จุดกึ่งกลางของใบขยับตามสเกลที่กำลังโตขึ้น เลยต้องคิดออฟเซ็ตใหม่ทุกเฟรม
            // ถ้าคิดครั้งเดียวตอนเริ่ม หน้าที่ pivot ไม่ได้อยู่กลางใบจะไปหยุดเยื้องจากกลางจอ
            detailTabRect.position = Vector3.Lerp(startPos, centerWorld - GetPivotToCenterOffset(detailTabRect), ease);

            yield return null;
        }

        detailTabRect.localScale = endScale;
        detailTabRect.localRotation = endRot;
        detailTabRect.position = centerWorld - GetPivotToCenterOffset(detailTabRect);
    }

    private RectTransform ResolveShowcaseLayer()
    {
        if (showcaseLayer != null) return showcaseLayer;

        CacheDetailTabBase();

        return detailTabCanvas != null ? detailTabCanvas.rootCanvas.transform as RectTransform : null;
    }

    private bool TryGetShowcaseCenterWorld(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;

        RectTransform layer = ResolveShowcaseLayer();
        if (layer == null) return false;

        Vector2 screenPoint = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + showcaseCenterOffset;

        return RectTransformUtility.ScreenPointToWorldPointInRectangle(layer, screenPoint, GetShowcaseCamera(), out worldPoint);
    }

    private Camera GetShowcaseCamera()
    {
        if (detailTabCanvas == null) return null;

        // Canvas แบบ Overlay ต้องส่งกล้องเป็น null ไม่งั้นจะแปลงพิกัดจอเพี้ยน
        Canvas root = detailTabCanvas.rootCanvas;

        return root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
    }

    // ใบที่ pivot ไม่ได้อยู่ตรงกลาง ถ้าเอา position ไปวางกลางจอตรงๆ ใบจะเยื้องออกไปครึ่งใบ
    private static Vector3 GetPivotToCenterOffset(RectTransform rect)
    {
        Rect rectSize = rect.rect;
        Vector2 pivot = rect.pivot;

        Vector3 centerLocal = new Vector3((0.5f - pivot.x) * rectSize.width, (0.5f - pivot.y) * rectSize.height, 0f);

        return rect.TransformPoint(centerLocal) - rect.position;
    }

    // หน้าประวัติจะค้างอยู่กลางจอต่อไปหลังสมุดหายไปแล้ว เผื่อจอแพ้/ชนะอยากเก็บมันทิ้งเมื่อไหร่ก็เรียกตัวนี้
    public void HideSuspectShowcase()
    {
        ShowSuspectDetailTab(false);
    }

    // ทยอยเด้งของทีละชิ้นตามลำดับ ใช้ coroutine เดียวคุมทั้งลิสต์ จะได้หยุดทีเดียวจบ
    private IEnumerator PlayEntryAnimation(List<NotebookEntry> entries)
    {
        if (entries == null || entries.Count == 0)
        {
            yield break;
        }

        HideEntries(entries);

        // รอหนึ่งเฟรมให้ Layout Group จัดตำแหน่งลูกๆ ที่เพิ่งสร้างเสร็จก่อน
        yield return null;

        float duration = Mathf.Max(0.01f, entryPopDuration);
        float stagger = Mathf.Max(0f, entryStagger);
        float total = duration + stagger * (entries.Count - 1);

        float time = 0f;
        while (time < total)
        {
            time += Time.unscaledDeltaTime;

            for (int i = 0; i < entries.Count; i++)
            {
                NotebookEntry entry = entries[i];
                if (entry.target == null) continue;

                float t = Mathf.Clamp01((time - stagger * i) / duration);

                entry.target.localScale = entry.baseScale * EaseOutBack(t);
                if (entry.group != null) entry.group.alpha = t;
            }

            yield return null;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            NotebookEntry entry = entries[i];
            if (entry.target == null) continue;

            entry.target.localScale = entry.baseScale;
            if (entry.group != null)
            {
                entry.group.alpha = 1f;
                entry.group.blocksRaycasts = true;
            }
        }
    }

    private NotebookEntry BuildEntry(GameObject target)
    {
        NotebookEntry entry = new NotebookEntry();
        entry.target = target.transform;
        entry.group = EnsureCanvasGroup(target);

        // เก็บสเกลจริงไว้ตั้งแต่ตอนเพิ่งเกิด ก่อนที่เราจะไปเขียนทับเป็น 0
        entry.baseScale = target.transform.localScale;

        return entry;
    }

    private void HideEntries(List<NotebookEntry> entries)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            NotebookEntry entry = entries[i];
            if (entry.target == null) continue;

            entry.target.localScale = Vector3.zero;
            if (entry.group != null)
            {
                entry.group.alpha = 0f;
                // การ์ดที่ยังไม่โผล่ต้องกดไม่ได้ด้วย ไม่งั้นผู้เล่นจะเผลอกดโดนของที่ยังมองไม่เห็น
                entry.group.blocksRaycasts = false;
            }
        }
    }

    private CanvasGroup EnsureCanvasGroup(GameObject target)
    {
        if (target == null)
        {
            return null;
        }

        CanvasGroup group = target.GetComponent<CanvasGroup>();
        if (group == null) group = target.AddComponent<CanvasGroup>();

        return group;
    }

    private void SetNotebookInteractable(bool canInteract)
    {
        if (notebookCanvasGroup == null)
        {
            return;
        }

        notebookCanvasGroup.interactable = canInteract;
        notebookCanvasGroup.blocksRaycasts = canInteract;
    }

    // ease-out back: พุ่งเลยเป้าไปนิดหนึ่งแล้วค่อยดีดกลับ ใช้ค่าเดียวกับที่ Customer/InvestigateGroup ใช้
    private static float EaseOutBack(float t)
    {
        float overshoot = 1.70158f;
        float easedT = t - 1f;

        return 1f + (overshoot + 1f) * Mathf.Pow(easedT, 3f) + overshoot * Mathf.Pow(easedT, 2f);
    }
}