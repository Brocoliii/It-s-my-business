using UnityEngine;
using System.Collections.Generic;

public class InvestigateGroup : MonoBehaviour, IInvestigatable
{
    [Header("Visual")]
    [SerializeField] private SpriteRenderer groupRenderer;
    [SerializeField] private Sprite[] possibleSprites;

    [Header("Stratagem Minigame")]
    [SerializeField] private int stratagemLength = 5;
    [SerializeField] private float progressPerSuccess = 10f;
    [SerializeField] private float correctFlashDuration = 0.15f;
    [Tooltip("เวลารอหลังต่อชุดปุ่มครบ ก่อนชุดใหม่จะเด้งขึ้น เผื่อให้คลื่นไอคอนวิ่งจบก่อน")]
    [SerializeField] private float sequenceClearedDelay = 0.28f;
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

    // สำหรับ Debug/Skip เท่านั้น: บังคับให้กลุ่มออกจากฉากทันที ผ่านเส้นทางเดิม (RemoveGroup)
    // เพื่อให้ manager.OnGroupLeft ถูกเรียกด้วย ไม่งั้น activeGroups/occupiedPoints ใน InvestigationManager จะค้าง
    public void ForceRemove()
    {
        RemoveGroup();
    }

    public void Init(InvestigationManager mgr, string text, float duration)
    {
        manager = mgr;
        clueDetail = text;
        requiredListenTime = duration;
        currentListenTime = 0f;
        currentStratagem.Clear();
        currentStratagemIndex = 0;
        lifeTimer = requiredListenTime + 10f;
        blinkPhase = 0.25f;
        baseGroupScale = transform.localScale;
        ApplyRandomSprite();
        if (groupRenderer != null)
        {
            baseSpriteColor = groupRenderer.color;
        }
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(true);
            UIManager.Instance.UpdateInvestigateTimer(lifeTimer);
        }

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
        groupRenderer.sprite = possibleSprites[Random.Range(0, possibleSprites.Length)];
    }

    private void Update()
    {
        // ระหว่างเล่นแอนิเมชันจางหาย ตัวกลุ่มยังไม่ถูก Destroy จริง
        // ต้องหยุด Update ทั้งหมด ไม่งั้นการกะพริบจะไปทับค่า alpha ที่กำลังจางอยู่
        // และ RemoveGroup() จะถูกเรียกซ้ำทุกเฟรมตอนหมดเวลา
        if (isRemoving)
            return;

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
    }

    public void OnListenStart()
    {
        if (!isClueCollected && !isRemoving)
        {
            isBeingListened = true;
            EnsureStratagemExists();
            // เล่นแอนิเมชันไอคอนเด้งขึ้นใหม่ทุกครั้งที่เริ่มฟัง แม้จะเป็นชุดเดิมที่ค้างไว้
            // ไม่งั้นไอคอนจะโผล่มาเฉย ๆ ตอนกดฟังซ้ำ
            pendingSequenceIntro = true;
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
        if (!isBeingListened || isClueCollected || isResettingAfterMistake || isRemoving)
            return;

        EnsureStratagemExists();

        if (currentStratagem.Count == 0)
            return;

        if (direction == currentStratagem[currentStratagemIndex])
        {
            HandleCorrectInputFeedback();
        }
        else
        {
            StartCoroutine(HandleWrongInputFeedback());
        }
    }

    public void OnListenEnd()
    {
        if (isBeingListened)
        {
            isBeingListened = false;
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
        currentStratagem.Clear();

        for (int i = 0; i < stratagemLength; i++)
        {
            currentStratagem.Add((StratagemDirection)Random.Range(0, 4));
        }

        currentStratagemIndex = 0;
        pendingSequenceIntro = true;
    }

    private void RefreshInvestigationUI()
    {
        if (UIManager.Instance == null || isClueCollected)
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

                if (groupRenderer != null)
                {
                    groupRenderer.color = baseSpriteColor;
                }

                StartCoroutine(CollectClueAfterBarFilled());
                return;
            }

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
        yield return new WaitForSeconds(delay);

        if (!isBeingListened || isClueCollected)
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
        RefreshInvestigationUI();
    }

    private void RemoveGroup()
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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowInvestigateTimer(false);
        }

        // ปิด Collider ก่อนเริ่มจางหาย ไม่ให้ผู้เล่นกดฟังกลุ่มที่กำลังจะหายไปแล้วได้อีก
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
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