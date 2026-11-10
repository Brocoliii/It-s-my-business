using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// แผงสรุปผลตอนจบด่าน โชว์ว่าผู้เล่นทำได้เท่าไหร่ในด่านนี้
//
//     CASE CLOSED
//     Orders Completed: 12
//     Clues Found: 4/4
//     Mistakes: 1/3
//     [ NEXT CASE ]  [ RETRY ]  [ MAIN MENU ]
//
// วิธีใช้: ลากสคริปต์นี้ใส่ GameObject เปล่า ๆ ในฉาก แล้วจบ
// (ไม่ผูกอะไรใน Inspector เลยก็ใช้ได้ ตัวสคริปต์จะสร้างแผงให้เองตอนเริ่มฉาก)
// ถ้าอยากจัดหน้าตาเองก็ผูกช่องต่าง ๆ ใน Inspector แทนได้ ช่องไหนผูกไว้แล้วจะไม่ถูกสร้างทับ
//
// GameManager เป็นคนสั่งโชว์ตอนเข้าสถานะ Win (และตอน Lose ถ้าเปิด Show On Lose ไว้)
// ฉากไหนมีแผงนี้อยู่ ด่านจะ "ไม่" เด้งไปฉากถัดไปเองอัตโนมัติอีก แต่รอให้ผู้เล่นกด NEXT CASE
//
// หมายเหตุ: GameObject ที่แปะสคริปต์นี้ต้องเปิดอยู่ตลอด (ตัวที่ถูกซ่อนคือแผงข้างในซึ่งเป็นลูกของมัน)
[DisallowMultipleComponent]
public class StageResultPanel : MonoBehaviour
{
    public static StageResultPanel Instance { get; private set; }

    [Header("แหล่งข้อมูล")]
    [Tooltip("ปล่อยว่างได้ จะไปหา GameManager ในฉากให้เอง")]
    public GameManager gameManager;
    [Tooltip("ปล่อยว่างได้ จะไปหา InvestigationManager ในฉากให้เอง\nถ้าฉากไม่มีตัวนี้ จำนวนเบาะแสจะโชว์เป็น 0 เฉย ๆ ไม่พัง")]
    public InvestigationManager investigationManager;

    [Header("สร้าง UI อัตโนมัติ")]
    [Tooltip("ติ๊กถูก = ช่องไหนไม่ได้ผูกไว้ จะสร้างให้เองตอนเริ่มฉาก")]
    public bool buildUiIfMissing = true;
    [Tooltip("ฟอนต์ที่ใช้ตอนสร้างข้อความให้เอง ถ้าไม่ใส่จะใช้ฟอนต์ปริยายของ TextMeshPro")]
    public TMP_FontAsset panelFont;
    [Tooltip("ลำดับการวางทับของ Canvas ที่สร้างให้เอง ต้องมากกว่าของสมุดหลักฐาน/HUD ไม่งั้นแผงจะโดนบัง")]
    public int canvasSortingOrder = 300;

    [Header("ชิ้นส่วนของแผง")]
    [Tooltip("แผงทั้งใบ (ตัวที่จะถูกเปิด/ปิด) ปล่อยว่าง = สร้างให้เอง")]
    public GameObject panelRoot;
    [Tooltip("ภาพดำทับจอด้านหลังแผง ไม่ใส่ก็ได้")]
    public Image dimmerImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI ordersText;
    public TextMeshProUGUI cluesText;
    public TextMeshProUGUI mistakesText;
    public TextMeshProUGUI timeText;
    [Tooltip("บรรทัดเล็กใต้หัวข้อ ใช้โชว์ชื่อด่าน หรือสาเหตุที่แพ้ ไม่ใส่ก็ได้")]
    public TextMeshProUGUI subtitleText;
    [Tooltip("ภาพอันดับที่แสดงจากคะแนนออเดอร์ที่ทำสำเร็จ")]
    public Image rankImage;

    [Header("ข้อความ")]
    public string winTitle = "CASE CLOSED";
    public string loseTitle = "CASE FAILED";
    [Tooltip("{0} = จำนวนออเดอร์ที่เสิร์ฟถูกใจลูกค้า")]
    public string ordersFormat = "Orders Completed: {0}";
    [Tooltip("{0} = เบาะแสที่เก็บได้, {1} = เบาะแสทั้งหมดที่นับเป็นเป้า")]
    public string cluesFormat = "Clues Found: {0}/{1}";
    [Tooltip("{0} = จำนวนครั้งที่พลาด, {1} = โควตาความผิดพลาดของด่าน")]
    public string mistakesFormat = "Mistakes: {0}/{1}";
    [Tooltip("{0} = นาที, {1} = วินาที")]
    public string timeFormat = "Time: {0:00}:{1:00}";
    [Tooltip("ติ๊กถูก = ช่อง Clues Found เทียบกับเบาะแสทั้งหมดของด่าน (Clues Pool)\nติ๊กออก = เทียบกับโควตาที่ต้องเก็บให้ผ่าน (Required Clues To Pass)")]
    public bool countCluesAgainstPool = true;
    [Tooltip("ติ๊กถูก = โชว์ชื่อด่าน (Stage Name) ไว้ใต้หัวข้อตอนชนะ")]
    public bool showStageNameOnWin = true;

