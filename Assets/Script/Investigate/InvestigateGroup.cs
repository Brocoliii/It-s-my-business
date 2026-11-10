using UnityEngine;
using System.Collections.Generic;

public class InvestigateGroup : MonoBehaviour, IInvestigatable
{
    [System.Serializable]
    private class SpeechBubbleLayout
    {
        [Tooltip("จำนวนฟองสำหรับสปริทกลุ่มนี้")]
        [Range(1, 5)] public int count = 4;
        [Tooltip("ตำแหน่งฟองแบบ Local Position รอบกลุ่ม")]
        public Vector2[] positions = new Vector2[4];
    }

    [Header("Visual")]
    [SerializeField] private SpriteRenderer groupRenderer;
    [SerializeField] private Sprite[] possibleSprites;
    [Tooltip("Prefab เงาตามลำดับของ Possible Sprites ถ้าเว้นว่างจะไม่สร้างเงา")]
    [SerializeField] private GameObject[] possibleShadowPrefabs;

    [Header("Speech Bubbles While Listening")]
    [Tooltip("สปริทฟองคำพูดที่จะแสดงรอบกลุ่มระหว่างผู้เล่นกำลังฟัง")]
    [SerializeField] private Sprite[] speechBubbleSprites;
    [Tooltip("จำนวนฟองคำพูดที่แสดง (ตั้งได้ 1-5)")]
    [SerializeField, Range(1, 5)] private int speechBubbleCount = 4;
    [Tooltip("ตั้งค่าจำนวนและตำแหน่งฟองแยกตามลำดับของ Possible Sprites ถ้าเว้นว่างจะใช้ค่าด้านบน")]
    [SerializeField] private SpeechBubbleLayout[] speechBubbleLayouts;
    [SerializeField] private float speechBubbleScale = 0.35f;
    [SerializeField] private Vector2 speechBubbleHorizontalRange = new Vector2(-0.9f, 0.9f);
    [SerializeField] private Vector2 speechBubbleVerticalRange = new Vector2(0.65f, 1.15f);
    [SerializeField] private float speechBubbleFloatHeight = 0.08f;
    [SerializeField] private float speechBubbleFloatSpeed = 1.5f;
    [SerializeField] private float speechBubbleAppearDuration = 0.25f;
    [SerializeField] private float speechBubbleDisappearDuration = 0.18f;
    [SerializeField] private float speechBubbleAnimationStagger = 0.04f;

    [Header("Stratagem Minigame")]
    [SerializeField] private int stratagemLength = 5;
    [SerializeField] private float progressPerSuccess = 10f;
    [SerializeField] private float correctFlashDuration = 0.15f;
    [Tooltip("เวลารอหลังต่อชุดปุ่มครบ ก่อนชุดใหม่จะเด้งขึ้น เผื่อให้คลื่นไอคอนวิ่งจบก่อน")]
    [SerializeField] private float sequenceClearedDelay = 0.28f;
    [Tooltip("เวลาห้ามกดเพิ่ม หลังชุดใหม่เด้งขึ้นมาแล้ว กันผู้เล่นรัวปุ่มค้างจากชุดก่อนแล้วกินปุ่มตัวแรกของชุดใหม่ไป")]
    [SerializeField] private float postSequenceInputLockExtra = 0.12f;
    [Tooltip("เวลาค้างให้ผู้เล่นเห็นหลอดเต็มหลังเก็บเบาะแสได้ ก่อนแถบจะถูกซ่อนและกลุ่มจะหายไป")]
    [SerializeField] private float clueCompleteHoldDuration = 0.45f;
    [Tooltip("เวลารอสูงสุดให้หลอดวิ่งไปจนเต็ม กันค้างถ้าหลอดไม่ได้ถูกผูกไว้ใน Inspector")]
    [SerializeField] private float fillCompleteWaitTimeout = 1f;

    [Header("Timeout Blink")]
    [SerializeField] private float blinkStartTime = 5f;
    [SerializeField] private float maxBlinkSpeed = 2f;
    [SerializeField] private float minBlinkSpeed = 8f;
    [SerializeField, Range(0f, 1f)] private float minBlinkAlpha = 0.2f;

    [Header("Appear Animation")]
    [Tooltip("ถ้าใส่ Animator นี้ไว้ จะสั่ง Trigger \"Appear\" ตอนกลุ่มเกิด แทนการเล่นแอนิเมชันขยายจากโค้ด")]
    [SerializeField] private Animator appearAnimator;
    [Tooltip("ระยะเวลาแอนิเมชันขยายตอนกลุ่มปรากฏ (ใช้ตอนไม่มี appearAnimator)")]
    [SerializeField] private float appearDuration = 0.35f;

    [Header("Disappear Animation")]
    [Tooltip("ถ้าใส่ Animator นี้ไว้ จะสั่ง Trigger \"Hide\" ตอนกลุ่มหายไป แทนการเล่นแอนิเมชันจางหายจากโค้ด")]
    [SerializeField] private Animator disappearAnimator;
    [Tooltip("ระยะเวลาแอนิเมชันจางหายตอนกลุ่มหายไป (ใช้เป็นเวลารอก่อน Destroy ด้วยตอนมี disappearAnimator)")]
    [SerializeField] private float disappearDuration = 0.3f;
    [Tooltip("ขนาดตอนจบแอนิเมชันจางหาย (1 = ไม่ย่อลง)")]
    [SerializeField] private float disappearEndScale = 0.85f;

