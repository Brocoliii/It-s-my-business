using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// HUD ที่โชว์เงื่อนไขแพ้-ชนะของด่านให้ผู้เล่นเห็นตลอดเวลา
// - เบาะแสที่เก็บได้ เทียบกับ Required Clues To Pass ของ StageConfig
// - ความผิดพลาดที่ทำไปแล้ว เทียบกับ Max Mistakes Allowed ของ StageConfig
//
// อ่านค่าจาก GameManager กับ InvestigationManager เองทุกเฟรม เลยไม่ต้องไปแก้โค้ดฝั่งไหนให้มาเรียก
// (ครอบทุกทางที่ค่าเปลี่ยน รวมปุ่มใน DebugMenu ด้วย)
// ไม่ผูกอะไรใน Inspector เลยก็ใช้ได้ ตัวสคริปต์จะสร้าง HUD ให้เองตอนเริ่มฉาก
[DisallowMultipleComponent]
public class StageObjectiveHUD : MonoBehaviour
{
    [Header("แหล่งข้อมูล")]
    [Tooltip("ปล่อยว่างได้ จะไปหา GameManager ในฉากให้เอง")]
    public GameManager gameManager;
    [Tooltip("ปล่อยว่างได้ จะไปหา InvestigationManager ในฉากให้เอง\nถ้าฉากไม่มีตัวนี้ จำนวนเบาะแสจะโชว์เป็น 0 เฉย ๆ ไม่พัง")]
    public InvestigationManager investigationManager;

    [Header("สร้าง UI อัตโนมัติ")]
    [Tooltip("ติ๊กถูก = ช่องไหนไม่ได้ผูกไว้ จะสร้างให้เองตอนเริ่มฉาก (ลากสคริปต์ใส่ GameObject เปล่า ๆ ก็ใช้ได้เลย)")]
    public bool buildUiIfMissing = true;
    [Tooltip("ฟอนต์ที่ใช้ตอนสร้างข้อความให้เอง ถ้าไม่ใส่จะใช้ฟอนต์ปริยายของ TextMeshPro ซึ่งมักไม่มีตัวอักษรไทย")]
    public TMP_FontAsset hudFont;
    [Tooltip("ระยะห่างจากมุมซ้ายบนของจอ (พิกเซล) ใช้ตอนสร้าง HUD ให้เองเท่านั้น")]
    public Vector2 hudOffset = new Vector2(28f, -24f);
    [Tooltip("ขนาดตัวอักษรตอนสร้าง HUD ให้เอง")]
    public float hudFontSize = 26f;

    [Header("ข้อความ")]
    public TextMeshProUGUI clueLabelText;
    public TextMeshProUGUI clueCounterText;
    public TextMeshProUGUI mistakeLabelText;
    public TextMeshProUGUI mistakeCounterText;
    public string clueLabel = "เบาะแส";
    public string mistakeLabel = "ความผิดพลาด";
    [Tooltip("{0} = ค่าปัจจุบัน, {1} = โควตาของด่าน")]
    public string counterFormat = "{0} / {1}";

    [Header("เม็ดไอคอน (Pips)")]
    [Tooltip("ติ๊กออก = โชว์แค่ตัวเลข ไม่ต้องมีเม็ดไอคอน")]
    public bool usePips = true;
    public RectTransform cluePipContainer;
    public RectTransform mistakePipContainer;
    [Tooltip("ทางเลือก: Prefab ของเม็ดไอคอน ถ้าไม่ใส่จะสร้าง Image กลม ๆ ให้เอง")]
    public Image pipPrefab;
    [Tooltip("ทางเลือก: สไปรต์ของเม็ดไอคอนตอนสร้างเอง ถ้าไม่ใส่จะใช้สไปรต์วงกลมของ Unity")]
    public Sprite pipSprite;
    public float pipSize = 24f;
    public float pipSpacing = 8f;
    [Tooltip("ติ๊กถูก = เม็ดความผิดพลาดทำตัวเหมือนหัวใจ (เต็มอยู่ก่อน แล้วหายไปทีละเม็ดตอนทำพลาด)\nติ๊กออก = เม็ดจะไล่ติดขึ้นทีละเม็ดตามจำนวนครั้งที่พลาด")]
    public bool showMistakesAsLives = true;