    [Header("อันดับจากคะแนน")]
    [Tooltip("คะแนนที่ต้องมีอย่างน้อยเพื่อได้อันดับ A (คะแนน = จำนวนออเดอร์ที่ทำสำเร็จ)")]
    [Min(0)] public int rankARequiredScore = 8;
    [Tooltip("คะแนนที่ต้องมีอย่างน้อยเพื่อได้อันดับ B (คะแนน = จำนวนออเดอร์ที่ทำสำเร็จ)")]
    [Min(0)] public int rankBRequiredScore = 5;
    [Tooltip("คะแนนที่ต้องมีอย่างน้อยเพื่อได้อันดับ C (คะแนน = จำนวนออเดอร์ที่ทำสำเร็จ)")]
    [Min(0)] public int rankCRequiredScore = 3;
    public Sprite rankASprite;
    public Sprite rankBSprite;
    public Sprite rankCSprite;
    public Sprite rankDSprite;

    [Header("ปุ่มด้านล่าง")]
    [Tooltip("ปุ่มบันทึกเกม โผล่เฉพาะตอนชนะและยังมีด่านถัดไปให้เล่นต่อ\nกดแล้วจะเปิดแผงให้เลือกช่องเซฟ (แผงสร้างตัวเองตอนถูกเรียก ไม่ต้องเตรียมอะไรในฉาก)")]
    public Button saveButton;
    public Button nextCaseButton;
    public Button retryButton;
    public Button mainMenuButton;
    public string saveLabel = "SAVE";
    public string nextCaseLabel = "NEXT CASE";
    public string retryLabel = "RETRY";
    public string mainMenuLabel = "MAIN MENU";

    [Header("แอนิเมชันก่อนเปลี่ยนฉาก")]
    [Tooltip("ภาพดำเต็มจอสำหรับเฟดก่อน Retry, Next Case หรือ Main Menu ปล่อยว่างเพื่อสร้างให้อัตโนมัติ")]
    public Image transitionFadeImage;
    public float transitionFadeDuration = 0.35f;

    [Header("Localization")]
    [Tooltip("Optional localized result title for a won stage. Leave empty to use Win Title.")]
    public LocalizedString winTitleLocalized;
    [Tooltip("Optional localized result title for a lost stage. Leave empty to use Lose Title.")]
    public LocalizedString loseTitleLocalized;
    [Tooltip("Optional localized format for completed orders. Keep the {0} placeholder.")]
    public LocalizedString ordersFormatLocalized;
    [Tooltip("Optional localized format for clues found. Keep the {0} and {1} placeholders.")]
    public LocalizedString cluesFormatLocalized;
    [Tooltip("Optional localized format for mistakes. Keep the {0} and {1} placeholders.")]
    public LocalizedString mistakesFormatLocalized;
    [Tooltip("Optional localized format for elapsed time. Keep the {0} and {1} placeholders.")]
    public LocalizedString timeFormatLocalized;
    [Tooltip("Optional localized SAVE button label.")]
    public LocalizedString saveLabelLocalized;
    [Tooltip("Optional localized NEXT CASE button label.")]
    public LocalizedString nextCaseLabelLocalized;
    [Tooltip("Optional localized RETRY button label.")]
    public LocalizedString retryLabelLocalized;
    [Tooltip("Optional localized MAIN MENU button label.")]
    public LocalizedString mainMenuLabelLocalized;

    [Tooltip("ชื่อฉากเมนูหลัก ปล่อยว่าง = ไปเอาค่าจาก Main Menu Scene Name ของ UIManager ในฉากแทน")]
    public string mainMenuSceneName = "";
    [Tooltip("ติ๊กถูก = ซ่อนปุ่ม NEXT CASE ไปเลยตอนไม่มีด่านถัดไป (หรือตอนแพ้)\nติ๊กออก = โชว์ไว้แต่กดไม่ได้")]
    public bool hideNextCaseWhenUnavailable = true;

    [Header("การแสดงผล")]
    [Tooltip("ติ๊กถูก = โชว์แผงนี้ตอนแพ้ด้วย\nเปิดเฉพาะตอนที่ไม่ได้ใช้หน้าจอแพ้ (Lose Panel) ของ UIManager แล้วเท่านั้น ไม่งั้นสองแผงจะซ้อนกัน")]
    public bool showOnLose = false;
    [Tooltip("หน่วงกี่วินาทีก่อนแผงจะเด้งขึ้นมา (เผื่อให้แอนิเมชันจบด่านเล่นจบก่อน)")]
    public float showDelay = 0.35f;
    [Tooltip("เวลาที่แผงใช้เลื่อนขึ้นมา")]
    public float popDuration = 0.4f;
    [Tooltip("ระยะที่แผงเลื่อนขึ้นมาจากด้านล่างของหน้าจอ (พิกเซล)")]
    public float slideDistance = 260f;
    [Tooltip("เวลาที่ตัวเลขแต่ละบรรทัดทยอยโผล่ตามกันมา ใส่ 0 = ขึ้นพร้อมกันหมด")]
    public float rowStagger = 0.12f;
    [Tooltip("หน่วงเวลาก่อนภาพอันดับจะเริ่มปรากฏ")]
    public float rankRevealDelay = 0.25f;
    [Tooltip("ระยะเวลาของแอนิเมชันตอนภาพอันดับปรากฏ")]
    public float rankRevealDuration = 0.35f;
    [Tooltip("ขนาดเริ่มต้นของภาพอันดับก่อนเด้งเข้า")]
    [Range(0f, 1f)] public float rankRevealStartScale = 0.65f;
    [Tooltip("ติ๊กถูก = หยุดเวลาเกมตอนแผงเปิดอยู่ (คืนให้เองก่อนเปลี่ยนฉากทุกครั้ง)")]
    public bool pauseGameWhileShown = true;
    [Tooltip("เสียงที่เล่นเมื่อข้อความสถิติแต่ละบรรทัดปรากฏ")]
    public string statRevealSoundId = SoundIds.ResultStatAppear;

