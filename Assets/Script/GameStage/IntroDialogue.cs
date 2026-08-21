using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class IntroDialogue : MonoBehaviour
{
    [Header("การเดินของ NPC")]
    public Transform startPoint;
    public Transform stopPoint;
    [Tooltip("เวลาหน่วงก่อนเริ่มเดินเข้ามา (วินาที)")]
    public float preWalkDelay = 0f;
    public float walkSpeed = 5f;
    [Tooltip("ความสูงของการเด้งขึ้นลงตอน NPC เดิน (ใส่ 0 ถ้าไม่ต้องการเด้ง)")]
    public float walkBobHeight = 0.15f;
    [Tooltip("ความเร็วของการเด้งขึ้นลงตอนเดิน (ยิ่งมากยิ่งเดินถี่)")]
    public float walkBobSpeed = 10f;

    [Header("UI กรอบข้อความ")]
    public Transform dialogBubble;
    public TMP_Text dialogText;
    [Tooltip("ความเร็วในการพิมพ์ตัวอักษร (ยิ่งน้อยยิ่งพิมพ์เร็ว)")]
    public float typeSpeed = 0.05f; // ✨ เพิ่มช่องปรับความเร็วการพิมพ์

    [Header("🗣️ สไปรท์ตอนพูด")]
    [Tooltip("SpriteRenderer ของตัว NPC ที่จะสลับสไปรท์ตอนพูด (ถ้าไม่ใส่จะหาให้อัตโนมัติจาก GetComponent)")]
    public SpriteRenderer npcSpriteRenderer;
    [Tooltip("สไปรท์ตอนไม่ได้พูด (ปากปิด/หน้านิ่ง) ถ้าไม่ใส่จะใช้สไปรท์เดิมที่ผูกไว้ตอนเริ่มเกม")]
    public Sprite idleSprite;
    [Tooltip("สไปรท์ที่จะวนสลับกันตอนกำลังพิมพ์ข้อความ (เช่น ปากเปิด/ปากปิดสลับกัน) ให้ดูเหมือนกำลังพูดอยู่")]
    public Sprite[] talkingSprites;
    [Tooltip("ความเร็วในการสลับเฟรมตอนพูด (วินาทีต่อเฟรม ยิ่งน้อยยิ่งขยับปากถี่)")]
    public float talkFrameInterval = 0.12f;

    [Header("Debug")]
    [Tooltip("ปุ่มสำหรับข้าม Intro ทั้งหมดทันที (ใช้ตอนทดสอบเกม)")]
    public Button skipIntroButton;

    [Header("✨ Visual Polish")]
    [Tooltip("ไอคอน/ลูกศรเตือนให้คลิกไปหน้าถัดไป (ไม่บังคับ) จะเด้งขึ้นลงเฉพาะตอนรอผู้เล่นคลิก")]
    public Transform continueIndicator;
    [Tooltip("ความสูงของการเด้งไอคอน continueIndicator")]
    public float indicatorBounceHeight = 8f;
    [Tooltip("ความเร็วของการเด้งไอคอน continueIndicator")]
    public float indicatorBounceSpeed = 3f;
    [Tooltip("ให้กรอบข้อความเด้งเล็กน้อยทุกครั้งที่เปลี่ยนหน้าข้อความ เพื่อความมีชีวิตชีวา")]
    public bool punchOnNewPage = true;

    [Header("🚦 Ready / Go")]
    [Tooltip("ปิดการแสดง Ready?/Go!!! ทั้งหมด (ปิดไว้เพราะรบกวนตอนทดสอบใน Unity)")]
    public bool enableReadyGoSequence = false;
    [Tooltip("Root object ของไอคอน Ready?/Go!!! (จะถูกซ่อนไว้จนกว่า Intro dialogue จะจบ)")]
    public Transform readyGoRoot;
    [Tooltip("Image ที่จะสลับ sprite ระหว่าง Ready กับ Go")]
    public Image readyGoImage;
    public Sprite readySprite;
    public Sprite goSprite;
    [Tooltip("เวลาที่ค้างไอคอน Ready? ไว้บนจอ ก่อนเปลี่ยนเป็น Go!!!")]
    public float readyHoldDuration = 0.7f;
    [Tooltip("เวลาที่ค้างไอคอน Go!!! ไว้บนจอ ก่อนเริ่มเกม")]
    public float goHoldDuration = 0.6f;
    [Tooltip("มุมเอียงตอนเด้งเข้ามา (องศา) จะคลายกลับเป็น 0 ระหว่างเด้ง ใส่ 0 ถ้าไม่ต้องการหมุน")]
    public float readyGoTiltAngle = 8f;
    [Tooltip("ความแรงบีบ/ยืด (squash-and-stretch) ตอน 'ลงจอด' หลังเด้งเข้ามาเต็มขนาด")]
    public float readyGoLandingSquashAmount = 0.18f;
    [Tooltip("ระยะเวลาบีบ/ยืดตอนลงจอด")]
    public float readyGoLandingSquashDuration = 0.18f;
    [Tooltip("มุมหมุนตอนหดหายไป (องศา) ให้ความรู้สึกถูกดูดหายแทนที่จะหดตรงๆ")]
    public float readyGoPopOutSpin = 20f;

    private string[] dialogPages;
    private Vector3 savedBubbleScale;
    private CanvasGroup dialogCanvasGroup;
    private Vector3 indicatorBasePos;
    private Coroutine indicatorRoutine;
    private Vector3 savedReadyGoScale;
    private CanvasGroup readyGoCanvasGroup;
    private Coroutine talkingRoutine;

    // ตัวแปรเช็คสถานะการคลิก
    private bool isTyping = false;
    private bool skipTyping = false;
    private bool isWaitingForNextPage = false;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            StageConfig currentStage = GameManager.Instance.GetCurrentStage();
            if (currentStage != null && !string.IsNullOrEmpty(currentStage.introMessage))
            {
                dialogPages = currentStage.introMessage.Split('\n');
            }
            else
            {
                dialogPages = new string[] { "ภารกิจวันนี้... เอ่อ... ฉันลืมบทน่ะ!" };
            }
        }

        savedBubbleScale = dialogBubble.localScale;
        dialogBubble.localScale = Vector3.zero;
        transform.position = startPoint.position;

        // เผื่อไม่ได้ผูก npcSpriteRenderer ไว้ใน Inspector ให้ลองหาจากตัวเองก่อน
        if (npcSpriteRenderer == null)
        {
            npcSpriteRenderer = GetComponent<SpriteRenderer>();
        }
        // จำสไปรท์เดิมไว้เป็นหน้าปกติ (ตอนไม่ได้พูด) ถ้ายังไม่ได้ตั้งค่าไว้เอง
        if (idleSprite == null && npcSpriteRenderer != null)
        {
            idleSprite = npcSpriteRenderer.sprite;
        }

        dialogCanvasGroup = dialogBubble.GetComponent<CanvasGroup>();
        if (dialogCanvasGroup == null)
        {
            dialogCanvasGroup = dialogBubble.gameObject.AddComponent<CanvasGroup>();
        }
        dialogCanvasGroup.alpha = 0f;

        if (continueIndicator != null)
        {
            indicatorBasePos = continueIndicator.localPosition;
            continueIndicator.gameObject.SetActive(false);
        }

        if (readyGoRoot != null)
        {
            savedReadyGoScale = readyGoRoot.localScale;
            readyGoRoot.localScale = Vector3.zero;
            readyGoCanvasGroup = readyGoRoot.GetComponent<CanvasGroup>();
            if (readyGoCanvasGroup == null)
            {
                readyGoCanvasGroup = readyGoRoot.gameObject.AddComponent<CanvasGroup>();
            }
            readyGoCanvasGroup.alpha = 0f;
            readyGoRoot.gameObject.SetActive(false);
        }

        // ป้องกัน Image โชว์เป็นกล่องสีขาวตอนยังไม่ได้ผูก sprite (เผื่อ build ที่ไม่มี OnValidate ทำงาน)
        if (readyGoImage != null)
        {
            readyGoImage.enabled = readySprite != null || goSprite != null;
        }

        if (skipIntroButton != null)
        {
            skipIntroButton.onClick.AddListener(SkipIntro);
        }

        StartCoroutine(IntroSequence());
    }

    // ป้องกัน Image โชว์เป็นกล่องสีขาว (ค่า default ของ Unity ตอนไม่ได้ใส่ sprite) ตอนยังไม่ได้ผูก readySprite/goSprite
    private void OnValidate()
    {
        if (readyGoImage != null)
        {
            readyGoImage.enabled = readySprite != null || goSprite != null;
        }
    }

    // เรียกจากปุ่ม Skip (Debug) เพื่อข้าม Intro ทั้งหมดทันทีและเริ่มเกมเลย
    public void SkipIntro()
    {
        StopAllCoroutines();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        Destroy(gameObject);
    }

    private void Update()
    {
        // ระบบรับการคลิกเมาส์
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (isTyping)
            {
                // ถ้ากำลังพิมพ์อยู่ ให้ข้ามไปแสดงข้อความเต็มๆ ทันที
                skipTyping = true;
            }
            else if (isWaitingForNextPage)
            {
                // ถ้าพิมพ์เสร็จแล้ว ให้กดเพื่อไปหน้าถัดไป
                isWaitingForNextPage = false;
            }
        }
    }

    private IEnumerator IntroSequence()
    {
        // 1. หน่วงเวลาก่อนเริ่มเดิน (ถ้าตั้งค่าไว้) แล้วเดินเข้ามาจากนอกจอ (เดินช้าลงตอนใกล้ถึงจุดหมาย)
        if (preWalkDelay > 0f)
        {
            yield return new WaitForSeconds(preWalkDelay);
        }
        yield return StartCoroutine(WalkTo(stopPoint.position));

        // 2. เด้งกรอบข้อความ (ตอนแรกจะยังไม่มีข้อความ)
        dialogText.text = "";
        yield return StartCoroutine(PopInAnimation(dialogBubble));

        // 3. เริ่มลูปแสดงข้อความทีละหน้า
        for (int i = 0; i < dialogPages.Length; i++)
        {
            string sentence = dialogPages[i].Trim();
            if (string.IsNullOrEmpty(sentence)) continue;

            // เด้งกรอบเบาๆ ทุกครั้งที่ขึ้นหน้าข้อความใหม่ (ยกเว้นหน้าแรกที่เพิ่งเด้งเข้ามา)
            if (punchOnNewPage && i > 0)
            {
                yield return StartCoroutine(PunchAnimation(dialogBubble));
            }

            // ✨ เริ่มกระบวนการพิมพ์ทีละตัวอักษร
            isTyping = true;
            skipTyping = false;
            dialogText.text = "";
            SetIndicatorVisible(false);
            StartTalkingAnimation(); // ✨ เริ่มขยับปาก NPC ตอนกำลังพิมพ์ข้อความ

            foreach (char letter in sentence.ToCharArray())
            {
                if (skipTyping) // ถ้าผู้เล่นกดคลิกข้าม
                {
                    dialogText.text = sentence; // โชว์ข้อความทั้งหมดทันที
                    break;
                }

                dialogText.text += letter;
                yield return new WaitForSeconds(typeSpeed); // รอเวลาตามความเร็วที่ตั้งไว้
            }

            // พิมพ์จบแล้ว หยุดขยับปาก กลับเป็นหน้านิ่ง แล้วรอผู้เล่นคลิกเพื่อไปหน้าต่อไป
            isTyping = false;
            StopTalkingAnimation();
            isWaitingForNextPage = true;
            SetIndicatorVisible(true);

            while (isWaitingForNextPage)
            {
                yield return null;
            }

            SetIndicatorVisible(false);
        }

        // 4. หดกรอบหายไป
        yield return StartCoroutine(PopOutAnimation(dialogBubble));

        // 5. เดินกลับออกไปนอกจอ
        yield return StartCoroutine(WalkTo(startPoint.position));

        // 5.5 โชว์ Ready? แล้วตามด้วย Go!!! เป็นการนับถอยหลังก่อนเริ่มเกมจริง
        yield return StartCoroutine(PlayReadyGoSequence());

        // 6. สั่งให้เกมเริ่ม และทำลายตัวเองทิ้ง
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameManager.GameState.Playing);
        }
        Destroy(gameObject);
    }

    // ==========================================
    // 🎨 ฟังก์ชันช่วยทำแอนิเมชันตอนเด้ง
    // ==========================================

    // เดินไปยัง destination โดยชะลอความเร็วตอนใกล้ถึง (ease-out) พร้อมเด้งขึ้นลงตอนเดิน
    private IEnumerator WalkTo(Vector3 destination)
    {
        // แยกตำแหน่งจริง (logical) ออกจากตำแหน่งที่แสดงผล (มีเด้งบวกเข้าไป)
        // เพื่อไม่ให้การเด้งขึ้นลงไปกวนเงื่อนไขระยะทางที่ใช้เช็คว่าถึงจุดหมายหรือยัง
        Vector3 logicalPos = transform.position;
        float totalDistance = Vector3.Distance(logicalPos, destination);
        float easeZone = Mathf.Max(totalDistance * 0.3f, 0.01f);
        float bobTimer = 0f;

        while (Vector3.Distance(logicalPos, destination) > 0.01f)
        {
            float remaining = Vector3.Distance(logicalPos, destination);
            float easeFactor = Mathf.Clamp01(remaining / easeZone);
            float currentSpeed = walkSpeed * Mathf.Lerp(0.25f, 1f, easeFactor);
            logicalPos = Vector3.MoveTowards(logicalPos, destination, currentSpeed * Time.deltaTime);

            bobTimer += Time.deltaTime * walkBobSpeed;
            float bobOffset = Mathf.Abs(Mathf.Sin(bobTimer)) * walkBobHeight;
            transform.position = logicalPos + Vector3.up * bobOffset;

            yield return null;
        }
        transform.position = destination;
    }

    private IEnumerator PopInAnimation(Transform target)
    {
        Vector3 originalScale = savedBubbleScale;
        target.localScale = Vector3.zero;
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            target.localScale = originalScale * scaleFactor;
            if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            yield return null;
        }
        target.localScale = originalScale;
        if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 1f;
    }

    private IEnumerator PopOutAnimation(Transform target)
    {
        float duration = 0.2f;
        float time = 0f;
        Vector3 startScale = target.localScale;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            target.localScale = startScale * scaleFactor;
            if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 1f - t;
            yield return null;
        }
        target.localScale = Vector3.zero;
        if (dialogCanvasGroup != null) dialogCanvasGroup.alpha = 0f;
    }

    // เริ่มวนสลับสไปรท์ตอนพูด (ปากขยับ) ระหว่างที่ข้อความกำลังพิมพ์อยู่
    private void StartTalkingAnimation()
    {
        if (npcSpriteRenderer == null || talkingSprites == null || talkingSprites.Length == 0) return;

        StopTalkingAnimation(); // กันไว้เผื่อมีลูปเก่าค้างอยู่ ก่อนเริ่มลูปใหม่
        talkingRoutine = StartCoroutine(TalkingAnimationLoop());
    }

    // หยุดวนสลับสไปรท์ แล้วกลับไปที่หน้านิ่ง (idleSprite)
    private void StopTalkingAnimation()
    {
        if (talkingRoutine != null)
        {
            StopCoroutine(talkingRoutine);
            talkingRoutine = null;
        }
        if (npcSpriteRenderer != null && idleSprite != null)
        {
            npcSpriteRenderer.sprite = idleSprite;
        }
    }

    private IEnumerator TalkingAnimationLoop()
    {
        int frameIndex = 0;
        while (true)
        {
            npcSpriteRenderer.sprite = talkingSprites[frameIndex % talkingSprites.Length];
            frameIndex++;
            yield return new WaitForSeconds(talkFrameInterval);
        }
    }

    // เด้งกรอบข้อความเบาๆ (punch) ตอนขึ้นหน้าใหม่ ให้รู้สึกมีชีวิตชีวา
    private IEnumerator PunchAnimation(Transform target)
    {
        Vector3 originalScale = savedBubbleScale;
        float duration = 0.15f;
        float time = 0f;
        float punchStrength = 0.08f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float bump = Mathf.Sin(t * Mathf.PI) * punchStrength;
            target.localScale = originalScale * (1f + bump);
            yield return null;
        }
        target.localScale = originalScale;
    }

    // เล่นลำดับ Ready? -> Go!!! ก่อนเข้าเกมจริง (ถ้าไม่ได้ผูก readyGoRoot ไว้ก็ข้ามไปเลย)
    private IEnumerator PlayReadyGoSequence()
    {
        if (readyGoRoot == null || !enableReadyGoSequence) yield break;

        readyGoRoot.gameObject.SetActive(true);

        if (readyGoImage != null) readyGoImage.sprite = readySprite;
        yield return StartCoroutine(ScalePopIn(readyGoRoot, savedReadyGoScale, readyGoCanvasGroup));
        yield return new WaitForSeconds(readyHoldDuration);
        yield return StartCoroutine(ScalePopOut(readyGoRoot, readyGoCanvasGroup));

        if (readyGoImage != null) readyGoImage.sprite = goSprite;
        yield return StartCoroutine(ScalePopIn(readyGoRoot, savedReadyGoScale, readyGoCanvasGroup));
        yield return new WaitForSeconds(goHoldDuration);
        yield return StartCoroutine(ScalePopOut(readyGoRoot, readyGoCanvasGroup));

        readyGoRoot.gameObject.SetActive(false);
    }

    // เด้งเข้าแบบ ease-out-back (overshoot) พร้อมหมุนคลายมุมเอียงและเฟดอัลฟ่า
    // จบด้วยการบีบ/ยืดตอน "ลงจอด" ให้รู้สึกกระแทกหนักแน่นแทนที่จะหยุดนิ่งดื้อๆ
    private IEnumerator ScalePopIn(Transform target, Vector3 originalScale, CanvasGroup canvasGroup)
    {
        target.localScale = Vector3.zero;
        target.localRotation = Quaternion.Euler(0f, 0f, readyGoTiltAngle);
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f);
            target.localScale = originalScale * scaleFactor;
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpAngle(readyGoTiltAngle, 0f, Mathf.Clamp01(t * 1.3f)));
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Clamp01(t * 1.5f);
            yield return null;
        }
        target.localScale = originalScale;
        target.localRotation = Quaternion.identity;
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        yield return StartCoroutine(PlayLandingSquash(target, originalScale));
    }

    // เด้งบีบ/ยืดสั้นๆ (squash-and-stretch) ตอนลงจอดหลังขยายเต็มขนาด สลายแรงเด้งลงเรื่อยๆ จนนิ่ง
    private IEnumerator PlayLandingSquash(Transform target, Vector3 baseScale)
    {
        if (readyGoLandingSquashAmount <= 0f) yield break;

        float time = 0f;
        while (time < readyGoLandingSquashDuration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / readyGoLandingSquashDuration);
            float squash = Mathf.Sin(t * Mathf.PI) * readyGoLandingSquashAmount * (1f - t);
            target.localScale = new Vector3(baseScale.x * (1f + squash), baseScale.y * (1f - squash), baseScale.z);
            yield return null;
        }
        target.localScale = baseScale;
    }

    // ยุบตัวเบาๆ ก่อน (anticipation) แล้วหดหายไปพร้อมหมุนวนและเฟดอัลฟ่า
    // ให้ความรู้สึกถูกดูดหายไปแบบมีแรงส่ง แทนที่จะหดตรงๆ เข้าจุดศูนย์กลาง
    private IEnumerator ScalePopOut(Transform target, CanvasGroup canvasGroup)
    {
        Vector3 startScale = target.localScale;

        float punchDuration = 0.08f;
        float punchTime = 0f;
        while (punchTime < punchDuration)
        {
            punchTime += Time.deltaTime;
            float t = punchTime / punchDuration;
            float squash = Mathf.Sin(t * Mathf.PI) * 0.15f;
            target.localScale = new Vector3(startScale.x * (1f + squash), startScale.y * (1f - squash), startScale.z);
            yield return null;
        }

        float duration = 0.2f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            float scaleFactor = Mathf.Lerp(1f, 0f, t * t);
            target.localScale = startScale * scaleFactor;
            target.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, readyGoPopOutSpin, t));
            if (canvasGroup != null) canvasGroup.alpha = 1f - t;
            yield return null;
        }
        target.localScale = Vector3.zero;
        target.localRotation = Quaternion.identity;
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    // แสดง/ซ่อน continueIndicator และเริ่ม-หยุดแอนิเมชันเด้งของมัน
    private void SetIndicatorVisible(bool visible)
    {
        if (continueIndicator == null) return;

        if (visible)
        {
            continueIndicator.gameObject.SetActive(true);
            if (indicatorRoutine == null)
            {
                indicatorRoutine = StartCoroutine(IndicatorBounceLoop());
            }
        }
        else
        {
            if (indicatorRoutine != null)
            {
                StopCoroutine(indicatorRoutine);
                indicatorRoutine = null;
            }
            continueIndicator.localPosition = indicatorBasePos;
            continueIndicator.gameObject.SetActive(false);
        }
    }

    private IEnumerator IndicatorBounceLoop()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * indicatorBounceSpeed;
            float offset = Mathf.Abs(Mathf.Sin(t)) * indicatorBounceHeight;
            continueIndicator.localPosition = indicatorBasePos + Vector3.up * offset;
            yield return null;
        }
    }
}
