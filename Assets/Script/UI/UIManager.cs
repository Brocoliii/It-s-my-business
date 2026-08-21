using UnityEngine;
using UnityEngine.UI;
using TMPro; // �Ӥѭ: ��ͧ��������� TextMeshPro
using System.Collections;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    // ���� Singleton ����������������¡�� UI ������
    public static UIManager Instance { get; private set; }

    [Header("˹�Ҩ���ѡ (HUD)")]
    public TextMeshProUGUI clueCounterText; // ��ͤ����͡�ӹǹ�����
    public TextMeshProUGUI centerWarningText; // ���˹ѧ�����͹��ҧ�� (�� ���ҹѺ�����ѧ)
    public TextMeshProUGUI investigateTimerText; // shows remaining time for an investigate group

    [Header("�к� UI �ͺ�ѧ")]
    public Canvas listeningCanvas;
    public Image listeningFill;
    public TextMeshProUGUI investigateSequenceText;
    public TextMeshProUGUI investigateStatusText;

    [Header("Investigate Canvas Animation")]
    [Tooltip("ตัวกรอบแถบที่จะย่อ/ขยาย ถ้าไม่ใส่จะใช้ตัวแม่ของ listeningFill ให้เอง")]
    public RectTransform listeningBarRoot;
    [Tooltip("ถ้าไม่ใส่ จะไปหา/ใส่ CanvasGroup ให้เองบน listeningCanvas ตอนรันไทม์")]
    public CanvasGroup listeningCanvasGroup;
    public float listeningShowDuration = 0.25f;
    public float listeningHideDuration = 0.16f;
    [Tooltip("สเกลตอนซ่อน แถบจะเด้งจากค่านี้ขึ้นไป 1 แบบ ease-out back")]
    [Range(0f, 1f)] public float listeningHiddenScale = 0.85f;
    [Tooltip("ระยะที่แถบเลื่อนขึ้นมาตอนโผล่ (พิกเซล) ถ้าแถบอยู่ใน Layout Group ให้ตั้งเป็น 0 ไม่งั้นตำแหน่งจะโดน Layout เขียนทับ")]
    public float listeningSlideDistance = 36f;
    [Tooltip("ทางเลือก: ถ้าใส่ Animator ไว้ จะยิง Trigger \"Show\"/\"Hide\" แทนการขยับด้วยโค้ด")]
    public Animator listeningCanvasAnimator;

    [Header("Investigate Fill Animation")]
    [Tooltip("ความเร็วที่หลอดไล่ตามค่าจริง ยิ่งมากยิ่งตามติด")]
    public float fillFollowSpeed = 9f;
    [Tooltip("ไล่สีหลอดตามความคืบหน้า ปิดได้ถ้าอยากใช้สีที่ตั้งไว้ใน Inspector")]
    public bool tintFillByProgress = true;
    public Color fillLowColor = new Color(0.36f, 0.72f, 1f, 1f);
    public Color fillHighColor = new Color(0.4f, 1f, 0.55f, 1f);
    public Color fillGainFlashColor = Color.white;
    public float fillGainFlashDuration = 0.18f;
    [Tooltip("ความแรงที่แถบเด้งตอนต่อชุดปุ่มสำเร็จ")]
    public float fillGainPunchScale = 0.16f;
    public float fillGainPunchDuration = 0.24f;
    [Tooltip("ความแรงที่แถบเด้งตอนหลอดเต็ม (เก็บเบาะแสได้)")]
    public float fillCompletePunchScale = 0.42f;

    [Header("Investigate Success Animation")]
    [Tooltip("ทางเลือก: ถ้าใส่ Animator ไว้ จะยิง Trigger \"Success\" ตอนเก็บเบาะแสได้ แทนการเล่นแอนิเมชันฉลองจากโค้ดทั้งหมด")]
    public Animator investigateSuccessAnimator;
    [Tooltip("ความแรงที่ทั้งแถบเด้งโตตอนเก็บเบาะแสได้ (แรงกว่าตอนต่อชุดปุ่มสำเร็จ)")]
    public float successPopScale = 0.34f;
    [Tooltip("ระยะเวลาแอนิเมชันฉลอง ใช้เป็นเวลาที่ฝั่งสืบสวนค้าง UI ไว้ก่อนซ่อนแถบด้วย")]
    public float successPopDuration = 0.55f;
    [Tooltip("องศาที่แถบสะบัดตอนเด้งฉลอง")]
    public float successWobbleAngle = 5f;
    [Tooltip("เวลาที่หลอดค้างสีแฟลชตอนเก็บเบาะแสได้")]
    public float successFillFlashDuration = 0.3f;
    [Tooltip("เวลาหน่วงระหว่างไอคอนแต่ละตัวตอนไล่เด้งเป็นคลื่นฉลอง")]
    public float successIconWaveStagger = 0.045f;

    [Header("Investigate Success Text")]
    [Tooltip("ข้อความที่เด้งขึ้นตอนเก็บเบาะแสได้ ไม่ใส่ก็ได้")]
    public TextMeshProUGUI investigateSuccessText;
    public string investigateSuccessMessage = "ได้เบาะแสแล้ว!";
    public float successTextPopDuration = 0.3f;
    public float successTextHoldDuration = 0.45f;
    public float successTextFadeDuration = 0.25f;
    [Tooltip("ระยะที่ข้อความลอยขึ้นตอนจางหาย (พิกเซล)")]
    public float successTextRiseDistance = 26f;

    [Header("Investigate Success Flash")]
    [Tooltip("ภาพทับจอที่จะสว่างวาบตอนเก็บเบาะแสได้ ไม่ใส่ก็ได้")]
    public Image investigateSuccessFlash;
    public Color successFlashColor = new Color(1f, 1f, 1f, 0.5f);
    public float successFlashDuration = 0.35f;

    private float listeningVisibility = 0f;
    private bool isListeningBarVisible = false;
    private float listeningCanvasHideTimer = 0f;
    private Vector3 listeningBarBaseScale = Vector3.one;
    private Vector2 listeningBarBasePosition;
    private Quaternion listeningBarBaseRotation = Quaternion.identity;
    private bool hasListeningBarBase = false;
    private bool hasResolvedListeningBarRoot = false;
    private float listeningBarPunchTimer = 0f;
    private float listeningBarPunchDuration = 0.24f;
    private float listeningBarPunchAmount = 0f;

    private float listeningFillTarget = 0f;
    private float listeningFillDisplayed = 0f;
    private float listeningFillFlashTimer = 0f;
    private bool snapFillOnNextUpdate = true;
    private Color listeningFillBaseColor = Color.white;
    private bool hasListeningFillBaseColor = false;

    private float successPopTimer = 0f;
    private Coroutine successTextCoroutine;
    private Coroutine successFlashCoroutine;
    private Vector3 successTextBaseScale = Vector3.one;
    private Vector2 successTextBasePosition;
    private Color successTextBaseColor = Color.white;
    private bool hasSuccessTextBase = false;

    [Header("Investigate Sequence Icons")]
    public RectTransform investigateSequenceIconContainer;
    public Image investigateSequenceIconPrefab;
    public Sprite upIcon;
    public Sprite downIcon;
    public Sprite leftIcon;
    public Sprite rightIcon;
    public Color pendingIconColor = Color.white;
    public Color completedIconColor = Color.green;
    public Color flashCorrectColor = Color.green;
    public Color flashWrongColor = Color.red;

    [Header("Stratagem Icon Animation")]
    [Tooltip("ระยะเวลาแอนิเมชันไอคอนเด้งขึ้นตอนขึ้นชุดปุ่มใหม่")]
    public float sequenceIntroDuration = 0.26f;
    [Tooltip("เวลาหน่วงระหว่างไอคอนแต่ละตัวตอนเด้งขึ้น (ทยอยขึ้นทีละตัวแบบ Helldivers)")]
    public float sequenceIntroStagger = 0.05f;
    [Tooltip("องศาที่ไอคอนสะบัดตอนเด้งขึ้น")]
    public float sequenceIntroRotation = 12f;
    [Tooltip("ความแรงที่ไอคอนโตขึ้นตอนกดถูก")]
    public float correctPunchScale = 0.45f;
    public float correctPunchDuration = 0.22f;
    [Tooltip("องศาที่ไอคอนสะบัดตอนกดผิด")]
    public float wrongShakeAngle = 14f;
    [Tooltip("ความแรงที่ไอคอนบีบ/ยืดตอนกดผิด")]
    [Range(0f, 0.5f)] public float wrongShakeSquash = 0.18f;
    public float wrongShakeDuration = 0.45f;
    [Tooltip("ระยะที่ไอคอนทั้งแถวสะบัดซ้ายขวาตอนกดผิด (พิกเซล)")]
    public float wrongRowShakeDistance = 18f;
    [Tooltip("ความเร็ว/ความแรงที่ไอคอนตัวที่รอให้กดอยู่หายใจเบา ๆ")]
    public float idlePulseSpeed = 1.6f;
    [Range(0f, 0.5f)] public float idlePulseAmount = 0.08f;

    private readonly List<Image> sequenceIconPool = new List<Image>();
    private readonly List<StratagemIconAnimator> sequenceIconAnimators = new List<StratagemIconAnimator>();
    private StratagemIconAnimator fallbackSequenceAnimator;
    private Coroutine sequenceRowShakeCoroutine;
    private Vector2 sequenceRowShakeBasePosition;
    private bool hasSequenceRowShakeBase = false;

    [Header("Binoculars Overlay")]
    public GameObject binocularsOverlay;

    [Header("Background Dim (Investigate)")]
    public SpriteRenderer backgroundSprite;
    public Color backgroundNormalColor = Color.white;
    public Color backgroundDarkColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    public float backgroundFadeDuration = 0.3f;

    private Coroutine backgroundFadeCoroutine;

    [Header("˹�ҵ�ҧ��ͻ�ѻ")]
    public GameObject endOfDayPanel; // ˹�Ҩͷֺ� �����駢���ҵ͹���ѹ
    public NotebookManager notebookManager; // ���Դ˹�ҵ�ҧ����辽�����˹�Ҩͨ��ѹ

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // �Դ��ͤ�����͹ ���˹�Ҩͨ��ѹ����͹�͹�������
        if (centerWarningText != null) centerWarningText.gameObject.SetActive(false);
        if (endOfDayPanel != null) endOfDayPanel.SetActive(false);
        ShowListeningBar(false);
        ShowInvestigateSequence(false);
        ShowBinocularsOverlay(false);

        // บังคับให้แถบเข้าสู่สถานะซ่อนทันทีตั้งแต่เฟรมแรก ไม่ต้องรอ LateUpdate
        // ไม่งั้น Canvas จะยังเปิดค้างอยู่หนึ่งเฟรมแล้วแวบให้เห็นตอนเริ่มเกม
        ApplyListeningCanvasState();
        ApplyListeningFill();

        if (backgroundSprite != null) backgroundSprite.color = backgroundNormalColor;
    }

    public void SetBackgroundDark(bool isDark)
    {
        if (backgroundSprite == null) return;

        if (backgroundFadeCoroutine != null) StopCoroutine(backgroundFadeCoroutine);
        backgroundFadeCoroutine = StartCoroutine(FadeBackgroundRoutine(isDark ? backgroundDarkColor : backgroundNormalColor));
    }

    private IEnumerator FadeBackgroundRoutine(Color targetColor)
    {
        Color startColor = backgroundSprite.color;
        float time = 0f;

        while (time < backgroundFadeDuration)
        {
            time += Time.deltaTime;
            backgroundSprite.color = Color.Lerp(startColor, targetColor, time / backgroundFadeDuration);
            yield return null;
        }

        backgroundSprite.color = targetColor;
        backgroundFadeCoroutine = null;
    }

    // �ѧ��ѹ����Ѻ�ѻവ��ʹ�����
    public void UpdateClueText(int current, int total)
    {
        if (clueCounterText != null)
        {
            clueCounterText.text = $"�����: {current} / {total}";
        }
    }

    // �ѧ��ѹ�Ѻ�����ѧ��ҧ�� (���¡�ҡ GameManager)
    public Coroutine StartCountdownDisplay(int seconds)
    {
        return StartCoroutine(CountdownRoutine(seconds));
    }

    private IEnumerator CountdownRoutine(int seconds)
    {
        if (centerWarningText != null)
        {
            centerWarningText.gameObject.SetActive(true);

            for (int i = seconds; i > 0; i--)
            {
                centerWarningText.text = $"���ѹ�... {i}";
                yield return new WaitForSeconds(1f);
            }

            centerWarningText.text = "�������!";
            yield return new WaitForSeconds(1f);

            centerWarningText.gameObject.SetActive(false);
        }
        else
        {
            yield return new WaitForSeconds(seconds);
        }

        ShowEndOfDayPanel(true);
    }

    // ��˹�Ҩͨ��ѹ ��������˹�ҵ�ҧ����辽������¡ѹ
    public void ShowEndOfDayPanel(bool isVisible)
    {
        if (endOfDayPanel != null) endOfDayPanel.SetActive(isVisible);

        if (!isVisible) return;

        NotebookManager notebook = notebookManager;
        if (notebook == null)
        {
            notebook = FindObjectOfType<NotebookManager>(true);
        }

        if (notebook != null)
        {
            notebook.OpenClueWindow();
        }
        else
        {
            Debug.LogWarning("[UIManager] ��辽�����������˹�ҵ�ҧ����辽�͹��ѹ");
        }
    }

    public void ShowListeningBar(bool isVisible)
    {
        if (isVisible && !isListeningBarVisible)
        {
            // เริ่มฟังรอบใหม่: ให้หลอดกระโดดไปที่ค่าจริงในเฟรมแรกเลย
            // ไม่งั้นมันจะไล่ขึ้น/ลงจากค่าของกลุ่มก่อนหน้าที่ยังค้างอยู่ ทำให้เห็นหลอดวิ่งมั่ว ๆ ตอนเปิด
            snapFillOnNextUpdate = true;
        }

        isListeningBarVisible = isVisible;

        if (listeningCanvasAnimator != null)
        {
            // ทางเลือกให้อาร์ตทำแอนิเมชันเอง โค้ดแค่เปิด Canvas ให้ทันแล้วรอปิดตอนอนิเมชันปิดเล่นจบ
            if (isVisible && listeningCanvas != null) listeningCanvas.enabled = true;

            listeningCanvasAnimator.SetTrigger(isVisible ? "Show" : "Hide");
            listeningCanvasHideTimer = isVisible ? 0f : Mathf.Max(0.01f, listeningHideDuration);
        }

        if (!isVisible)
        {
            ShowInvestigateSequence(false);
            CancelInvestigateSuccess();
        }
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;
        UpdateListeningCanvasAnimation(dt);
        UpdateListeningFillAnimation(dt);
    }

    private void UpdateListeningCanvasAnimation(float dt)
    {
        if (listeningCanvasAnimator != null)
        {
            if (!isListeningBarVisible && listeningCanvasHideTimer > 0f)
            {
                listeningCanvasHideTimer -= dt;

                if (listeningCanvasHideTimer <= 0f && listeningCanvas != null)
                {
                    listeningCanvas.enabled = false;
                }
            }

            return;
        }

        float target = isListeningBarVisible ? 1f : 0f;
        float duration = isListeningBarVisible ? listeningShowDuration : listeningHideDuration;
        bool wasSettled = Mathf.Approximately(listeningVisibility, target);

        if (!wasSettled)
        {
            listeningVisibility = duration <= 0f
                ? target
                : Mathf.MoveTowards(listeningVisibility, target, dt / duration);
        }

        if (listeningBarPunchTimer > 0f)
        {
            listeningBarPunchTimer -= dt;
        }

        if (successPopTimer > 0f)
        {
            successPopTimer -= dt;
        }

        // ซ่อนสนิทแล้วและไม่มีแอนิเมชันค้าง ไม่ต้องเขียนทับ transform ทุกเฟรมอีก
        if (wasSettled && listeningVisibility <= 0f && listeningBarPunchTimer <= 0f && successPopTimer <= 0f)
        {
            return;
        }

        ApplyListeningCanvasState();
    }

    private void ApplyListeningCanvasState()
    {
        float v = Mathf.Clamp01(listeningVisibility);

        if (listeningCanvas != null)
        {
            // ปิด Canvas ก็ต่อเมื่อจางจนสุดแล้วเท่านั้น ไม่งั้นแอนิเมชันตอนซ่อนจะโดนตัดหายไปทั้งดุ้น
            listeningCanvas.enabled = v > 0f || isListeningBarVisible;
        }

        CanvasGroup group = ResolveListeningCanvasGroup();
        if (group != null)
        {
            group.alpha = v;
        }

        RectTransform root = ResolveListeningBarRoot();
        if (root == null) return;

        // ขาขึ้นใช้ ease-out back ให้เด้งเกินนิดนึงแล้วเข้าที่ ส่วนขาลงใช้ smoothstep เฉย ๆ
        // ถ้าใช้ back ตอนซ่อนด้วย แถบจะพองขึ้นก่อนหายไป ดูเหมือนบั๊กมากกว่าแอนิเมชัน
        float ease = isListeningBarVisible ? EaseOutBack(v) : v * v * (3f - 2f * v);
        float scaleMultiplier = Mathf.Lerp(listeningHiddenScale, 1f, ease);

        if (listeningBarPunchTimer > 0f && listeningBarPunchDuration > 0f)
        {
            float t = 1f - Mathf.Clamp01(listeningBarPunchTimer / listeningBarPunchDuration);
            scaleMultiplier *= 1f + listeningBarPunchAmount * Mathf.Sin(t * Mathf.PI);
        }

        // เด้งฉลองตอนเก็บเบาะแสได้ ต้องคูณทับบนค่าเดิมตรงนี้ ห้ามไปเขียน localScale เองจากคอรูทีน
        // ไม่งั้นจะโดน ApplyListeningCanvasState เขียนทับทุกเฟรมจนแอนิเมชันไม่ขึ้น
        float wobbleAngle = 0f;

        if (successPopTimer > 0f && successPopDuration > 0f)
        {
            float t = 1f - Mathf.Clamp01(successPopTimer / successPopDuration);
            float wave = Mathf.Sin(t * Mathf.PI);
            scaleMultiplier *= 1f + successPopScale * wave;
            wobbleAngle = successWobbleAngle * wave * Mathf.Sin(t * Mathf.PI * 3f);
        }

        root.localScale = listeningBarBaseScale * scaleMultiplier;
        root.localRotation = listeningBarBaseRotation * Quaternion.Euler(0f, 0f, wobbleAngle);

        if (listeningSlideDistance != 0f)
        {
            root.anchoredPosition = listeningBarBasePosition + new Vector2(0f, -listeningSlideDistance * (1f - ease));
        }
    }

    private RectTransform ResolveListeningBarRoot()
    {
        if (listeningBarRoot == null && listeningFill != null && !hasResolvedListeningBarRoot)
        {
            hasResolvedListeningBarRoot = true;
            // ใช้ตัวแม่ของหลอด (กรอบแถบ) ไม่ใช่ตัวหลอดเอง เพราะการย่อ/ขยาย Image ที่เป็น Filled
            // จะทำให้หลอดหลุดกรอบ
            listeningBarRoot = listeningFill.transform.parent as RectTransform;

            // ถ้าพ่อของหลอดคือตัว Canvas เอง ห้ามเอามาย่อ/ขยาย เพราะ Canvas ตัวนอกสุดถูก Unity คุม
            // ทั้ง scale และ position อยู่ ขยับไปก็โดนเขียนทับ (กรณีนี้จะเหลือแค่เอฟเฟกต์จาง ๆ)
            if (listeningCanvas != null && listeningBarRoot == listeningCanvas.transform)
            {
                listeningBarRoot = null;
            }
        }

        if (listeningBarRoot != null && !hasListeningBarBase)
        {
            // จำสเกล/ตำแหน่งฐานครั้งเดียวก่อนแอนิเมชันตัวแรกจะเริ่ม
            // ถ้าไปจำตอนกำลังเล่นอยู่ จะได้ค่าที่ถูกย่อไว้ (อาจเป็น 0) มาเป็นค่าฐาน
            listeningBarBaseScale = listeningBarRoot.localScale == Vector3.zero ? Vector3.one : listeningBarRoot.localScale;
            listeningBarBasePosition = listeningBarRoot.anchoredPosition;
            listeningBarBaseRotation = listeningBarRoot.localRotation;
            hasListeningBarBase = true;
        }

        return listeningBarRoot;
    }

    private CanvasGroup ResolveListeningCanvasGroup()
    {
        if (listeningCanvasGroup != null) return listeningCanvasGroup;
        if (listeningCanvas == null) return null;

        // ใส่ให้เองตอนรันไทม์ จะได้ไม่ต้องไปแก้ Prefab หรือผูกอะไรเพิ่มใน Inspector
        listeningCanvasGroup = listeningCanvas.GetComponent<CanvasGroup>();

        if (listeningCanvasGroup == null)
        {
            listeningCanvasGroup = listeningCanvas.gameObject.AddComponent<CanvasGroup>();
        }

        return listeningCanvasGroup;
    }

    private void PunchListeningBar(float amount, float duration)
    {
        if (amount <= 0f || duration <= 0f) return;

        listeningBarPunchAmount = amount;
        listeningBarPunchDuration = duration;
        listeningBarPunchTimer = duration;
    }

    private static float EaseOutBack(float t)
    {
        float overshoot = 1.70158f;
        float shifted = t - 1f;
        return 1f + (overshoot + 1f) * Mathf.Pow(shifted, 3f) + overshoot * Mathf.Pow(shifted, 2f);
    }

    // Investigate group timer display
    public void ShowInvestigateTimer(bool isVisible)
    {
        if (investigateTimerText != null)
        {
            investigateTimerText.gameObject.SetActive(isVisible);
        }
    }

    public void UpdateInvestigateTimer(float secondsRemaining)
    {
        if (investigateTimerText != null)
        {
            int sec = Mathf.CeilToInt(secondsRemaining);
            investigateTimerText.text = $"Disappears in: {sec}s";
        }
    }

    // ให้ฝั่งสืบสวนรอจนหลอดวิ่งไปเต็มจริงก่อนจะซ่อนแถบ
    // ถ้าไม่มี listeningFill ผูกไว้ ให้ถือว่าเต็มแล้ว ไม่งั้นฝั่งนั้นจะรอเปล่า ๆ จนครบ timeout
    public bool IsListeningFillFull
    {
        get
        {
            if (listeningFill == null) return true;
            return listeningFillTarget >= 0.999f && listeningFillDisplayed >= 0.999f;
        }
    }

    public void UpdateListeningBar(float progress)
    {
        float clamped = Mathf.Clamp01(progress);

        if (snapFillOnNextUpdate)
        {
            snapFillOnNextUpdate = false;
            listeningFillTarget = clamped;
            listeningFillDisplayed = clamped;
            ApplyListeningFill();
            return;
        }

        if (clamped > listeningFillTarget + 0.0001f)
        {
            // ความคืบหน้าเพิ่ม = ต่อชุดปุ่มสำเร็จ ให้แถบเด้ง + แฟลชสีเป็นการตอบสนอง
            // ชุดสุดท้าย (หลอดเต็ม) ปล่อยให้ไล่ขึ้นตามปกติเหมือนชุดอื่น ไม่ตัดไปเต็มทันที
            // ฝั่ง InvestigateGroup รอ IsListeningFillFull ก่อนจะซ่อนแถบอยู่แล้ว
            bool isComplete = clamped >= 0.999f;
            PunchListeningBar(isComplete ? fillCompletePunchScale : fillGainPunchScale, fillGainPunchDuration);
            listeningFillFlashTimer = fillGainFlashDuration;
        }
        else if (clamped < listeningFillTarget - 0.0001f)
        {
            // ค่าถอยหลัง (เช่นสลับไปกลุ่มใหม่) ไม่ต้องไล่ให้เห็น ตัดไปเลย
            listeningFillDisplayed = clamped;
        }

        listeningFillTarget = clamped;
    }

    private void UpdateListeningFillAnimation(float dt)
    {
        if (listeningFill == null) return;

        bool isChasing = !Mathf.Approximately(listeningFillDisplayed, listeningFillTarget);
        bool isFlashing = listeningFillFlashTimer > 0f;

        if (!isChasing && !isFlashing) return;

        if (isChasing)
        {
            // ไล่แบบ exponential เพื่อให้ความเร็วไม่ผูกกับเฟรมเรต
            listeningFillDisplayed = Mathf.Lerp(listeningFillDisplayed, listeningFillTarget, 1f - Mathf.Exp(-fillFollowSpeed * dt));

            if (Mathf.Abs(listeningFillTarget - listeningFillDisplayed) < 0.001f)
            {
                listeningFillDisplayed = listeningFillTarget;
            }
        }

        if (isFlashing)
        {
            listeningFillFlashTimer -= dt;
        }

        ApplyListeningFill();
    }

    private void ApplyListeningFill()
    {
        if (listeningFill == null) return;

        if (!hasListeningFillBaseColor)
        {
            // จำสีเดิมไว้ก่อนจะไปทับ เผื่อปิด tintFillByProgress และเพื่อคงค่าอัลฟ่าที่ตั้งไว้ใน Inspector
            listeningFillBaseColor = listeningFill.color;
            hasListeningFillBaseColor = true;
        }

        listeningFill.fillAmount = Mathf.Clamp01(listeningFillDisplayed);

        Color color = tintFillByProgress
            ? Color.Lerp(fillLowColor, fillHighColor, listeningFillDisplayed)
            : listeningFillBaseColor;

        if (listeningFillFlashTimer > 0f && fillGainFlashDuration > 0f)
        {
            // ไล่กลับจากสีแฟลชแทนที่จะตัดกลับทันที ไม่งั้นมันจะกระพริบแข็ง ๆ
            float t = Mathf.Clamp01(listeningFillFlashTimer / fillGainFlashDuration);
            color = Color.Lerp(color, fillGainFlashColor, t);
        }

        color.a = listeningFillBaseColor.a;
        listeningFill.color = color;
    }

    public void ShowInvestigateSequence(bool isVisible)
    {
        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(isVisible);
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.gameObject.SetActive(isVisible);
        }

        if (investigateSequenceIconContainer != null)
        {
            investigateSequenceIconContainer.gameObject.SetActive(isVisible);
        }

        if (!isVisible)
        {
            for (int i = 0; i < sequenceIconPool.Count; i++)
            {
                if (sequenceIconPool[i] != null)
                {
                    // คืนสเกล/มุม/สีก่อนปิด ไม่งั้นไอคอนจะถูกเก็บเข้าพูลทั้งที่ยังเบี้ยวอยู่
                    // แล้วรอบหน้าจะโผล่มาผิดรูปก่อนที่แอนิเมชันเด้งขึ้นจะเริ่มทำงาน
                    if (i < sequenceIconAnimators.Count && sequenceIconAnimators[i] != null)
                    {
                        sequenceIconAnimators[i].ResetState();
                    }

                    sequenceIconPool[i].gameObject.SetActive(false);
                }
            }

            StopSequenceRowShake();
        }
    }

    public void UpdateInvestigateSequence(List<StratagemDirection> sequence, int currentIndex, string status, int flashIndex = -1, Color? flashColor = null)
    {
        if (sequence == null)
        {
            return;
        }

        if (HasSequenceSprites())
        {
            UpdateInvestigateSequenceIcons(sequence, currentIndex, flashIndex, flashColor);
        }
        else if (investigateSequenceText != null)
        {
            investigateSequenceText.text = BuildFallbackSequenceText(sequence);
            investigateSequenceText.gameObject.SetActive(true);
        }

        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(!HasSequenceSprites());
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.text = status;
        }
    }

    public void UpdateInvestigateSequence(string sequence, string status)
    {
        if (investigateSequenceText != null)
        {
            investigateSequenceText.text = sequence;
            investigateSequenceText.gameObject.SetActive(true);
        }

        if (investigateStatusText != null)
        {
            investigateStatusText.text = status;
        }

        if (investigateSequenceIconContainer != null)
        {
            investigateSequenceIconContainer.gameObject.SetActive(false);
        }
    }

    private void UpdateInvestigateSequenceIcons(List<StratagemDirection> sequence, int currentIndex, int flashIndex, Color? flashColor)
    {
        RectTransform container = GetSequenceIconContainer();
        if (container == null)
        {
            return;
        }

        EnsureSequenceIconCount(container, sequence.Count);

        for (int i = 0; i < sequence.Count; i++)
        {
            Image icon = sequenceIconPool[i];
            if (icon == null)
            {
                continue;
            }

            icon.gameObject.SetActive(true);
            icon.sprite = GetSpriteForDirection(sequence[i]);
            icon.preserveAspect = true;

            Color resolvedColor = ResolveSequenceIconColor(i, currentIndex, flashIndex, flashColor);
            StratagemIconAnimator animator = GetSequenceIconAnimator(i);

            if (animator != null)
            {
                // ปล่อยให้ animator เป็นเจ้าของสีไอคอนคนเดียว ไม่งั้นสีที่เซ็ตตรงนี้ทุกเฟรม
                // จะไปทับสีที่กำลังไล่/กำลังแฟลชอยู่จนแอนิเมชันสีไม่ขึ้น
                bool isFlashing = flashColor.HasValue && i == flashIndex;
                animator.SetColor(resolvedColor, isFlashing);
                animator.SetCurrentStep(i == currentIndex);
            }
            else
            {
                icon.color = resolvedColor;
            }
        }

        for (int i = sequence.Count; i < sequenceIconPool.Count; i++)
        {
            if (sequenceIconPool[i] != null)
            {
                // คืนสภาพไอคอนส่วนเกินก่อนปิด เผื่อมันค้างอยู่กลางแอนิเมชันตอนชุดปุ่มสั้นลง
                StratagemIconAnimator surplusAnimator = GetSequenceIconAnimator(i);
                if (surplusAnimator != null)
                {
                    surplusAnimator.ResetState();
                }

                sequenceIconPool[i].gameObject.SetActive(false);
            }
        }

        if (investigateSequenceText != null)
        {
            investigateSequenceText.gameObject.SetActive(false);
        }
    }

    private void EnsureSequenceIconCount(RectTransform container, int desiredCount)
    {
        while (sequenceIconPool.Count < desiredCount)
        {
            Image icon = CreateSequenceIcon(container);
            sequenceIconPool.Add(icon);
            sequenceIconAnimators.Add(AttachIconAnimator(icon));
        }
    }

    private Image CreateSequenceIcon(RectTransform container)
    {
        Image icon;

        if (investigateSequenceIconPrefab != null)
        {
            icon = Instantiate(investigateSequenceIconPrefab, container);
        }
        else
        {
            GameObject iconObject = new GameObject("StratagemIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            iconObject.transform.SetParent(container, false);
            icon = iconObject.GetComponent<Image>();
        }

        return icon;
    }

    // ใส่ตัวเล่นแอนิเมชันให้ไอคอนตอนสร้าง เพราะไอคอนถูกสร้างตอนรันไทม์
    // ทำแบบนี้จะได้ไม่ต้องไปแก้ Prefab หรือผูกอะไรเพิ่มใน Inspector
    private StratagemIconAnimator AttachIconAnimator(Image icon)
    {
        if (icon == null)
        {
            return null;
        }

        StratagemIconAnimator animator = icon.GetComponent<StratagemIconAnimator>();

        if (animator == null)
        {
            animator = icon.gameObject.AddComponent<StratagemIconAnimator>();
        }

        animator.Bind(icon);
        animator.SetPulse(idlePulseSpeed, idlePulseAmount);
        return animator;
    }

    private StratagemIconAnimator GetSequenceIconAnimator(int index)
    {
        if (index < 0 || index >= sequenceIconAnimators.Count)
        {
            return null;
        }

        return sequenceIconAnimators[index];
    }

    // ตอนไม่ได้ใส่สไปรต์ทิศทางไว้ HUD จะตกไปใช้ข้อความ "W > A > S > D" แทน
    // ข้อความก็เป็น Graphic เหมือนกัน เลยใช้ตัวเล่นแอนิเมชันตัวเดียวกันได้เลย
    private StratagemIconAnimator GetFallbackSequenceAnimator()
    {
        if (fallbackSequenceAnimator != null)
        {
            return fallbackSequenceAnimator;
        }

        if (investigateSequenceText == null)
        {
            return null;
        }

        fallbackSequenceAnimator = investigateSequenceText.GetComponent<StratagemIconAnimator>();

        if (fallbackSequenceAnimator == null)
        {
            fallbackSequenceAnimator = investigateSequenceText.gameObject.AddComponent<StratagemIconAnimator>();
        }

        fallbackSequenceAnimator.Bind(investigateSequenceText);
        fallbackSequenceAnimator.SetPulse(idlePulseSpeed, idlePulseAmount * 0.5f);
        return fallbackSequenceAnimator;
    }

    private RectTransform GetSequenceIconContainer()
    {
        if (investigateSequenceIconContainer != null)
        {
            return investigateSequenceIconContainer;
        }

        if (investigateSequenceText != null)
        {
            return investigateSequenceText.transform.parent as RectTransform;
        }

        return null;
    }

    private bool HasSequenceSprites()
    {
        return upIcon != null && downIcon != null && leftIcon != null && rightIcon != null;
    }

    private Sprite GetSpriteForDirection(StratagemDirection direction)
    {
        switch (direction)
        {
            case StratagemDirection.Up:
                return upIcon;
            case StratagemDirection.Down:
                return downIcon;
            case StratagemDirection.Left:
                return leftIcon;
            case StratagemDirection.Right:
                return rightIcon;
            default:
                return null;
        }
    }

    private Color ResolveSequenceIconColor(int iconIndex, int currentIndex, int flashIndex, Color? flashColor)
    {
        if (flashColor.HasValue && iconIndex == flashIndex)
        {
            return flashColor.Value;
        }

        if (iconIndex < currentIndex)
        {
            return completedIconColor;
        }

        return pendingIconColor;
    }

    private string BuildFallbackSequenceText(List<StratagemDirection> sequence)
    {
        string result = string.Empty;

        for (int i = 0; i < sequence.Count; i++)
        {
            result += DirectionToKey(sequence[i]);

            if (i < sequence.Count - 1)
            {
                result += "  >  ";
            }
        }

        return result;
    }

    private string DirectionToKey(StratagemDirection direction)
    {
        switch (direction)
        {
            case StratagemDirection.Up:
                return "W";
            case StratagemDirection.Down:
                return "S";
            case StratagemDirection.Left:
                return "A";
            case StratagemDirection.Right:
                return "D";
            default:
                return "?";
        }
    }

    // เรียกหลังสุ่มชุดปุ่มใหม่ ให้ไอคอนทยอยเด้งขึ้นทีละตัวแทนที่จะโผล่มาเฉย ๆ
    // ต้องเรียก "หลัง" UpdateInvestigateSequence เสมอ เพราะไอคอนเพิ่งถูกสร้าง/เปิดใช้งานตรงนั้น
    public void PlayStratagemIntro()
    {
        if (!HasSequenceSprites())
        {
            StratagemIconAnimator fallback = GetFallbackSequenceAnimator();
            if (fallback != null)
            {
                fallback.PlayAppear(0f, sequenceIntroDuration, 0f);
            }
            return;
        }

        int shownCount = 0;

        for (int i = 0; i < sequenceIconAnimators.Count; i++)
        {
            StratagemIconAnimator animator = sequenceIconAnimators[i];

            if (animator == null || !animator.gameObject.activeSelf)
                continue;

            animator.PlayAppear(shownCount * sequenceIntroStagger, sequenceIntroDuration, sequenceIntroRotation);
            shownCount++;
        }
    }

    // กดถูก: ไอคอนตัวที่เพิ่งกดผ่านจะเด้งโตแล้วยุบกลับ
    public void PlayStratagemCorrect(int index)
    {
        if (!HasSequenceSprites())
        {
            StratagemIconAnimator fallback = GetFallbackSequenceAnimator();
            if (fallback != null)
            {
                fallback.PlayPunch(correctPunchScale * 0.4f, correctPunchDuration, 0f, 0f);
                fallback.FlashColor(flashCorrectColor, correctPunchDuration);
            }
            return;
        }

        StratagemIconAnimator animator = GetSequenceIconAnimator(index);

        if (animator != null)
        {
            animator.PlayPunch(correctPunchScale, correctPunchDuration, 0f, sequenceIntroRotation * 0.6f);
        }
    }

    // กดผิด: ไอคอนตัวที่พลาดสะบัด และแถวทั้งแถวสั่นซ้ายขวา
    public void PlayStratagemWrong(int index)
    {
        if (!HasSequenceSprites())
        {
            StratagemIconAnimator fallback = GetFallbackSequenceAnimator();
            if (fallback != null)
            {
                fallback.PlayShake(wrongShakeAngle * 0.5f, wrongShakeSquash * 0.5f, wrongShakeDuration);
                fallback.FlashColor(flashWrongColor, wrongShakeDuration);
            }
            ShakeSequenceRow(wrongRowShakeDistance, wrongShakeDuration);
            return;
        }

        StratagemIconAnimator animator = GetSequenceIconAnimator(index);

        if (animator != null)
        {
            animator.PlayShake(wrongShakeAngle, wrongShakeSquash, wrongShakeDuration);
        }

        ShakeSequenceRow(wrongRowShakeDistance, wrongShakeDuration);
    }

    // ต่อชุดปุ่มครบ: ไล่เด้งไอคอนเป็นคลื่นจากซ้ายไปขวาก่อนชุดใหม่จะขึ้น
    // skipIndex คือไอคอนตัวที่เพิ่งกดถูก ต้องข้ามไว้ ไม่งั้นการเด้งทันทีของมัน
    // จะโดนคลื่น (ซึ่งมีดีเลย์) เขียนทับ กลายเป็นกดแล้วเงียบไปแป๊บนึงก่อนค่อยเด้ง
    public void PlayStratagemSequenceCleared(int skipIndex = -1)
    {
        if (!HasSequenceSprites())
        {
            StratagemIconAnimator fallback = GetFallbackSequenceAnimator();
            if (fallback != null)
            {
                fallback.PlayPunch(correctPunchScale * 0.6f, correctPunchDuration, 0f, 0f);
                fallback.FlashColor(flashCorrectColor, correctPunchDuration);
            }
            return;
        }

        int shownCount = 0;

        for (int i = 0; i < sequenceIconAnimators.Count; i++)
        {
            StratagemIconAnimator animator = sequenceIconAnimators[i];

            if (animator == null || !animator.gameObject.activeSelf)
                continue;

            if (i != skipIndex)
            {
                // ใช้เวลาสั้นกว่าการเด้งปกติ เพราะคลื่นนี้ต้องวิ่งจบก่อนชุดปุ่มใหม่จะเด้งขึ้นมาแทน
                animator.PlayPunch(correctPunchScale * 0.7f, correctPunchDuration * 0.7f, shownCount * sequenceIntroStagger * 0.6f, 0f);
            }

            shownCount++;
        }
    }

    // สะบัดทั้งแถวที่ตัว container ไม่ใช่ที่ไอคอนรายตัว
    // เพราะถ้า container เป็น Layout Group ตำแหน่งของไอคอนจะโดน Layout เขียนทับตอน rebuild
    private void ShakeSequenceRow(float distance, float duration)
    {
        RectTransform container = GetSequenceIconContainer();

        if (container == null || duration <= 0f || distance <= 0f)
            return;

        // คืนตำแหน่งเดิมก่อนเริ่มรอบใหม่ ไม่งั้นตำแหน่งฐานจะเพี้ยนสะสมเมื่อกดผิดรัว ๆ
        StopSequenceRowShake();

        sequenceRowShakeBasePosition = container.anchoredPosition;
        hasSequenceRowShakeBase = true;
        sequenceRowShakeCoroutine = StartCoroutine(ShakeSequenceRowRoutine(container, distance, duration));
    }

    private void StopSequenceRowShake()
    {
        if (sequenceRowShakeCoroutine != null)
        {
            StopCoroutine(sequenceRowShakeCoroutine);
            sequenceRowShakeCoroutine = null;
        }

        if (hasSequenceRowShakeBase)
        {
            RectTransform container = GetSequenceIconContainer();

            if (container != null)
            {
                container.anchoredPosition = sequenceRowShakeBasePosition;
            }

            hasSequenceRowShakeBase = false;
        }
    }

    private IEnumerator ShakeSequenceRowRoutine(RectTransform container, float distance, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float damping = 1f - t;
            float offset = Mathf.Sin(t * Mathf.PI * 8f) * distance * damping;
            container.anchoredPosition = sequenceRowShakeBasePosition + new Vector2(offset, 0f);
            yield return null;
        }

        container.anchoredPosition = sequenceRowShakeBasePosition;
        hasSequenceRowShakeBase = false;
        sequenceRowShakeCoroutine = null;
    }

    // เวลารวมของแอนิเมชันฉลอง ให้ฝั่งสืบสวนค้าง UI ไว้จนเล่นจบก่อนค่อยซ่อนแถบ
    // ไม่งั้นแถบจะถูกซ่อนตัดกลางแอนิเมชันเมื่อ clueCompleteHoldDuration สั้นกว่า
    public float InvestigateSuccessDuration
    {
        get
        {
            if (investigateSuccessAnimator != null)
            {
                return successPopDuration;
            }

            float total = successPopDuration;

            if (investigateSuccessText != null)
            {
                total = Mathf.Max(total, successTextPopDuration + successTextHoldDuration + successTextFadeDuration);
            }

            if (investigateSuccessFlash != null)
            {
                total = Mathf.Max(total, successFlashDuration);
            }

            return total;
        }
    }

    // ฉลองตอนเก็บเบาะแสได้: เด้งทั้งแถบ + แฟลชหลอด + ไล่คลื่นไอคอน + ข้อความเด้งขึ้น + แฟลชจอ
    // ต้องเรียก "หลัง" หลอดวิ่งไปเต็มแล้วเท่านั้น ไม่งั้นจะไปฉลองตั้งแต่หลอดยังวิ่งไม่ถึงปลาย
    public void PlayInvestigateSuccess()
    {
        if (investigateSuccessAnimator != null)
        {
            // ทางเลือกให้อาร์ตทำแอนิเมชันฉลองเองทั้งชุด โค้ดไม่ต้องไปแตะอะไรต่อ
            investigateSuccessAnimator.SetTrigger("Success");
            return;
        }

        listeningFillFlashTimer = Mathf.Max(listeningFillFlashTimer, successFillFlashDuration);

        // ห้ามเด้งตัวแถบเองถ้ามี listeningCanvasAnimator ผูกไว้ เพราะตอนนั้น Animator เป็นเจ้าของ
        // ทั้งสเกล/อัลฟ่าของแถบ และ listeningVisibility ค้างอยู่ที่ 0 ตลอด
        // ถ้าไปเรียก ApplyListeningCanvasState แถบจะโดนย่อลงไปเป็นสถานะซ่อนกลางคัน
        if (listeningCanvasAnimator == null)
        {
            successPopTimer = successPopDuration;

            // ดันสถานะเข้าเฟรมนี้เลย ไม่ต้องรอ LateUpdate จะได้ไม่มีเฟรมที่ค้างสภาพเดิม
            ApplyListeningCanvasState();
        }

        ApplyListeningFill();

        PlayStratagemSuccessWave();

        if (investigateSuccessText != null)
        {
            if (successTextCoroutine != null) StopCoroutine(successTextCoroutine);
            successTextCoroutine = StartCoroutine(SuccessTextRoutine());
        }

        if (investigateSuccessFlash != null)
        {
            if (successFlashCoroutine != null) StopCoroutine(successFlashCoroutine);
            successFlashCoroutine = StartCoroutine(SuccessFlashRoutine());
        }
    }

    // ไล่เด้งไอคอนทั้งแถวเป็นคลื่นพร้อมย้อมสีเขียว แรงกว่าคลื่นตอนต่อชุดปุ่มครบ
    private void PlayStratagemSuccessWave()
    {
        float flashDuration = successPopDuration * 0.6f;

        if (!HasSequenceSprites())
        {
            StratagemIconAnimator fallback = GetFallbackSequenceAnimator();
            if (fallback != null)
            {
                fallback.PlayPunch(correctPunchScale, correctPunchDuration, 0f, 0f);
                fallback.FlashColor(flashCorrectColor, flashDuration);
            }
            return;
        }

        int shownCount = 0;

        for (int i = 0; i < sequenceIconAnimators.Count; i++)
        {
            StratagemIconAnimator animator = sequenceIconAnimators[i];

            if (animator == null || !animator.gameObject.activeSelf)
                continue;

            animator.PlayPunch(correctPunchScale, correctPunchDuration, shownCount * successIconWaveStagger, sequenceIntroRotation);
            animator.FlashColor(flashCorrectColor, flashDuration);
            shownCount++;
        }
    }

    // ตัดแอนิเมชันฉลองทิ้งตอนแถบถูกซ่อน (เช่นผู้เล่นปล่อยเมาส์ก่อนฉลองจบ)
    // ถ้าไม่ตัด ข้อความ/แฟลชจะค้างเล่นต่อบน Canvas ที่ปิดไปแล้ว แล้วไปโผล่ตอนกลุ่มถัดไปเปิดแถบขึ้นมา
    private void CancelInvestigateSuccess()
    {
        successPopTimer = 0f;

        if (successTextCoroutine != null)
        {
            StopCoroutine(successTextCoroutine);
            successTextCoroutine = null;
        }

        if (successFlashCoroutine != null)
        {
            StopCoroutine(successFlashCoroutine);
            successFlashCoroutine = null;
        }

        ResetSuccessText();

        if (investigateSuccessFlash != null)
        {
            investigateSuccessFlash.gameObject.SetActive(false);
        }
    }

    private IEnumerator SuccessTextRoutine()
    {
        RectTransform rect = investigateSuccessText.rectTransform;

        if (!hasSuccessTextBase)
        {
            // จำสเกล/ตำแหน่ง/สีฐานครั้งเดียวก่อนแอนิเมชันตัวแรกจะเริ่ม เหมือนที่ทำกับตัวแถบ
            successTextBaseScale = rect.localScale == Vector3.zero ? Vector3.one : rect.localScale;
            successTextBasePosition = rect.anchoredPosition;
            successTextBaseColor = investigateSuccessText.color;
            hasSuccessTextBase = true;
        }

        if (!string.IsNullOrEmpty(investigateSuccessMessage))
        {
            investigateSuccessText.text = investigateSuccessMessage;
        }

        rect.anchoredPosition = successTextBasePosition;
        rect.localScale = Vector3.zero;
        SetSuccessTextAlpha(0f);
        investigateSuccessText.gameObject.SetActive(true);

        float time = 0f;
        float popDuration = Mathf.Max(0.01f, successTextPopDuration);
        while (time < popDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / popDuration);
            rect.localScale = successTextBaseScale * EaseOutBack(t);
            SetSuccessTextAlpha(Mathf.Clamp01(t * 3f));
            yield return null;
        }

        rect.localScale = successTextBaseScale;
        SetSuccessTextAlpha(1f);

        if (successTextHoldDuration > 0f)
        {
            yield return new WaitForSeconds(successTextHoldDuration);
        }

        time = 0f;
        float fadeDuration = Mathf.Max(0.01f, successTextFadeDuration);
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / fadeDuration);
            t = t * t * (3f - 2f * t);
            SetSuccessTextAlpha(1f - t);
            rect.anchoredPosition = successTextBasePosition + new Vector2(0f, successTextRiseDistance * t);
            yield return null;
        }

        successTextCoroutine = null;
        ResetSuccessText();
    }

    private void ResetSuccessText()
    {
        if (investigateSuccessText == null || !hasSuccessTextBase)
        {
            if (investigateSuccessText != null)
            {
                investigateSuccessText.gameObject.SetActive(false);
            }
            return;
        }

        // คืนสภาพก่อนปิด ไม่งั้นรอบหน้าข้อความจะโผล่มาจาง ๆ ลอยค้างอยู่ตำแหน่งเดิม
        investigateSuccessText.gameObject.SetActive(false);
        investigateSuccessText.rectTransform.anchoredPosition = successTextBasePosition;
        investigateSuccessText.rectTransform.localScale = successTextBaseScale;
        investigateSuccessText.color = successTextBaseColor;
    }

    private void SetSuccessTextAlpha(float alpha)
    {
        if (investigateSuccessText == null) return;

        Color c = successTextBaseColor;
        c.a = successTextBaseColor.a * Mathf.Clamp01(alpha);
        investigateSuccessText.color = c;
    }

    private IEnumerator SuccessFlashRoutine()
    {
        investigateSuccessFlash.gameObject.SetActive(true);

        float time = 0f;
        float duration = Mathf.Max(0.01f, successFlashDuration);

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            t = t * t * (3f - 2f * t);

            // สว่างสุดทันทีแล้วค่อยจางลง ถ้าไล่ขึ้นก่อนจะดูเป็นฉากค่อย ๆ สว่าง ไม่ใช่แฟลช
            Color c = successFlashColor;
            c.a = successFlashColor.a * (1f - t);
            investigateSuccessFlash.color = c;
            yield return null;
        }

        investigateSuccessFlash.gameObject.SetActive(false);
        successFlashCoroutine = null;
    }

    public void ShowBinocularsOverlay(bool isVisible)
    {
        if (binocularsOverlay != null)
        {
            binocularsOverlay.SetActive(isVisible);
        }
    }
}