    private CanvasGroup panelCanvasGroup;
    private CanvasGroup rankImageCanvasGroup;
    private RectTransform animatedPanel;
    private Vector2 panelBasePosition;
    private Vector3 rankImageBaseScale = Vector3.one;
    private bool hasPanelBase = false;
    private bool hasRankImageBaseScale = false;
    private bool rankImageRevealed;
    private RectTransform builtCanvasRoot;
    private Coroutine showRoutine;
    private bool isShown = false;
    private bool hasAppliedStats;
    private bool currentIsWin;
    private string currentReason;
    private bool isLeavingResultScreen;
    private bool refreshingLocalization;

    public bool IsShown => isShown;

    // จุดที่จะเล่นต่อของด่านนี้ เก็บไว้ตอนแผงเปิด แล้วส่งให้แผงเลือกช่องเซฟตอนกดปุ่ม SAVE
    // null = ไม่มีอะไรให้เซฟ (แพ้ หรือด่านสุดท้ายที่ไม่มีด่านถัดไป) ปุ่ม SAVE จะถูกซ่อนไว้
    private SaveData pendingSaveData;

    // บรรทัดสถิติทั้งหมดตามลำดับที่จะทยอยโผล่
    private readonly TextMeshProUGUI[] statRows = new TextMeshProUGUI[4];

    private void OnEnable()
    {
        SubscribeLocalization();
    }

    private void OnDisable()
    {
        UnsubscribeLocalization();
    }

    // ทางเข้าเดียวที่ฝั่ง GameManager ใช้ หาแผงในฉากให้เอง (รวมตัวที่ถูกปิดอยู่)
    // คืน false = ฉากนี้ไม่มีแผงสรุปผล ให้ผู้เรียกไปใช้ทางเดิมของตัวเองต่อ
    public static bool TryShowResult(bool isWin, string reason = null)
    {
        StageResultPanel panel = Instance != null ? Instance : FindObjectOfType<StageResultPanel>(true);
        if (panel == null) return false;

        // เผื่อคนวางสคริปต์ไว้บน GameObject ที่ปิดอยู่ ต้องเปิดก่อนไม่งั้น coroutine ของแผงจะไม่เดิน
        if (!panel.gameObject.activeSelf) panel.gameObject.SetActive(true);

        return panel.Show(isWin, reason);
    }

    private void Awake()
    {
        // ฉากหนึ่งควรมีแผงสรุปผลใบเดียว ตัวที่เกินมาทิ้งไปเลยตั้งแต่ยังไม่ทันสร้าง UI ของตัวเอง
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        ResolveSources();

        if (buildUiIfMissing && NeedsBuild())
        {
            BuildDefaultPanel();
        }

        statRows[0] = ordersText;
        statRows[1] = cluesText;
        statRows[2] = mistakesText;
        statRows[3] = timeText;

        // ผูกปุ่มจากโค้ดแทนการผูกใน Inspector จะได้ไม่มีเคสลืมผูกแล้วปุ่มกดไม่ติด
        // ผูกตั้งแต่ Awake เพราะแผงเริ่มมาแบบปิดอยู่ ปุ่มข้างในเลยยังไม่ทำงานถ้าไปรอผูกทีหลัง
        if (saveButton != null) saveButton.onClick.AddListener(OnClickSave);
        if (nextCaseButton != null) nextCaseButton.onClick.AddListener(OnClickNextCase);
        if (retryButton != null) retryButton.onClick.AddListener(OnClickRetry);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnClickMainMenu);

        if (panelRoot != null) panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeLocalization();

        if (saveButton != null) saveButton.onClick.RemoveListener(OnClickSave);
        if (nextCaseButton != null) nextCaseButton.onClick.RemoveListener(OnClickNextCase);
        if (retryButton != null) retryButton.onClick.RemoveListener(OnClickRetry);
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveListener(OnClickMainMenu);

        // Canvas ที่สร้างเองอาจถูกวางไว้ที่ระดับฉาก (ไม่ได้เป็นลูกของเรา) ต้องเก็บทิ้งเองไม่ให้ค้างในฉาก
        if (builtCanvasRoot != null && builtCanvasRoot.parent == null) Destroy(builtCanvasRoot.gameObject);