    [Header("Stratagem Reaction")]
    [Tooltip("ระยะเวลาที่ตัวกลุ่มสะบัดตอนกดผิด")]
    [SerializeField] private float wrongReactionDuration = 0.35f;
    [Tooltip("ความแรงที่ตัวกลุ่มบีบ/ยืดตอนกดผิด")]
    [SerializeField, Range(0f, 0.5f)] private float wrongReactionSquash = 0.12f;
    [Tooltip("ระยะเวลาที่ตัวกลุ่มเด้งตอนต่อชุดปุ่มครบ")]
    [SerializeField] private float successReactionDuration = 0.3f;
    [Tooltip("ความแรงที่ตัวกลุ่มเด้งตอนต่อชุดปุ่มครบ")]
    [SerializeField, Range(0f, 0.5f)] private float successReactionPop = 0.18f;

    [HideInInspector] public string clueDetail;
    [HideInInspector] public Transform assignedSpawnPoint;

    private float requiredListenTime;
    private float currentListenTime;
    private float lifeTimer;
    private float blinkPhase;
    private Color baseSpriteColor = Color.white;

    private bool isBeingListened = false;
    private bool isClueCollected = false;
    private bool isResettingAfterMistake = false;
    private bool isRemoving = false;
    private InvestigationManager manager;
    private readonly List<StratagemDirection> currentStratagem = new List<StratagemDirection>();
    private int currentStratagemIndex = 0;
    private int feedbackFlashIndex = -1;
    private Color? feedbackFlashColor = null;
    private int feedbackFlashToken = 0;
    private bool pendingSequenceIntro = false;
    private Vector3 baseGroupScale = Vector3.one;
    private bool isPlayingAppearAnimation = false;
    private Coroutine reactionCoroutine;
    private bool isHoldingCompleteBar = false;
    private bool skipRemainingCompleteHold = false;
    private float stratagemInputUnlockTime = 0f;
    private int stratagemGenerationToken = 0;
    private int selectedSpriteIndex = -1;
    private GameObject instantiatedShadow;
    private SpriteRenderer[] shadowRenderers = new SpriteRenderer[0];
    private Color[] shadowBaseColors = new Color[0];
    private readonly List<Transform> speechBubbles = new List<Transform>();
    private readonly List<float> speechBubbleBaseHeights = new List<float>();
    private readonly List<float> speechBubblePhases = new List<float>();
    private readonly List<float> speechBubbleBaseAlphas = new List<float>();
    private Coroutine speechBubbleAnimationCoroutine;

    public bool IsBeingListened => isBeingListened;
    public bool IsClueCollected => isClueCollected;

    // ช่วงห้ามกดหลังต่อชุดปุ่มครบ ใช้เวลาแทนคอรูทีน จะได้ไม่ต้องคอยไล่เคลียร์ธงตามทางออกทุกทาง
    private bool IsStratagemInputLocked => Time.time < stratagemInputUnlockTime;

    private void LockStratagemInput(float duration)
    {
        if (duration <= 0f)
            return;

        // ต่อเวลาเท่านั้น ไม่ตัดให้สั้นลง เผื่อมีตัวล็อกอื่นที่ยาวกว่าค้างอยู่
        stratagemInputUnlockTime = Mathf.Max(stratagemInputUnlockTime, Time.time + duration);
    }

    // สำหรับ Debug/Skip เท่านั้น: บังคับให้กลุ่มออกจากฉากทันที ผ่านเส้นทางเดิม (RemoveGroup)
    // เพื่อให้ manager.OnGroupLeft ถูกเรียกด้วย ไม่งั้น activeGroups/occupiedPoints ใน InvestigationManager จะค้าง
    // ค่าเริ่มต้นไม่ทิ้งเบาะแส เพราะนี่ไม่ใช่การที่ผู้เล่นฟังไม่ทันจริง ๆ
    public void ForceRemove(bool discardClue = false)
    {
        RemoveGroup(discardClue);
    }

    // lifetime = เวลาที่กลุ่มอยู่บนจอก่อนหายไป ส่งมาจาก InvestigationManager ตามค่าใน StageConfig
    // ใส่ <= 0 มา จะย้อนไปใช้สูตรเดิม (เวลาแอบฟัง + 10 วินาที) เผื่อฉากไหนยังไม่ได้ตั้งค่าในด่าน
    public void Init(InvestigationManager mgr, string text, float duration, float lifetime = 0f)
    {
        manager = mgr;
        clueDetail = text;
        requiredListenTime = duration;
        currentListenTime = 0f;
        currentStratagem.Clear();
        currentStratagemIndex = 0;
        lifeTimer = lifetime > 0f ? lifetime : requiredListenTime + 10f;
        blinkPhase = 0.25f;
        baseGroupScale = transform.localScale;
        ApplyRandomSprite();
        if (groupRenderer != null)
        {
            baseSpriteColor = groupRenderer.color;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateWarningBubble();
            UIManager.Instance.ShowInvestigateDirectionIndicator(transform);
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(true);
            UIManager.Instance.UpdateInvestigateTimer(lifeTimer);
        }

        // ยิงที่ตำแหน่งของกลุ่ม เผื่อเสียงตั้ง Is 3D ไว้ จะได้ดังมาจากมุมที่กลุ่มโผล่จริง
        // ดังพร้อมกับที่แอนิเมชันเริ่มเด้งขึ้นพอดี ผู้เล่นจะได้หันไปมองทัน
        SoundManager.Instance.PlaySFX(SoundIds.InvestigateAppear, transform.position);

        StartCoroutine(PlayAppearAnimation());
    }