    [Header("สีของเม็ดไอคอน")]
    public Color cluePipEmptyColor = new Color(1f, 1f, 1f, 0.22f);
    public Color cluePipFilledColor = new Color(1f, 0.85f, 0.3f, 1f);
    public Color mistakePipEmptyColor = new Color(1f, 1f, 1f, 0.22f);
    public Color mistakePipFilledColor = new Color(1f, 0.35f, 0.32f, 1f);

    [Header("สีของข้อความ")]
    public Color labelColor = new Color(1f, 1f, 1f, 0.75f);
    public Color counterColor = Color.white;
    [Tooltip("สีตัวเลขเบาะแสตอนเก็บครบโควตาแล้ว")]
    public Color clueCompleteColor = new Color(0.45f, 1f, 0.55f, 1f);
    [Tooltip("สีตัวเลขความผิดพลาดตอนใช้โควตาหมดแล้ว (พลาดอีกครั้งเดียวแพ้)")]
    public Color dangerColor = new Color(1f, 0.35f, 0.32f, 1f);

    [Header("Bonus clues (over quota)")]
    [Tooltip("Shown once clues exceed the quota. Index 0 = x1, 1 = x2 ... 4 = x5. More extra clues than sprites reuses the last sprite.")]
    public Sprite[] bonusSprites;
    public Image bonusImage;
    public TextMeshProUGUI bonusText;
    [Tooltip("{0} = number of extra clues")]
    public string bonusFormat = "x{0}";
    [Tooltip("Show the text together with the sprite")]
    public bool showTextWithSprite = false;
    public Color bonusColor = Color.white;
    public float bonusSize = 40f;
    public float bonusPopScale = 0.6f;
    public float bonusPopDuration = 0.35f;

    [Header("แอนิเมชัน")]
    [Tooltip("ความแรงที่เม็ดไอคอนเด้งโตตอนค่าเปลี่ยน")]
    public float pipPopScale = 0.55f;
    public float pipPopDuration = 0.3f;
    [Tooltip("สีที่เม็ดไอคอนแฟลชตอนค่าเปลี่ยน")]
    public Color pipFlashColor = Color.white;
    [Tooltip("ระยะที่ทั้งแถวสะบัดซ้ายขวาตอนทำพลาด (พิกเซล) ใส่ 0 = ไม่ต้องสะบัด")]
    public float rowShakeDistance = 12f;
    public float rowShakeDuration = 0.35f;
    [Tooltip("ความเร็วที่ตัวเลขความผิดพลาดกะพริบตอนเหลือโควตาสุดท้าย ใส่ 0 = ไม่ต้องกะพริบ")]
    public float dangerPulseSpeed = 3.5f;

    [Header("การแสดงผล")]
    [Tooltip("ติ๊กถูก = ซ่อน HUD ตอนไม่ได้อยู่ในช่วงเล่น (หน้าจอแพ้ ชนะ หรือตอนเปิดสมุดเลือกคนร้าย)")]
    public bool hideOutsidePlay = true;
    [Tooltip("โชว์ HUD ตั้งแต่ช่วงเนื้อเรื่องก่อนเริ่มด่านเลยไหม")]
    public bool showDuringIntro = false;
    public float fadeDuration = 0.2f;

    // เม็ดไอคอนหนึ่งเม็ด เก็บตัวจับเวลาแอนิเมชันของตัวเองไว้ จะได้เด้งทีละเม็ดได้
    private class Pip
    {
        public Image image;
        public RectTransform rect;
        public Vector3 baseScale = Vector3.one;
        public float popTimer;
        public Color targetColor = Color.white;
    }

    // หนึ่งแถวของ HUD (แถวเบาะแส กับแถวความผิดพลาด)
    private class PipRow
    {
        public RectTransform container;
        public readonly List<Pip> pips = new List<Pip>();
        public int filledCount = -1;
        public float shakeTimer;
        public Vector2 basePosition;
        public bool hasBasePosition;
    }

    private readonly PipRow clueRow = new PipRow();
    private readonly PipRow mistakeRow = new PipRow();

    private RectTransform hudRoot;
    private CanvasGroup hudCanvasGroup;

    private int shownClues = -1;
    private int shownRequiredClues = -1;
    private int shownMistakes = -1;
    private int shownMaxMistakes = -1;
    private int shownBonus = -1;
    private float bonusPopTimer;