        if (Instance == this) Instance = null;
    }

    // คืน true = รับงานแล้ว (แผงจะโชว์เอง) ผู้เรียกไม่ต้องทำอะไรต่อ
    public bool Show(bool isWin, string reason = null)
    {
        if (!isWin && !showOnLose) return false;

        if (panelRoot == null)
        {
            Debug.LogWarning("[StageResultPanel] ไม่มีแผงให้โชว์ (ปิด Build Ui If Missing ไว้ และไม่ได้ผูก Panel Root)");
            return false;
        }

        // กันสั่งซ้ำ เผื่อเข้าสถานะจบด่านมากกว่าหนึ่งครั้ง
        if (isShown) return true;
        isShown = true;

        if (showRoutine != null) StopCoroutine(showRoutine);
        showRoutine = StartCoroutine(ShowRoutine(isWin, reason));
        return true;
    }

    public void SetVisibleForPause(bool visible)
    {
        if (!isShown || panelRoot == null) return;

        panelRoot.SetActive(visible);
    }

    private IEnumerator ShowRoutine(bool isWin, string reason)
    {
        if (showDelay > 0f)
        {
            // Realtime เพราะจังหวะจบด่านมีสิทธิ์ที่เวลาเกมถูกหยุดไว้อยู่แล้ว
            yield return new WaitForSecondsRealtime(showDelay);
        }

        // เก็บ HUD ตอนเล่น + หน้าประวัติผู้ต้องสงสัยที่ลอยค้างอยู่กลางจอ ไม่งั้นมันจะทับแผงสรุปผล
        if (UIManager.Instance != null) UIManager.Instance.HideGameplayHudForEnding();

        ApplyStats(isWin, reason);

        panelRoot.SetActive(true);
        SoundManager.Instance.PlaySFX(SoundIds.ResultPanelAppear);

        if (pauseGameWhileShown) Time.timeScale = 0f;

        yield return PopRoutine();

        showRoutine = null;
    }

    private void ApplyStats(bool isWin, string reason)
    {
        currentIsWin = isWin;
        currentReason = reason;
        hasAppliedStats = true;
        ResolveSources();

        StageConfig stage = GetCurrentStage();

        int orders = gameManager != null ? Mathf.Max(0, gameManager.completedOrders) : 0;
        int mistakes = gameManager != null ? Mathf.Max(0, gameManager.currentMistakes) : 0;
        int maxMistakes = stage != null ? Mathf.Max(0, stage.maxMistakesAllowed) : 0;

        int clues = investigationManager != null ? investigationManager.collectedClues.Count : 0;
        int clueTarget = GetClueTarget(stage);
        float elapsedTime = gameManager != null ? gameManager.GetStageElapsedTime() : 0f;
        int totalSeconds = Mathf.FloorToInt(elapsedTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        if (titleText != null)
        {
            LocalizedString localizedTitle = isWin ? winTitleLocalized : loseTitleLocalized;
            titleText.text = GetLocalizedValue(localizedTitle, isWin ? winTitle : loseTitle);
        }

        if (subtitleText != null)
        {
            // ตอนแพ้เอาสาเหตุที่แพ้มาโชว์ ตอนชนะเอาชื่อด่านมาโชว์แทน
            string subtitle = !isWin && !string.IsNullOrEmpty(reason)
                ? reason
                : (isWin && showStageNameOnWin && stage != null ? stage.stageName : string.Empty);

            subtitleText.text = subtitle;
            subtitleText.gameObject.SetActive(!string.IsNullOrEmpty(subtitle));
        }

        if (ordersText != null)
        {
            string format = GetLocalizedValue(ordersFormatLocalized, ordersFormat);
            ordersText.text = string.Format(format, orders);
        }

        // เก็บเกินเป้าได้ (เบาะแสส่วนเกิน) แต่บนสรุปผลไม่ควรโชว์ 5/4 เลยตัดไว้ที่เป้า
        if (cluesText != null)
        {
            string format = GetLocalizedValue(cluesFormatLocalized, cluesFormat);
            cluesText.text = string.Format(format, Mathf.Min(clues, clueTarget), clueTarget);
        }

        if (mistakesText != null)
        {
            string format = GetLocalizedValue(mistakesFormatLocalized, mistakesFormat);
            mistakesText.text = string.Format(format, Mathf.Min(mistakes, maxMistakes), maxMistakes);
        }

        if (timeText != null)
        {
            string format = GetLocalizedValue(timeFormatLocalized, timeFormat);
            timeText.text = string.Format(format, minutes, seconds);
        }

        int rankScore = Mathf.Max(0, orders + clues - mistakes);
        ApplyRankImage(rankScore, mistakes);
        ApplyLocalizedButtonLabels();
        ApplyButtonStates(isWin);
    }

    private void ApplyRankImage(int score, int mistakes)
    {
        if (rankImage == null)
            return;

        Sprite rankSprite = GetRankSprite(score, mistakes);
        rankImage.sprite = rankSprite;
        rankImage.gameObject.SetActive(rankSprite != null);
        rankImage.preserveAspect = true;

        if (rankSprite == null)
        {
            rankImageRevealed = false;
            return;
        }

        if (!hasRankImageBaseScale)
        {
            rankImageBaseScale = rankImage.rectTransform.localScale;
            hasRankImageBaseScale = true;
        }

        if (rankImageCanvasGroup == null)
        {
            rankImageCanvasGroup = rankImage.GetComponent<CanvasGroup>();
            if (rankImageCanvasGroup == null)
                rankImageCanvasGroup = rankImage.gameObject.AddComponent<CanvasGroup>();
        }

        if (rankImageRevealed)
        {
            rankImageCanvasGroup.alpha = 1f;
            rankImage.rectTransform.localScale = rankImageBaseScale;
        }
        else
        {
            rankImageCanvasGroup.alpha = 0f;
            rankImage.rectTransform.localScale = rankImageBaseScale * Mathf.Clamp01(rankRevealStartScale);
        }
    }

    private Sprite GetRankSprite(int score, int mistakes)
    {
        if (mistakes == 0 && score >= rankARequiredScore) return rankASprite;
        if (score >= rankBRequiredScore) return rankBSprite;
        if (score >= rankCRequiredScore) return rankCSprite;
        return rankDSprite;
    }

    private void SubscribeLocalization()
    {
        SubscribeLocalizedString(winTitleLocalized);
        SubscribeLocalizedString(loseTitleLocalized);
        SubscribeLocalizedString(ordersFormatLocalized);
        SubscribeLocalizedString(cluesFormatLocalized);
        SubscribeLocalizedString(mistakesFormatLocalized);
        SubscribeLocalizedString(timeFormatLocalized);
        SubscribeLocalizedString(saveLabelLocalized);
        SubscribeLocalizedString(nextCaseLabelLocalized);
        SubscribeLocalizedString(retryLabelLocalized);
        SubscribeLocalizedString(mainMenuLabelLocalized);

        refreshingLocalization = true;
        RefreshLocalizedStrings();
        refreshingLocalization = false;

        if (hasAppliedStats)
            ApplyStats(currentIsWin, currentReason);
        else
            RefreshLocalizedButtonLabels();
    }

    private void UnsubscribeLocalization()
    {
        UnsubscribeLocalizedString(winTitleLocalized);
        UnsubscribeLocalizedString(loseTitleLocalized);
        UnsubscribeLocalizedString(ordersFormatLocalized);
        UnsubscribeLocalizedString(cluesFormatLocalized);
        UnsubscribeLocalizedString(mistakesFormatLocalized);
        UnsubscribeLocalizedString(timeFormatLocalized);
        UnsubscribeLocalizedString(saveLabelLocalized);
        UnsubscribeLocalizedString(nextCaseLabelLocalized);
        UnsubscribeLocalizedString(retryLabelLocalized);
        UnsubscribeLocalizedString(mainMenuLabelLocalized);
    }

    private void SubscribeLocalizedString(LocalizedString localizedString)
    {
        if (localizedString != null)
            localizedString.StringChanged += OnLocalizedStringChanged;
    }

    private void UnsubscribeLocalizedString(LocalizedString localizedString)
    {
        if (localizedString != null)
            localizedString.StringChanged -= OnLocalizedStringChanged;
    }

    private void OnLocalizedStringChanged(string localizedText)
    {
        if (refreshingLocalization)
            return;

        if (hasAppliedStats)
            ApplyStats(currentIsWin, currentReason);
        else
            RefreshLocalizedButtonLabels();
    }

    private void RefreshLocalizedButtonLabels()
    {
        ApplyLocalizedButtonLabels();
    }

    private void RefreshLocalizedStrings()
    {
        RefreshLocalizedString(winTitleLocalized);
        RefreshLocalizedString(loseTitleLocalized);
        RefreshLocalizedString(ordersFormatLocalized);
        RefreshLocalizedString(cluesFormatLocalized);
        RefreshLocalizedString(mistakesFormatLocalized);
        RefreshLocalizedString(timeFormatLocalized);
        RefreshLocalizedString(saveLabelLocalized);
        RefreshLocalizedString(nextCaseLabelLocalized);
        RefreshLocalizedString(retryLabelLocalized);
        RefreshLocalizedString(mainMenuLabelLocalized);
    }

    private void ApplyLocalizedButtonLabels()
    {
        SetButtonLabel(saveButton, saveLabelLocalized, saveLabel);
        SetButtonLabel(nextCaseButton, nextCaseLabelLocalized, nextCaseLabel);
        SetButtonLabel(retryButton, retryLabelLocalized, retryLabel);
        SetButtonLabel(mainMenuButton, mainMenuLabelLocalized, mainMenuLabel);
    }

    private void SetButtonLabel(Button button, LocalizedString localizedString, string fallback)
    {
        if (button == null)
            return;

        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = GetLocalizedValue(localizedString, fallback);
    }

    private static string GetLocalizedValue(LocalizedString localizedString, string fallback)
    {
        if (localizedString == null)
            return fallback;

        string localizedValue = localizedString.GetLocalizedString();
        return string.IsNullOrWhiteSpace(localizedValue) ? fallback : localizedValue;
    }

    private static void RefreshLocalizedString(LocalizedString localizedString)
    {
        if (localizedString != null)
            localizedString.RefreshString();
    }

    private void ApplyButtonStates(bool isWin)
    {
        // เซฟได้เฉพาะตอนชนะ และต้องมีจุดให้เล่นต่อจริง ๆ (ด่านสุดท้ายจะไม่มี)
        // เก็บผลไว้เลยตั้งแต่ตอนนี้ ตอนกดปุ่มค่อยส่งต่อ ไม่ต้องไปอ่านสภาพเกมซ้ำ
        // ซึ่งตอนนั้นอาจถูกอะไรแก้ไปแล้ว
        pendingSaveData = isWin ? SaveManager.CaptureStageCleared(gameManager, investigationManager) : null;

        if (saveButton != null) saveButton.gameObject.SetActive(pendingSaveData != null);

        if (nextCaseButton == null) return;

        // ไปด่านถัดไปได้เฉพาะตอนชนะ และด่านนี้ต้องตั้ง Next Scene Name ที่โหลดได้จริงไว้ด้วย
        bool canGoNext = isWin && !string.IsNullOrEmpty(ResolveNextSceneName());

        if (hideNextCaseWhenUnavailable)
        {
            nextCaseButton.gameObject.SetActive(canGoNext);
        }
        else
        {
            nextCaseButton.gameObject.SetActive(true);
            nextCaseButton.interactable = canGoNext;
        }
    }

    private int GetClueTarget(StageConfig stage)
    {
        if (stage == null) return 0;

        if (countCluesAgainstPool && stage.cluesPool != null && stage.cluesPool.Count > 0)
        {
            return stage.cluesPool.Count;
        }

        return Mathf.Max(0, stage.requiredCluesToPass);
    }

    private string ResolveNextSceneName()
    {
        return gameManager != null ? gameManager.ResolveNextSceneName(false) : string.Empty;
    }

    private IEnumerator PopRoutine()
    {
        if (animatedPanel == null)
        {
            RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
            animatedPanel = panelRect != null
                ? (panelRect.Find("Card") as RectTransform ?? panelRect)
                : null;
        }

        if (!hasPanelBase && animatedPanel != null)
        {
            // จำตำแหน่งจริงไว้ครั้งเดียวก่อนจะเลื่อนแผงลงไปด้านล่าง
            hasPanelBase = true;
            panelBasePosition = animatedPanel.anchoredPosition;

            panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null) panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
        }

        // ซ่อนบรรทัดสถิติไว้ก่อน แล้วค่อยทยอยโผล่ทีละบรรทัดหลังแผงเด้งขึ้นมาแล้ว
        SetStatRowsAlpha(0f);

        float duration = Mathf.Max(0.01f, popDuration);
        float time = 0f;

        if (animatedPanel != null)
            animatedPanel.anchoredPosition = panelBasePosition + Vector2.down * slideDistance;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 0f;

        while (time < duration)
        {
            // unscaledDeltaTime เพราะแผงนี้หยุดเวลาเกมไว้ตอนเปิดอยู่
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            if (animatedPanel != null)
                animatedPanel.anchoredPosition = Vector2.LerpUnclamped(
                    panelBasePosition + Vector2.down * slideDistance,
                    panelBasePosition,
                    EaseOutCubic(t));
            if (panelCanvasGroup != null) panelCanvasGroup.alpha = Mathf.Clamp01(t * 3f);

            yield return null;
        }

        if (animatedPanel != null)
            animatedPanel.anchoredPosition = panelBasePosition;
        if (panelCanvasGroup != null) panelCanvasGroup.alpha = 1f;

        yield return RevealStatRowsRoutine();
        yield return RevealRankImageRoutine();
    }

    private IEnumerator RevealStatRowsRoutine()
    {
        if (rowStagger <= 0f)
        {
            SetStatRowsAlpha(1f);
            PlayStatRevealSounds();
            yield break;
        }

        for (int i = 0; i < statRows.Length; i++)
        {
            TextMeshProUGUI row = statRows[i];
            if (row == null) continue;

            float time = 0f;
            float duration = Mathf.Max(0.01f, rowStagger);

            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                SetRowAlpha(row, Mathf.Clamp01(time / duration));
                yield return null;
            }

            SetRowAlpha(row, 1f);
            PlayStatRevealSound();
        }
    }

    private void PlayStatRevealSounds()
    {
        for (int i = 0; i < statRows.Length; i++)
        {
            if (statRows[i] != null)
                PlayStatRevealSound();
        }
    }

    private void PlayStatRevealSound()
    {
        if (SoundManager.Instance != null && !string.IsNullOrEmpty(statRevealSoundId))
            SoundManager.Instance.PlaySFX(statRevealSoundId);
    }

    private void SetStatRowsAlpha(float alpha)
    {
        for (int i = 0; i < statRows.Length; i++)
        {
            SetRowAlpha(statRows[i], alpha);
        }
    }

    private void SetRowAlpha(TextMeshProUGUI row, float alpha)
    {
        if (row == null) return;

        Color color = row.color;
        color.a = Mathf.Clamp01(alpha);
        row.color = color;
    }

    private IEnumerator RevealRankImageRoutine()
    {
        if (rankImage == null || !rankImage.gameObject.activeSelf || rankImage.sprite == null)
            yield break;

        if (rankRevealDelay > 0f)
            yield return new WaitForSecondsRealtime(rankRevealDelay);

        SoundManager.Instance.PlaySFX(SoundIds.RankExAppear);

        float duration = Mathf.Max(0.01f, rankRevealDuration);
        float time = 0f;
        Vector3 startScale = rankImageBaseScale * Mathf.Clamp01(rankRevealStartScale);

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);
            float easedT = EaseOutBack(t);

            if (rankImageCanvasGroup != null)
                rankImageCanvasGroup.alpha = t;
            rankImage.rectTransform.localScale = Vector3.LerpUnclamped(startScale, rankImageBaseScale, easedT);
            yield return null;
        }

        if (rankImageCanvasGroup != null)
            rankImageCanvasGroup.alpha = 1f;
        rankImage.rectTransform.localScale = rankImageBaseScale;
        rankImageRevealed = true;
    }

    // ---------- ปุ่ม ----------

    // กดบันทึกเกม: เปิดแผงให้ผู้เล่นเลือกว่าจะลงช่องไหน (แผงเลือกช่องจะเป็นคนเขียนไฟล์เอง)
    // ไม่ปิดแผงสรุปผล ผู้เล่นจะได้กด NEXT CASE ต่อได้เลยหลังเซฟเสร็จ
    public void OnClickSave()
    {
        if (pendingSaveData == null)
        {
            Debug.LogWarning("[StageResultPanel] ตอนนี้ไม่มีจุดให้เซฟไว้เล่นต่อ (ยังไม่ชนะ หรือด่านนี้ไม่มีด่านถัดไป)");
            return;
        }

        SaveSlotPanel.OpenSavePanel(pendingSaveData);
    }

    public void OnClickNextCase()
    {
        if (isLeavingResultScreen) return;

        string nextScene = ResolveNextSceneName();

        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogWarning("[StageResultPanel] ยังไม่ได้ตั้ง Next Scene Name ของด่านนี้ (หรือชื่อฉากโหลดไม่ได้) เลยไปด่านถัดไปไม่ได้");
            return;
        }

        StartCoroutine(LeaveResultScreenAndLoadScene(nextScene));
    }

    public void OnClickRetry()
    {
        if (isLeavingResultScreen) return;

        StartCoroutine(LeaveResultScreenAndLoadScene(SceneManager.GetActiveScene().name));
    }

    public void OnClickMainMenu()
    {
        if (isLeavingResultScreen) return;

        string sceneName = ResolveMainMenuSceneName();

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[StageResultPanel] ยังไม่ได้ตั้ง Main Menu Scene Name ไว้ เลยไม่รู้ว่าต้องกลับไปฉากไหน");
            return;
        }

        // เช็คตั้งแต่ตอนนี้เลย จะได้ไม่คืน timeScale ทิ้งไว้แล้วไปพังตอน LoadScene
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[StageResultPanel] โหลดฉาก '{sceneName}' ไม่ได้! เช็คชื่อฉากให้ตรง และเพิ่มฉากนี้ใน File > Build Settings ด้วย");
            return;
        }

        StartCoroutine(LeaveResultScreenAndLoadScene(sceneName));
    }

    private string ResolveMainMenuSceneName()
    {
        if (!string.IsNullOrWhiteSpace(mainMenuSceneName)) return mainMenuSceneName;

        // ฉากส่วนใหญ่ตั้งชื่อฉากเมนูหลักไว้ที่ UIManager อยู่แล้ว ไม่ต้องมาตั้งซ้ำอีกที่
        return UIManager.Instance != null ? UIManager.Instance.mainMenuSceneName : string.Empty;
    }

    // งานที่ต้องเก็บกวาดเหมือนกันทุกปุ่ม ก่อนจะออกจากแผงสรุปผลไปฉากใหม่
    private void LeaveResultScreen()
    {
        // กันฉากใหม่เปิดมาแล้วค้างนิ่งทั้งฉาก เพราะแผงนี้เป็นคนหยุดเวลาไว้เอง
        Time.timeScale = 1f;

        // SoundManager ข้ามฉากไปด้วย (DontDestroyOnLoad) เสียงที่ค้างอยู่เลยตามไปดังในฉากใหม่ได้
        if (SoundManager.Instance != null) SoundManager.Instance.StopAllSFX();

        if (showRoutine != null)
        {
            StopCoroutine(showRoutine);
            showRoutine = null;
        }

        if (panelRoot != null) panelRoot.SetActive(false);
        isShown = false;
    }

    private IEnumerator LeaveResultScreenAndLoadScene(string sceneName)
    {
        isLeavingResultScreen = true;
        LeaveResultScreen();

        Image fadeImage = EnsureTransitionFadeImage();
        if (fadeImage != null)
        {
            CanvasGroup fadeGroup = fadeImage.GetComponent<CanvasGroup>();
            if (fadeGroup == null) fadeGroup = fadeImage.gameObject.AddComponent<CanvasGroup>();

            fadeImage.gameObject.SetActive(true);
            fadeGroup.alpha = 0f;

            float duration = Mathf.Max(0.01f, transitionFadeDuration);
            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(time / duration);
                yield return null;
            }

            fadeGroup.alpha = 1f;
        }

        SceneManager.LoadScene(sceneName);
    }

    private Image EnsureTransitionFadeImage()
    {
        if (transitionFadeImage != null) return transitionFadeImage;

        Canvas canvas = panelRoot != null
            ? panelRoot.GetComponentInParent<Canvas>(true)
            : GetComponentInParent<Canvas>(true);

        if (canvas == null)
        {
            Debug.LogWarning("[StageResultPanel] ไม่พบ Canvas สำหรับแสดงเฟดก่อนเปลี่ยนฉาก");
            return null;
        }

        GameObject fadeObject = new GameObject("ResultTransitionBlackFade", typeof(RectTransform), typeof(Image), typeof(Canvas));
        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.SetParent(canvas.rootCanvas.transform, false);
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Canvas fadeCanvas = fadeObject.GetComponent<Canvas>();
        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder = canvas.rootCanvas.sortingOrder + 1;

        transitionFadeImage = fadeObject.GetComponent<Image>();
        transitionFadeImage.color = Color.black;
        transitionFadeImage.raycastTarget = false;
        transitionFadeImage.gameObject.SetActive(false);
        return transitionFadeImage;
    }

    private static float EaseOutCubic(float t)
    {
        return 1f - Mathf.Pow(1f - t, 3f);
    }

    private static float EaseOutBack(float t)
    {
        t = Mathf.Clamp01(t);
        const float overshoot = 1.70158f;
        float adjustedT = t - 1f;
        return 1f + (overshoot + 1f) * adjustedT * adjustedT * adjustedT
            + overshoot * adjustedT * adjustedT;
    }

    private void ResolveSources()
    {
        if (gameManager == null)
        {
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindObjectOfType<GameManager>();
        }

        if (investigationManager == null)
        {
            investigationManager = FindObjectOfType<InvestigationManager>();
        }
    }

    // อ่านด่านปัจจุบันแบบกันพัง เผื่อฉากไหนยังไม่ได้ตั้ง Stages ไว้ (เช่นฉากทดสอบ)
    private StageConfig GetCurrentStage()
    {
        if (gameManager == null || gameManager.stages == null) return null;

        int index = gameManager.currentStageIndex;
        if (index < 0 || index >= gameManager.stages.Count) return null;

        return gameManager.stages[index];
    }

    // ---------- สร้าง UI ให้เอง ----------

    private bool NeedsBuild()
    {
        return panelRoot == null;
    }

    private void BuildDefaultPanel()
    {
        RectTransform canvasRoot = EnsureCanvasRoot();
        if (canvasRoot == null) return;

        // แผงทั้งใบ = ตัวที่จะถูกเปิด/ปิด รวมทั้งภาพดำทับจอไว้ข้างในด้วย
        // จะได้ปิดทีเดียวหายทั้งชุด ไม่ต้องไล่ปิดทีละชิ้น
        GameObject rootObject = new GameObject("StageResultPanel", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        rootRect.SetParent(canvasRoot, false);
        Stretch(rootRect);
        panelRoot = rootObject;

        if (dimmerImage == null)
        {
            dimmerImage = CreateImage(rootRect, "Dimmer", new Color(0f, 0f, 0f, 0.72f), null);
            Stretch(dimmerImage.rectTransform);
            // กินคลิกไว้ ไม่ให้ผู้เล่นกดของในฉากทะลุแผงสรุปผลได้
            dimmerImage.raycastTarget = true;
        }

        RectTransform card = CreateCard(rootRect);

        if (titleText == null) titleText = CreateText(card, "Title", winTitle, Color.white, 58f, TextAlignmentOptions.Center, FontStyles.Bold);
        if (subtitleText == null) subtitleText = CreateText(card, "Subtitle", string.Empty, new Color(1f, 1f, 1f, 0.7f), 26f, TextAlignmentOptions.Center, FontStyles.Normal);

        CreateSpacer(card, 10f);

        if (rankImage == null)
        {
            rankImage = CreateImage(card, "RankImage", Color.white, null);
            rankImage.preserveAspect = true;

            LayoutElement rankLayout = rankImage.gameObject.AddComponent<LayoutElement>();
            rankLayout.preferredHeight = 140f;
        }

        if (ordersText == null) ordersText = CreateText(card, "OrdersRow", string.Empty, Color.white, 34f, TextAlignmentOptions.Left, FontStyles.Normal);
        if (cluesText == null) cluesText = CreateText(card, "CluesRow", string.Empty, Color.white, 34f, TextAlignmentOptions.Left, FontStyles.Normal);
        if (mistakesText == null) mistakesText = CreateText(card, "MistakesRow", string.Empty, Color.white, 34f, TextAlignmentOptions.Left, FontStyles.Normal);
        if (timeText == null) timeText = CreateText(card, "TimeRow", string.Empty, Color.white, 34f, TextAlignmentOptions.Left, FontStyles.Normal);

        CreateSpacer(card, 18f);

        RectTransform buttonRow = CreateButtonRow(card);

        if (saveButton == null) saveButton = CreateButton(buttonRow, "SaveButton", saveLabel, new Color(0.24f, 0.45f, 0.66f, 1f));
        if (nextCaseButton == null) nextCaseButton = CreateButton(buttonRow, "NextCaseButton", nextCaseLabel, new Color(0.24f, 0.62f, 0.36f, 1f));
        if (retryButton == null) retryButton = CreateButton(buttonRow, "RetryButton", retryLabel, new Color(0.28f, 0.32f, 0.42f, 1f));
        if (mainMenuButton == null) mainMenuButton = CreateButton(buttonRow, "MainMenuButton", mainMenuLabel, new Color(0.28f, 0.32f, 0.42f, 1f));

        // ใส่เสียงกด/เลื่อนผ่านให้ปุ่มทั้งแผงในทีเดียว (ตัวมันไล่ผูกให้เองตอน Start)
        if (rootObject.GetComponent<UIButtonSound>() == null) rootObject.AddComponent<UIButtonSound>();

        if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            Debug.LogWarning("[StageResultPanel] ฉากนี้ไม่มี EventSystem ปุ่มบนแผงสรุปผลจะกดไม่ได้ (เพิ่มได้จาก GameObject > UI > Event System)");
        }
    }

    private RectTransform EnsureCanvasRoot()
    {
        if (builtCanvasRoot != null) return builtCanvasRoot;

        // สร้าง Canvas ของตัวเองเสมอ ไม่ไปอาศัย Canvas ที่มีอยู่ในฉาก
        // เพราะแผงสรุปผลต้องอยู่บนสุด (เหนือสมุดหลักฐานและ HUD) ซึ่งคุมด้วย Sorting Order ตรงนี้ที่เดียวจบ
        GameObject canvasObject = new GameObject("StageResultPanel Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
        // กว้างพอให้ปุ่มสี่ใบ (SAVE / NEXT CASE / RETRY / MAIN MENU) เรียงกันได้โดยป้ายไม่ตกบรรทัด
        rect.sizeDelta = new Vector2(880f, 0f);

        VerticalLayoutGroup layout = background.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(56, 56, 44, 44);
        layout.spacing = 12f;
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

    private RectTransform CreateButtonRow(RectTransform parent)
    {
        GameObject rowObject = new GameObject("Buttons", typeof(RectTransform));
        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement element = rowObject.AddComponent<LayoutElement>();
        element.preferredHeight = 72f;

        return rect;
    }

    private Button CreateButton(RectTransform parent, string name, string label, Color color)
    {
        Image background = CreateImage(parent, name, color, ResolveBoxSprite());

        Button button = background.gameObject.AddComponent<Button>();
        button.targetGraphic = background;

        LayoutElement element = background.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 72f;
        element.preferredWidth = 170f;

        TextMeshProUGUI text = CreateText(background.rectTransform, "Label", label, Color.white, 26f, TextAlignmentOptions.Center, FontStyles.Bold);
        Stretch(text.rectTransform);

        // ป้ายบนปุ่มต้องไม่กินคลิก ไม่งั้นบางจังหวะคลิกจะไปโดนตัวอักษรแทนตัวปุ่ม
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
        text.enableWordWrapping = true;
        text.raycastTarget = false;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = fontSize * 1.6f;

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