    // เล่นแอนิเมชันตอนกลุ่มปรากฏขึ้นมา
    // ถ้ามี appearAnimator จะสั่ง Trigger "Appear" ให้ Animator Controller เล่นเอง
    // ถ้าไม่มีจะเล่นแอนิเมชันขยายจาก 0 ขึ้นมาแบบเด้ง (ease-out back) จากโค้ดแทน
    private System.Collections.IEnumerator PlayAppearAnimation()
    {
        if (appearAnimator != null)
        {
            appearAnimator.SetTrigger("Appear");
            yield break;
        }

        Vector3 targetScale = baseGroupScale;
        transform.localScale = Vector3.zero;

        // กันไม่ให้แอนิเมชันตอบสนอง Stratagem มาแย่งเขียน localScale ระหว่างกลุ่มกำลังเด้งขึ้น
        isPlayingAppearAnimation = true;

        float time = 0f;
        while (time < appearDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / appearDuration);
            float overshoot = 1.70158f;
            float easedT = t - 1f;
            float easeOutBack = 1f + (overshoot + 1f) * Mathf.Pow(easedT, 3f) + overshoot * Mathf.Pow(easedT, 2f);
            transform.localScale = targetScale * easeOutBack;
            yield return null;
        }
        transform.localScale = targetScale;
        isPlayingAppearAnimation = false;
    }

    // แอนิเมชันตอบสนองของตัวกลุ่มเวลาผู้เล่นกด Stratagem
    // isPop = true คือเด้งโตขึ้นแล้วยุบกลับ (ต่อชุดครบ), false คือสะบัดบีบ/ยืดซ้ายขวา (กดผิด)
    private void PlayGroupReaction(float amount, float duration, bool isPop)
    {
        if (isRemoving || isPlayingAppearAnimation || duration <= 0f || amount <= 0f)
            return;

        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            // คืนสเกลก่อนเริ่มรอบใหม่ ไม่งั้นสเกลจะเพี้ยนสะสมเวลากดรัว ๆ
            transform.localScale = baseGroupScale;
        }

        reactionCoroutine = StartCoroutine(GroupReactionRoutine(amount, duration, isPop));
    }

    private System.Collections.IEnumerator GroupReactionRoutine(float amount, float duration, bool isPop)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);

            if (isPop)
            {
                float pop = Mathf.Sin(t * Mathf.PI) * amount;
                transform.localScale = new Vector3(baseGroupScale.x * (1f + pop * 0.6f), baseGroupScale.y * (1f + pop), baseGroupScale.z);
            }
            else
            {
                float wave = Mathf.Sin(t * Mathf.PI * 6f) * amount * (1f - t);
                transform.localScale = new Vector3(baseGroupScale.x * (1f + wave), baseGroupScale.y * (1f - wave), baseGroupScale.z);
            }

            yield return null;
        }

        transform.localScale = baseGroupScale;
        reactionCoroutine = null;
    }

    private void ApplyRandomSprite()
    {
        if (groupRenderer == null || possibleSprites == null || possibleSprites.Length == 0)
            return;

        groupRenderer.enabled = true;
        selectedSpriteIndex = Random.Range(0, possibleSprites.Length);
        groupRenderer.sprite = possibleSprites[selectedSpriteIndex];
        CreateSelectedShadow();
    }

    private void CreateSelectedShadow()
    {
        if (instantiatedShadow != null)
        {
            Destroy(instantiatedShadow);
            instantiatedShadow = null;
        }
        shadowRenderers = new SpriteRenderer[0];
        shadowBaseColors = new Color[0];

        if (possibleShadowPrefabs == null
            || selectedSpriteIndex < 0
            || selectedSpriteIndex >= possibleShadowPrefabs.Length)
            return;

        GameObject shadowPrefab = possibleShadowPrefabs[selectedSpriteIndex];
        if (shadowPrefab == null)
            return;

        instantiatedShadow = Instantiate(shadowPrefab, transform, false);

        if (groupRenderer == null)
            return;

        shadowRenderers = instantiatedShadow.GetComponentsInChildren<SpriteRenderer>(true);
        shadowBaseColors = new Color[shadowRenderers.Length];
        for (int i = 0; i < shadowRenderers.Length; i++)
        {
            if (shadowRenderers[i] == null)
                continue;

            shadowBaseColors[i] = shadowRenderers[i].color;
            shadowRenderers[i].sortingLayerID = groupRenderer.sortingLayerID;
            shadowRenderers[i].sortingOrder = groupRenderer.sortingOrder - 1;
        }
    }

    private void Update()
    {
        // ระหว่างเล่นแอนิเมชันจางหาย ตัวกลุ่มยังไม่ถูก Destroy จริง
        // ต้องหยุด Update ทั้งหมด ไม่งั้นการกะพริบจะไปทับค่า alpha ที่กำลังจางอยู่
        // และ RemoveGroup() จะถูกเรียกซ้ำทุกเฟรมตอนหมดเวลา
        if (isRemoving)
            return;

        UpdateSpeechBubbles();

        if (lifeTimer > 0)
        {
            lifeTimer -= Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdateInvestigateTimer(lifeTimer);
            }

            if (!isClueCollected)
            {
                UpdateTimeoutBlink();
            }
        }
        else if (!isClueCollected)
        {
            // หมดเวลาแล้วยังฟังไม่จบ = เบาะแสหลุด ดังเสียงเตือนตรงนี้ที่เดียว
            // ไม่ไปใส่ใน RemoveGroup เพราะทางนั้นถูกเรียกตอนเก็บเบาะแสสำเร็จ
            // และตอน Debug/Skip ด้วย จะกลายเป็นดังผิดจังหวะ
            SoundManager.Instance.PlaySFX(SoundIds.InvestigateTimeout);
            RemoveGroup();
            return;
        }

        if (isBeingListened && !isClueCollected)
        {
            RefreshInvestigationUI();
        }
    }

    private void UpdateTimeoutBlink()
    {
        if (groupRenderer == null)
            return;

        if (lifeTimer > blinkStartTime)
        {
            blinkPhase = 0.25f;
            if (groupRenderer.color != baseSpriteColor)
                groupRenderer.color = baseSpriteColor;
            SetShadowBlinkOpacity(1f);
            return;
        }

        float urgency = 1f - Mathf.Clamp01(lifeTimer / blinkStartTime);
        float blinkSpeed = Mathf.Lerp(maxBlinkSpeed, minBlinkSpeed, urgency);

        blinkPhase += Time.deltaTime * blinkSpeed;
        float pulse = (Mathf.Sin(blinkPhase * Mathf.PI * 2f) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minBlinkAlpha, baseSpriteColor.a, pulse);

        Color c = baseSpriteColor;
        c.a = alpha;
        groupRenderer.color = c;
        SetShadowBlinkOpacity(baseSpriteColor.a > 0f ? alpha / baseSpriteColor.a : 1f);
    }

    private void SetShadowBlinkOpacity(float opacity)
    {
        opacity = Mathf.Clamp01(opacity);

        for (int i = 0; i < shadowRenderers.Length; i++)
        {
            if (shadowRenderers[i] == null)
                continue;

            Color color = shadowBaseColors[i];
            color.a = shadowBaseColors[i].a * opacity;
            shadowRenderers[i].color = color;
        }
    }

    public float GetTimeoutBlinkOpacity()
    {
        if (groupRenderer == null || baseSpriteColor.a <= 0f)
            return 1f;

        return Mathf.Clamp01(groupRenderer.color.a / baseSpriteColor.a);
    }

    public void OnListenStart()
    {
        if (!isClueCollected && !isRemoving)
        {
            isBeingListened = true;
            EnsureStratagemExists();
            UIManager.Instance?.HideInvestigateDirectionIndicator();
            // เล่นแอนิเมชันไอคอนเด้งขึ้นใหม่ทุกครั้งที่เริ่มฟัง แม้จะเป็นชุดเดิมที่ค้างไว้
            // ไม่งั้นไอคอนจะโผล่มาเฉย ๆ ตอนกดฟังซ้ำ
            pendingSequenceIntro = true;

            SoundManager.Instance.PlaySFX(SoundIds.BinocularsOn);
            SoundManager.Instance.PlaySFX(SoundIds.InvestigateStart);

            ShowSpeechBubbles();
            UIManager.Instance.ShowListeningBar(true);
            UIManager.Instance.ShowBinocularsOverlay(true);
            UIManager.Instance.ShowInvestigateSequence(true);
            UIManager.Instance.SetBackgroundDark(true);
            CustomerManager.Instance?.SetAllCustomerCanvasesVisible(false);
            RefreshInvestigationUI();
        }
    }

    public void OnListening()
    {
        if (!isBeingListened || isClueCollected || isRemoving) return;

        RefreshInvestigationUI();
    }

    public void OnStratagemInput(StratagemDirection direction)
    {
        // IsStratagemInputLocked = ยังอยู่ในจังหวะฉลองต่อชุดครบ ปุ่มที่กดช่วงนี้ต้องทิ้งทั้งหมด
        // ห้ามปล่อยให้หลุดไปถึง EnsureStratagemExists เพราะตอนนั้น index เลยท้ายชุดไปแล้ว
        // มันจะสุ่มชุดใหม่ให้ทันทีตั้งแต่ปุ่มแรกที่กด = ข้ามคลื่นไอคอนฉลอง แล้วชุดใหม่ก็โดนกินไปหนึ่งปุ่มโดยที่ผู้เล่นยังไม่ทันเห็น
        if (!isBeingListened || isClueCollected || isResettingAfterMistake || isRemoving || IsStratagemInputLocked)
            return;

        EnsureStratagemExists();

        if (currentStratagem.Count == 0)
            return;

        if (direction == currentStratagem[currentStratagemIndex])
        {
            SoundManager.Instance.PlaySFX(SoundIds.InvestigateCorrect);
            HandleCorrectInputFeedback();
        }
        else
        {
            SoundManager.Instance.PlaySFX(SoundIds.InvestigateWrong);
            StartCoroutine(HandleWrongInputFeedback());
        }
    }

    public void OnListenEnd()
    {
        if (isBeingListened)
        {
            isBeingListened = false;

            if (!isClueCollected && !isRemoving)
            {
                UIManager.Instance?.ShowInvestigateDirectionIndicator(transform);
            }

            SoundManager.Instance.PlaySFX(SoundIds.BinocularsOff);

            HideSpeechBubbles();
            UIManager.Instance.ShowListeningBar(false);
            UIManager.Instance.ShowBinocularsOverlay(false);
            UIManager.Instance.ShowInvestigateSequence(false);
            UIManager.Instance.SetBackgroundDark(false);
            CustomerManager.Instance?.SetAllCustomerCanvasesVisible(true);
        }
        else if (isHoldingCompleteBar)
        {
            // ปล่อยเมาส์ตอนกำลังค้างหลอดเต็มอยู่ InputManager จะซูมกล้องออกทันที
            // ถ้ายังค้างแว่นส่องกับฉากมืดไว้ต่อจะดูเหมือนบั๊ก ตัดจบการค้างทิ้งเลย
            skipRemainingCompleteHold = true;
        }
    }

    private void ShowSpeechBubbles()
    {
        if (speechBubbleSprites == null || speechBubbleSprites.Length == 0)
            return;

        if (speechBubbles.Count == 0)
            CreateSpeechBubbles();

        if (speechBubbleAnimationCoroutine != null)
            StopCoroutine(speechBubbleAnimationCoroutine);

        for (int i = 0; i < speechBubbles.Count; i++)
        {
            Transform bubble = speechBubbles[i];
            if (bubble == null)
                continue;

            bubble.gameObject.SetActive(true);
            bubble.localScale = Vector3.zero;
            SpriteRenderer renderer = bubble.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 0f;
                renderer.color = color;
            }
        }

        speechBubbleAnimationCoroutine = StartCoroutine(AnimateSpeechBubbles(true));
    }

    private void HideSpeechBubbles()
    {
        if (speechBubbleAnimationCoroutine != null)
            StopCoroutine(speechBubbleAnimationCoroutine);

        speechBubbleAnimationCoroutine = StartCoroutine(AnimateSpeechBubbles(false));
    }

    private System.Collections.IEnumerator AnimateSpeechBubbles(bool appearing)
    {
        float duration = appearing ? speechBubbleAppearDuration : speechBubbleDisappearDuration;
        Vector3 targetScale = Vector3.one * speechBubbleScale;
        List<int> animationOrder = CreateRandomBubbleOrder();

        for (int orderIndex = 0; orderIndex < animationOrder.Count; orderIndex++)
        {
            int bubbleIndex = animationOrder[orderIndex];
            Transform bubble = speechBubbles[bubbleIndex];
            if (bubble == null)
                continue;

            if (!appearing)
            {
                bubble.localScale = targetScale;
            }

            SpriteRenderer renderer = bubble.GetComponent<SpriteRenderer>();
            Color targetColor = renderer != null ? renderer.color : Color.white;
            if (bubbleIndex < speechBubbleBaseAlphas.Count)
                targetColor.a = speechBubbleBaseAlphas[bubbleIndex];
            float startAlpha = appearing ? 0f : targetColor.a;
            float elapsed = -orderIndex * speechBubbleAnimationStagger;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
                t = t * t * (3f - 2f * t);

                bubble.localScale = Vector3.Lerp(
                    appearing ? Vector3.zero : targetScale,
                    appearing ? targetScale : Vector3.zero,
                    t);

                if (renderer != null)
                {
                    Color color = targetColor;
                    color.a = Mathf.Lerp(startAlpha, appearing ? targetColor.a : 0f, t);
                    renderer.color = color;
                }

                yield return null;
            }

            if (appearing)
            {
                bubble.localScale = targetScale;
                if (renderer != null)
                    renderer.color = targetColor;
            }
            else
            {
                bubble.gameObject.SetActive(false);
            }
        }

        speechBubbleAnimationCoroutine = null;
    }

    private List<int> CreateRandomBubbleOrder()
    {
        List<int> order = new List<int>();
        for (int i = 0; i < speechBubbles.Count; i++)
            order.Add(i);

        for (int i = order.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temporary = order[i];
            order[i] = order[randomIndex];
            order[randomIndex] = temporary;
        }

        return order;
    }

    private void CreateSpeechBubbles()
    {
        SpeechBubbleLayout layout = GetSelectedSpeechBubbleLayout();
        int count = layout != null
            ? Mathf.Clamp(layout.count, 1, 5)
            : Mathf.Clamp(speechBubbleCount, 1, 5);

        for (int i = 0; i < count; i++)
        {
            GameObject bubbleObject = new GameObject("Speech Bubble");
            bubbleObject.transform.SetParent(transform, false);
            Vector2 position = layout != null && layout.positions != null && i < layout.positions.Length
                ? layout.positions[i]
                : GetDefaultSpeechBubblePosition(i, count);
            bubbleObject.transform.localPosition = position;
            bubbleObject.transform.localScale = Vector3.one * speechBubbleScale;

            SpriteRenderer bubbleRenderer = bubbleObject.AddComponent<SpriteRenderer>();
            bubbleRenderer.sprite = speechBubbleSprites[Random.Range(0, speechBubbleSprites.Length)];
            if (groupRenderer != null)
            {
                bubbleRenderer.sortingLayerID = groupRenderer.sortingLayerID;
                bubbleRenderer.sortingOrder = groupRenderer.sortingOrder + 1;
            }

            speechBubbles.Add(bubbleObject.transform);
            speechBubbleBaseHeights.Add(bubbleObject.transform.localPosition.y);
            speechBubblePhases.Add(Random.Range(0f, Mathf.PI * 2f));
            speechBubbleBaseAlphas.Add(bubbleRenderer.color.a);
            bubbleObject.SetActive(false);
        }
    }

    private SpeechBubbleLayout GetSelectedSpeechBubbleLayout()
    {
        if (speechBubbleLayouts == null
            || selectedSpriteIndex < 0
            || selectedSpriteIndex >= speechBubbleLayouts.Length)
            return null;

        return speechBubbleLayouts[selectedSpriteIndex];
    }

    private Vector2 GetDefaultSpeechBubblePosition(int index, int count)
    {
        float horizontalT = count == 1 ? 0.5f : (float)index / (count - 1);
        float horizontal = Mathf.Lerp(
            speechBubbleHorizontalRange.x,
            speechBubbleHorizontalRange.y,
            horizontalT);
        float verticalT = index % 2 == 0 ? 0.2f : 0.8f;
        float vertical = Mathf.Lerp(
            speechBubbleVerticalRange.x,
            speechBubbleVerticalRange.y,
            verticalT);
        return new Vector2(horizontal, vertical);
    }

    private void UpdateSpeechBubbles()
    {
        if (!isBeingListened || speechBubbles.Count == 0)
            return;

        for (int i = 0; i < speechBubbles.Count; i++)
        {
            Transform bubble = speechBubbles[i];
            if (bubble == null)
                continue;

            Vector3 position = bubble.localPosition;
            position.y = speechBubbleBaseHeights[i]
                + Mathf.Sin(Time.time * speechBubbleFloatSpeed + speechBubblePhases[i]) * speechBubbleFloatHeight;
            bubble.localPosition = position;
        }
    }

    private void EnsureStratagemExists()
    {
        // If there is no stratagem or the index has already advanced past
        // the end (e.g. player completed the sequence but exited),
        // regenerate and reset related state so the UI isn't stuck showing
        // all-completed icons on next entry.
        if (currentStratagem.Count == 0 || currentStratagemIndex >= currentStratagem.Count)
        {
            GenerateNewStratagem();
            currentStratagemIndex = 0;
            feedbackFlashIndex = -1;
            feedbackFlashColor = null;
        }
    }

    private void GenerateNewStratagem()
    {
        // ทุกครั้งที่สุ่มชุดใหม่ถือเป็นการยกเลิกคิวสุ่มชุดใหม่ที่ค้างอยู่
        stratagemGenerationToken++;
        currentStratagem.Clear();

        for (int i = 0; i < stratagemLength; i++)
        {
            currentStratagem.Add((StratagemDirection)Random.Range(0, 4));
        }

        currentStratagemIndex = 0;
        pendingSequenceIntro = true;
    }

    // force = true ใช้เฉพาะตอนต้องล้างสีแฟลชหลังติดธง isClueCollected แล้ว
    // (ระหว่างค้างหลอดเต็มก่อนซ่อนแถบ) ไอคอนยังอยู่บนจอและผู้เล่นยังเห็นอยู่
    private void RefreshInvestigationUI(bool force = false)
    {
        if (UIManager.Instance == null || (isClueCollected && !force))
            return;

        float progress = requiredListenTime <= 0f ? 0f : currentListenTime / requiredListenTime;
        UIManager.Instance.UpdateListeningBar(progress);
        UIManager.Instance.UpdateInvestigateSequence(currentStratagem, currentStratagemIndex, $"{currentStratagemIndex}/{currentStratagem.Count} | {Mathf.CeilToInt(currentListenTime)}s / {Mathf.CeilToInt(requiredListenTime)}s", feedbackFlashIndex, feedbackFlashColor);

        // สั่งเล่นแอนิเมชันไอคอนเด้งขึ้นตรงนี้เท่านั้น ห้ามไปสั่งใน GenerateNewStratagem
        // เพราะตอนนั้นไอคอนยังไม่ถูกสร้าง/เปลี่ยนสไปรต์ แอนิเมชันจะไปเล่นกับชุดเก่าแทน
        if (pendingSequenceIntro && isBeingListened)
        {
            pendingSequenceIntro = false;
            UIManager.Instance.PlayStratagemIntro();
        }
    }

    private void HandleCorrectInputFeedback()
    {
        int flashedIndex = currentStratagemIndex;
        currentStratagemIndex++;
        PlayFeedbackFlash(flashedIndex, UIManager.Instance != null ? UIManager.Instance.flashCorrectColor : Color.green, correctFlashDuration);

        // เรียกหลัง PlayFeedbackFlash เพราะข้างในนั้นรีเฟรช UI ซึ่งเป็นตอนที่ไอคอนถูกอัปเดตสี
        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayStratagemCorrect(flashedIndex);
        }

        RefreshInvestigationUI();

        if (currentStratagemIndex >= currentStratagem.Count)
        {
            currentListenTime = Mathf.Min(requiredListenTime, currentListenTime + progressPerSuccess);

            // ต้องดันค่าใหม่เข้าหลอดตรงนี้ ก่อนจะไปติดธง isClueCollected
            // เพราะ RefreshInvestigationUI() จะ return ทิ้งทันทีเมื่อธงติดแล้ว
            // ชุดปุ่มสุดท้ายจึงไม่เคยส่งค่าเต็มเข้าหลอดเลย แถบหายไปทั้งที่หลอดยังไม่ขึ้น
            RefreshInvestigationUI();

            if (UIManager.Instance != null)
            {
                UIManager.Instance.PlayStratagemSequenceCleared(flashedIndex);
            }

            PlayGroupReaction(successReactionPop, successReactionDuration, true);

            if (currentListenTime >= requiredListenTime)
            {
                isClueCollected = true;
                isBeingListened = false;
                isResettingAfterMistake = false;
                HideSpeechBubbles();
                SoundManager.Instance.PlaySFX(SoundIds.InvestigateSuccess);

                if (groupRenderer != null)
                {
                    groupRenderer.color = baseSpriteColor;
                }
                SetShadowBlinkOpacity(1f);

                StartCoroutine(CollectClueAfterBarFilled());
                return;
            }

            // ล็อกปุ่มคลุมทั้งช่วงรอ + เผื่ออีกนิดหลังชุดใหม่เด้งขึ้น
            // ผู้เล่นจะได้เห็นชุดใหม่ก่อนเสมอ ไม่ใช่กดรัวข้ามมาจากชุดเดิม
            LockStratagemInput(sequenceClearedDelay + postSequenceInputLockExtra);

            StartCoroutine(AdvanceToNextStratagemAfterFlash(sequenceClearedDelay));
            return;
        }

        RefreshInvestigationUI();
    }

    // รอให้หลอดวิ่งขึ้นไปจนเต็มแล้วค้างไว้ให้ผู้เล่นเห็นก่อน ค่อยซ่อน UI แล้วเก็บเบาะแส
    // ถ้าซ่อนทันทีในเฟรมเดียวกับที่หลอดเต็ม ผู้เล่นจะไม่เห็นหลอดขึ้น เห็นแค่แถบหายไปเฉย ๆ
    private System.Collections.IEnumerator CollectClueAfterBarFilled()
    {
        isHoldingCompleteBar = true;
        skipRemainingCompleteHold = false;

        float waited = 0f;
        while (!skipRemainingCompleteHold && waited < fillCompleteWaitTimeout
               && UIManager.Instance != null && !UIManager.Instance.IsListeningFillFull)
        {
            waited += Time.deltaTime;
            yield return null;
        }

        float holdTarget = clueCompleteHoldDuration;

        // สั่งฉลองตอนนี้เท่านั้น คือหลังหลอดวิ่งไปเต็มแล้วจริง ๆ
        // แล้วยืดเวลาค้างให้ยาวพอที่แอนิเมชันฉลองจะเล่นจบก่อนแถบจะถูกซ่อน
        // ถ้าผู้เล่นปล่อยเมาส์ไปแล้ว (skip) ก็ไม่ต้องฉลอง เพราะกล้องซูมออกไปแล้วมองไม่เห็นอยู่ดี
        if (!skipRemainingCompleteHold && UIManager.Instance != null)
        {
            UIManager.Instance.PlayInvestigateSuccess();
            holdTarget = Mathf.Max(holdTarget, UIManager.Instance.InvestigateSuccessDuration);
        }

        float held = 0f;
        while (!skipRemainingCompleteHold && held < holdTarget)
        {
            held += Time.deltaTime;
            yield return null;
        }

        isHoldingCompleteBar = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowListeningBar(false);
            UIManager.Instance.ShowBinocularsOverlay(false);
            UIManager.Instance.ShowInvestigateSequence(false);
            UIManager.Instance.SetBackgroundDark(false);
        }
        CustomerManager.Instance?.SetAllCustomerCanvasesVisible(true);

        manager.CollectClue(clueDetail);
        RemoveGroup();
    }

    private System.Collections.IEnumerator AdvanceToNextStratagemAfterFlash(float delay)
    {
        int token = stratagemGenerationToken;

        yield return new WaitForSeconds(delay);

        // token ไม่ตรง = มีการสุ่มชุดใหม่ไปแล้วระหว่างรอ (เช่นผู้เล่นปล่อยเมาส์แล้วกดฟังใหม่
        // ซึ่ง OnListenStart จะสุ่มให้ทันที) ถ้าสุ่มซ้ำตรงนี้ ปุ่มที่ผู้เล่นเพิ่งกดไปจะโดนล้างทิ้ง
        if (!isBeingListened || isClueCollected || token != stratagemGenerationToken)
        {
            yield break;
        }

        feedbackFlashIndex = -1;
        feedbackFlashColor = null;
        GenerateNewStratagem();
        RefreshInvestigationUI();
    }

    private System.Collections.IEnumerator HandleWrongInputFeedback()
    {
        isResettingAfterMistake = true;
        PlayFeedbackFlash(currentStratagemIndex, UIManager.Instance != null ? UIManager.Instance.flashWrongColor : Color.red, 1f);

        if (UIManager.Instance != null)
        {
            UIManager.Instance.PlayStratagemWrong(currentStratagemIndex);
        }

        PlayGroupReaction(wrongReactionSquash, wrongReactionDuration, false);

        yield return new WaitForSeconds(1f);

        GenerateNewStratagem();
        isResettingAfterMistake = false;
        feedbackFlashIndex = -1;
        feedbackFlashColor = null;
        RefreshInvestigationUI();
    }

    private void PlayFeedbackFlash(int flashIndex, Color flashColor, float duration)
    {
        feedbackFlashToken++;
        int token = feedbackFlashToken;

        feedbackFlashIndex = flashIndex;
        feedbackFlashColor = flashColor;

        RefreshInvestigationUI();
        StartCoroutine(ClearFeedbackFlashAfterDelay(token, duration));
    }

    private System.Collections.IEnumerator ClearFeedbackFlashAfterDelay(int token, float duration)
    {
        yield return new WaitForSeconds(duration);

        if (token != feedbackFlashToken)
        {
            yield break;
        }

        feedbackFlashIndex = -1;
        feedbackFlashColor = null;

        // ต้อง force เพราะปุ่มตัวสุดท้ายของชุดสุดท้ายจะติดธง isClueCollected ไปแล้ว
        // ตั้งแต่ก่อนแฟลชจะหมดเวลา ถ้าไม่ force ไอคอนตัวนั้นจะค้างเป็นสีแฟลช
        // ไม่ยอมไล่กลับไปเป็นสี "กดผ่านแล้ว" เหมือนตัวอื่นตลอดช่วงฉลอง
        // force เฉพาะตอนยังค้างหลอดเต็มอยู่ (แถบยังโชว์) ถ้าแถบถูกซ่อนไปแล้วอย่าไปปลุกไอคอนกลับมา
        RefreshInvestigationUI(isHoldingCompleteBar);
    }

    private void RemoveGroup(bool discardClue = true)
    {
        // กันเรียกซ้ำ เพราะตอนนี้ยังไม่ Destroy ทันที ต้องรอแอนิเมชันจางหายจบก่อน
        if (isRemoving)
            return;

        isRemoving = true;

        // หยุดแอนิเมชันตอบสนอง Stratagem แล้วคืนสเกลก่อน
        // ไม่งั้นแอนิเมชันจางหายจะไปจับสเกลตอนกำลังเด้งค้างอยู่มาเป็นสเกลเริ่มต้น
        if (reactionCoroutine != null)
        {
            StopCoroutine(reactionCoroutine);
            reactionCoroutine = null;
            transform.localScale = baseGroupScale;
        }

        if (isBeingListened)
        {
            isBeingListened = false;
            UIManager.Instance.ShowListeningBar(false);
            UIManager.Instance.ShowBinocularsOverlay(false);
            UIManager.Instance.ShowInvestigateSequence(false);
            UIManager.Instance.SetBackgroundDark(false);
            CustomerManager.Instance?.SetAllCustomerCanvasesVisible(true);
        }
        HideSpeechBubbles();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(false);
            UIManager.Instance.HideInvestigateDirectionIndicator();
        }

        // ปิด Collider ก่อนเริ่มจางหาย ไม่ให้ผู้เล่นกดฟังกลุ่มที่กำลังจะหายไปแล้วได้อีก
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        // ผู้เล่นฟังไม่จบจนกลุ่มหายไป = เบาะแสนี้หลุดถาวร ไม่ถูกสุ่มกลับมาอีก
        // ต้องทิ้งก่อนเรียก OnGroupLeft เพราะหลังจากนั้นกลุ่มถัดไปมีสิทธิ์เกิดได้แล้ว
        // ถ้าทิ้งทีหลังกลุ่มใหม่อาจหยิบเบาะแสเดิมไปใช้ซ้ำ
        if (discardClue && !isClueCollected)
        {
            // ปล่อยกลุ่มเดินจากไปทั้งที่ยังฟังไม่จบ ถือเป็นความผิดพลาดของผู้เล่น 1 ครั้ง
            // นับก่อนเรียก DiscardClue เพราะข้างในนั้นอาจตัดจบเกม (เบาะแสเหลือไม่พอถึงโควตา)
            // ถ้านับทีหลัง AddMistake จะโดนกันไว้ตอนเกมจบแล้ว หลอดพลาดบน HUD จะไม่ขยับให้เห็น
            GameManager.Instance.AddMistake();

            manager.DiscardClue(clueDetail);
        }

        // บอก manager ให้ปล่อยจุดเกิดคืนทันที ไม่ต้องรอแอนิเมชันจบ
        // กลุ่มถัดไปจะได้เกิดต่อได้ตามจังหวะเดิม
        manager.OnGroupLeft(this);

        StartCoroutine(PlayDisappearAnimation());
    }

    // เล่นแอนิเมชันตอนกลุ่มหายไป (ทั้งตอนเก็บเบาะแสสำเร็จและตอนหมดเวลา)
    // ถ้ามี disappearAnimator จะสั่ง Trigger "Hide" แล้วรอ disappearDuration ก่อน Destroy
    // ถ้าไม่มีจะจาง alpha ลงพร้อมย่อขนาดแบบ smoothstep จากโค้ดแทน
    private System.Collections.IEnumerator PlayDisappearAnimation()
    {
        if (disappearDuration > 0f)
        {
            if (disappearAnimator != null)
            {
                disappearAnimator.SetTrigger("Hide");
                yield return new WaitForSeconds(disappearDuration);
            }
            else
            {
                // เก็บ renderer ลูกทั้งหมดไว้ด้วย เผื่อ prefab มีสไปรต์ซ้อนหลายชั้น
                // ถ้าจางแค่ groupRenderer ตัวเดียว ส่วนที่เหลือจะค้างแล้วหายวับตอน Destroy
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
                Color[] startColors = new Color[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    startColors[i] = renderers[i].color;
                }

                Vector3 startScale = transform.localScale;
                Vector3 endScale = startScale * disappearEndScale;

                float time = 0f;
                while (time < disappearDuration)
                {
                    time += Time.deltaTime;
                    float t = Mathf.Clamp01(time / disappearDuration);
                    t = t * t * (3f - 2f * t);

                    for (int i = 0; i < renderers.Length; i++)
                    {
                        if (renderers[i] == null)
                            continue;

                        Color c = startColors[i];
                        c.a = Mathf.Lerp(startColors[i].a, 0f, t);
                        renderers[i].color = c;
                    }

                    transform.localScale = Vector3.Lerp(startScale, endScale, t);
                    yield return null;
                }
            }
        }

        Destroy(gameObject);
    }
}