    private void Awake()
    {
        ResolveSources();

        if (buildUiIfMissing && NeedsBuild())
        {
            BuildDefaultHud();
        }

        clueRow.container = cluePipContainer;
        mistakeRow.container = mistakePipContainer;

        // HUD เป็นแค่ตัวโชว์ผล ห้ามกินคลิกของผู้เล่น ไม่งั้นจะไปบังการคลิกวัตถุในฉาก
        hudCanvasGroup = GetHudCanvasGroup();
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.interactable = false;
            hudCanvasGroup.blocksRaycasts = false;
        }

        if (bonusImage != null) bonusImage.raycastTarget = false;
        ApplyBonusSize();
        SetGraphicRaycastTarget(bonusText, false);
        UpdateBonus(0, true);
        SetGraphicRaycastTarget(clueLabelText, false);
        SetGraphicRaycastTarget(clueCounterText, false);
        SetGraphicRaycastTarget(mistakeLabelText, false);
        SetGraphicRaycastTarget(mistakeCounterText, false);



        if (clueLabelText != null) clueLabelText.text = clueLabel;
        if (mistakeLabelText != null) mistakeLabelText.text = mistakeLabel;

    }

    private void Start()
    {
        // เฟรมแรกให้ค่าเข้าที่ทันที ไม่ต้องเล่นแอนิเมชันเด้งของค่าเริ่มต้น
        Refresh(true);

        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = GetTargetAlpha();
        }
    }

    private void LateUpdate()
    {
        ResolveSources();
        Refresh(false);

        // unscaledDeltaTime เพราะสมุดหลักฐาน/หน้าจอจบวันหยุดเวลาเกมไว้ แอนิเมชันจะได้ยังเล่นต่อ
        float dt = Time.unscaledDeltaTime;

        UpdateRowAnimation(clueRow, dt);
        UpdateRowAnimation(mistakeRow, dt);
        UpdateBonusPop(dt);
        UpdateDangerPulse();
        UpdateVisibility(dt);
    }

    // เรียกจากข้างนอกได้ ถ้าอยากบังคับให้อัปเดตทันทีโดยไม่ต้องรอเฟรมถัดไป
    public void Refresh(bool snap)
    {
        StageConfig stage = GetCurrentStage();

        int requiredClues = stage != null ? Mathf.Max(0, stage.requiredCluesToPass) : 0;
        int maxMistakes = stage != null ? Mathf.Max(0, stage.maxMistakesAllowed) : 0;

        int clues = investigationManager != null ? investigationManager.collectedClues.Count : 0;
        int mistakes = gameManager != null ? Mathf.Max(0, gameManager.currentMistakes) : 0;

        bool clueChanged = clues != shownClues || requiredClues != shownRequiredClues;
        bool mistakeChanged = mistakes != shownMistakes || maxMistakes != shownMaxMistakes;

        if (!clueChanged && !mistakeChanged) return;

        if (clueChanged)
        {
            bool quotaMet = requiredClues > 0 && clues >= requiredClues;

            if (clueCounterText != null)
            {
                clueCounterText.text = string.Format(counterFormat, clues, requiredClues);
                clueCounterText.color = quotaMet ? clueCompleteColor : counterColor;
            }

            // เม็ดเบาะแสมีเท่าโควตาของด่าน เก็บเกินโควตาได้แต่ไม่ต้องเพิ่มเม็ด
            ApplyPips(clueRow, requiredClues, Mathf.Min(clues, requiredClues), cluePipFilledColor, cluePipEmptyColor, snap);

            // Required Clues = 0 means every clue is a bonus (quota is 0, so all clues are "over" it)
            int extraClues = Mathf.Max(0, clues - requiredClues);
            UpdateBonus(extraClues, snap);

            shownClues = clues;
            shownRequiredClues = requiredClues;
        }

        if (mistakeChanged)
        {
            // GameManager แพ้ตอน currentMistakes >= maxMistakesAllowed
            // แปลว่าเหลือโควตาอีกครั้งเดียวคือพลาดอีกทีจบ เลยเตือนตั้งแต่ตอนนั้น
            bool onLastChance = maxMistakes > 0 && mistakes >= maxMistakes - 1;

            if (mistakeCounterText != null)
            {
                mistakeCounterText.text = string.Format(counterFormat, mistakes, maxMistakes);
                mistakeCounterText.color = onLastChance ? dangerColor : counterColor;
            }

            int filled = showMistakesAsLives
                ? Mathf.Clamp(maxMistakes - mistakes, 0, maxMistakes)
                : Mathf.Clamp(mistakes, 0, maxMistakes);

            ApplyPips(mistakeRow, maxMistakes, filled, mistakePipFilledColor, mistakePipEmptyColor, snap);

            // สะบัดทั้งแถวเฉพาะตอนพลาดเพิ่ม ไม่ใช่ตอนโหลดค่าครั้งแรกหรือตอนรีเซ็ตด่าน
            if (!snap && shownMistakes >= 0 && mistakes > shownMistakes && rowShakeDistance > 0f)
            {
                mistakeRow.shakeTimer = rowShakeDuration;
            }

            shownMistakes = mistakes;
            shownMaxMistakes = maxMistakes;
        }
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

    private void ApplyBonusSize()
    {
        if (bonusImage == null) return;

        bonusImage.preserveAspect = true;
        bonusImage.rectTransform.sizeDelta = new Vector2(bonusSize, bonusSize);

        LayoutElement le = bonusImage.GetComponent<LayoutElement>();
        if (le == null) le = bonusImage.gameObject.AddComponent<LayoutElement>();
        le.minWidth = bonusSize;
        le.minHeight = bonusSize;
        le.preferredWidth = bonusSize;
        le.preferredHeight = bonusSize;
    }

    private void UpdateBonus(int extra, bool snap)
    {
        ApplyBonusSize();
        bool show = extra > 0;

        Sprite sprite = null;
        if (show && bonusSprites != null && bonusSprites.Length > 0)
        {
            sprite = bonusSprites[Mathf.Clamp(extra, 1, bonusSprites.Length) - 1];
        }

        if (bonusImage != null)
        {
            bonusImage.sprite = sprite;
            bonusImage.color = bonusColor;
            bonusImage.gameObject.SetActive(show && sprite != null);
        }

        if (bonusText != null)
        {
            bool showText = show && (sprite == null || showTextWithSprite);
            bonusText.gameObject.SetActive(showText);
            if (showText) bonusText.text = string.Format(bonusFormat, extra);
        }

        // pop only when a new extra clue is collected
        if (!snap && show && extra > shownBonus && bonusPopDuration > 0f)
        {
            bonusPopTimer = bonusPopDuration;
        }

        shownBonus = extra;
    }

    private void UpdateBonusPop(float dt)
    {
        if (bonusPopTimer <= 0f) return;

        bonusPopTimer = Mathf.Max(0f, bonusPopTimer - dt);

        float progress = bonusPopDuration > 0f ? 1f - (bonusPopTimer / bonusPopDuration) : 1f;
        Vector3 scale = Vector3.one * (1f + bonusPopScale * Mathf.Sin(progress * Mathf.PI));

        if (bonusImage != null) bonusImage.rectTransform.localScale = scale;
        if (bonusText != null) bonusText.rectTransform.localScale = scale;
    }

    private Image CreateBonusImage(RectTransform parent, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.preferredWidth = bonusSize;
        layout.preferredHeight = bonusSize;

        obj.SetActive(false);
        return image;
    }

    private void ApplyPips(PipRow row, int total, int filledCount, Color filledColor, Color emptyColor, bool snap)
    {
        if (!usePips || row.container == null) return;

        EnsurePipCount(row, total);

        int previousFilled = row.filledCount;

        for (int i = 0; i < row.pips.Count; i++)
        {
            Pip pip = row.pips[i];
            if (pip == null || pip.image == null) continue;

            bool active = i < total;
            if (pip.image.gameObject.activeSelf != active)
            {
                pip.image.gameObject.SetActive(active);
            }

            if (!active) continue;

            bool isFilled = i < filledCount;
            pip.targetColor = isFilled ? filledColor : emptyColor;

            // เด้งเฉพาะเม็ดที่เพิ่งเปลี่ยนสถานะรอบนี้ จะได้รู้ว่าค่าไหนขยับ
            bool wasFilled = previousFilled >= 0 && i < previousFilled;
            bool changed = previousFilled >= 0 && wasFilled != isFilled;

            if (!snap && changed && pipPopDuration > 0f)
            {
                pip.popTimer = pipPopDuration;
            }
            else if (pip.popTimer <= 0f)
            {
                pip.image.color = pip.targetColor;
            }
        }

        row.filledCount = filledCount;
    }

    private void EnsurePipCount(PipRow row, int desiredCount)
    {
        while (row.pips.Count < desiredCount)
        {
            Pip pip = CreatePip(row.container);
            if (pip == null) break;
            row.pips.Add(pip);
        }
    }

    private Pip CreatePip(RectTransform container)
    {
        if (container == null) return null;

        Image image;

        if (pipPrefab != null)
        {
            image = Instantiate(pipPrefab, container);
        }
        else
        {
            GameObject pipObject = new GameObject("Pip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pipObject.transform.SetParent(container, false);
            image = pipObject.GetComponent<Image>();
            image.sprite = ResolvePipSprite();
        }

        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.sizeDelta = new Vector2(pipSize, pipSize);

        Pip pip = new Pip
        {
            image = image,
            rect = rect,
            baseScale = rect.localScale == Vector3.zero ? Vector3.one : rect.localScale
        };

        // เม็ดที่อยู่ใน Layout Group ต้องบอกขนาดผ่าน LayoutElement ไม่งั้น Layout จะบีบจนแบน
        LayoutElement layout = image.GetComponent<LayoutElement>();
        if (layout == null) layout = image.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = pipSize;
        layout.preferredHeight = pipSize;

        return pip;
    }

    private Sprite ResolvePipSprite()
    {
        if (pipSprite != null) return pipSprite;

        // สไปรต์วงกลมที่ติดมากับ Unity ใช้เป็นเม็ดกลม ๆ ได้เลย ไม่ต้องเตรียมอาร์ต
        // หาไม่เจอก็ไม่เป็นไร Image จะกลายเป็นสี่เหลี่ยมทึบแทน
        return Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
    }

    private void UpdateRowAnimation(PipRow row, float dt)
    {
        for (int i = 0; i < row.pips.Count; i++)
        {
            Pip pip = row.pips[i];
            if (pip == null || pip.image == null || !pip.image.gameObject.activeSelf) continue;

            if (pip.popTimer > 0f)
            {
                pip.popTimer = Mathf.Max(0f, pip.popTimer - dt);

                float progress = pipPopDuration > 0f ? 1f - (pip.popTimer / pipPopDuration) : 1f;
                float curve = Mathf.Sin(progress * Mathf.PI);

                pip.rect.localScale = pip.baseScale * (1f + pipPopScale * curve);
                pip.image.color = Color.Lerp(pip.targetColor, pipFlashColor, curve);

                if (pip.popTimer <= 0f)
                {
                    pip.rect.localScale = pip.baseScale;
                    pip.image.color = pip.targetColor;
                }
            }
        }

        if (row.container == null) return;

        if (row.shakeTimer > 0f)
        {
            if (!row.hasBasePosition)
            {
                row.hasBasePosition = true;
                row.basePosition = row.container.anchoredPosition;
            }

            row.shakeTimer = Mathf.Max(0f, row.shakeTimer - dt);

            float remaining = rowShakeDuration > 0f ? row.shakeTimer / rowShakeDuration : 0f;
            float offset = Mathf.Sin(row.shakeTimer * 46f) * rowShakeDistance * remaining;

            row.container.anchoredPosition = row.basePosition + new Vector2(offset, 0f);

            if (row.shakeTimer <= 0f)
            {
                row.container.anchoredPosition = row.basePosition;
            }
        }
    }

    // ตัวเลขความผิดพลาดกะพริบตอนเหลือโควตาครั้งสุดท้าย ให้ผู้เล่นรู้ว่าพลาดอีกครั้งเดียวจบ
    private void UpdateDangerPulse()
    {
        if (mistakeCounterText == null || dangerPulseSpeed <= 0f) return;

        bool onLastChance = shownMaxMistakes > 0 && shownMistakes >= shownMaxMistakes - 1;
        if (!onLastChance) return;

        float t = (Mathf.Sin(Time.unscaledTime * dangerPulseSpeed) + 1f) * 0.5f;
        mistakeCounterText.color = Color.Lerp(dangerColor, counterColor, t * 0.55f);
    }

    private void UpdateVisibility(float dt)
    {
        if (hudCanvasGroup == null) return;

        float target = GetTargetAlpha();

        hudCanvasGroup.alpha = fadeDuration > 0f
            ? Mathf.MoveTowards(hudCanvasGroup.alpha, target, dt / fadeDuration)
            : target;
    }

    private float GetTargetAlpha()
    {
        if (!hideOutsidePlay || gameManager == null) return 1f;

        switch (gameManager.CurrentState)
        {
            case GameManager.GameState.Playing:
                return 1f;
            case GameManager.GameState.Intro:
                return showDuringIntro ? 1f : 0f;
            default:
                // จบวัน/แพ้/ชนะ: เก็บ HUD ทิ้ง ไม่งั้นจะค้างทับหน้าจอแพ้กับสมุดหลักฐาน
                return 0f;
        }
    }

    private CanvasGroup GetHudCanvasGroup()
    {
        RectTransform root = hudRoot != null ? hudRoot : GetComponent<RectTransform>();
        if (root == null) return null;

        CanvasGroup group = root.GetComponent<CanvasGroup>();
        if (group == null) group = root.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    private void SetGraphicRaycastTarget(Graphic graphic, bool value)
    {
        if (graphic != null) graphic.raycastTarget = value;
    }

    private bool NeedsBuild()
    {
        if (clueCounterText == null || mistakeCounterText == null) return true;
        if (usePips && (cluePipContainer == null || mistakePipContainer == null)) return true;
        return false;
    }

    // สร้าง HUD ทั้งชุดตอนรันไทม์ เผื่อคนใช้ไม่อยากไปวาง UI เองใน Hierarchy
    // ช่องไหนผูกมาแล้วจะไม่ถูกสร้างทับ
    private void BuildDefaultHud()
    {
        RectTransform parent = EnsureHudRoot();
        if (parent == null) return;

        if (clueCounterText == null || (usePips && cluePipContainer == null))
        {
            RectTransform row = CreateRow(parent, "ClueRow");

            if (clueLabelText == null) clueLabelText = CreateText(row, "ClueLabel", clueLabel, labelColor, 170f);
            if (clueCounterText == null) clueCounterText = CreateText(row, "ClueCounter", string.Empty, counterColor, 190f);
            if (usePips && cluePipContainer == null) cluePipContainer = CreatePipContainer(row, "CluePips");

            if (bonusImage == null) bonusImage = CreateBonusImage(row, "ClueBonusImage");
            if (bonusText == null)
            {
                bonusText = CreateText(row, "ClueBonusText", string.Empty, clueCompleteColor, 80f);
                bonusText.gameObject.SetActive(false);
            }

        }

   

        if (mistakeCounterText == null || (usePips && mistakePipContainer == null))
        {
            RectTransform row = CreateRow(parent, "MistakeRow");

            if (mistakeLabelText == null) mistakeLabelText = CreateText(row, "MistakeLabel", mistakeLabel, labelColor, 170f);
            if (mistakeCounterText == null) mistakeCounterText = CreateText(row, "MistakeCounter", string.Empty, counterColor, 190f);
            if (usePips && mistakePipContainer == null) mistakePipContainer = CreatePipContainer(row, "MistakePips");
        }
    }

    private RectTransform EnsureHudRoot()
    {
        if (hudRoot != null) return hudRoot;

        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            // ไม่ได้อยู่ใต้ Canvas ไหนเลย สร้าง Canvas ของตัวเองแล้วย้ายตัวเองเข้าไปอยู่ข้างใน
            GameObject canvasObject = new GameObject("StageObjectiveHUD Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            transform.SetParent(canvasObject.transform, false);
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (rect == null) rect = gameObject.AddComponent<RectTransform>();

        // เกาะมุมซ้ายบนของจอ แล้วให้สองแถวเรียงลงมา
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = hudOffset;
        rect.sizeDelta = new Vector2(560f, 0f);

        VerticalLayoutGroup layout = gameObject.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = gameObject.GetComponent<ContentSizeFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        hudRoot = rect;
        return hudRoot;
    }

    private RectTransform CreateRow(RectTransform parent, string name)
    {
        GameObject rowObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = rowObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        HorizontalLayoutGroup layout = rowObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return rect;
    }

    private TextMeshProUGUI CreateText(RectTransform parent, string name, string content, Color color, float width)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        if (hudFont != null) text.font = hudFont;
        text.text = content;
        text.color = color;
        text.fontSize = hudFontSize;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = hudFontSize * 1.5f;

        return text;
    }

    private RectTransform CreatePipContainer(RectTransform parent, string name)
    {
        GameObject containerObject = new GameObject(name, typeof(RectTransform));
        RectTransform rect = containerObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        HorizontalLayoutGroup layout = containerObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = pipSpacing;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = containerObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        return rect;
    }
}
